using System;
using System.IO;
using DoorDemo.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DoorDemo.Editor
{
    public static class DemoProjectTools
    {
        private const string ScenePath = "Assets/DoorDemo/Scenes/DoorDemo.unity";
        private const string SettingsPath = "Assets/DoorDemo/Configuration/DoorDemoSettings.asset";

        /// <summary>Creates missing demo assets and a scene while preserving an existing scene.</summary>
        [MenuItem("Door Demo/Create or open demo scene")]
        public static void Prepare()
        {
            ConfigurePlayer();
            EnsureSurfaceMaterial();
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            AssetDatabase.Refresh();
            var settings = AssetDatabase.LoadAssetAtPath<DemoSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DemoSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Door Demo").AddComponent<DemoBootstrap>().Configure(settings);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Door Demo: escena y configuracion listas.");
        }

        /// <summary>Includes the material and cube mesh explicitly so player stripping cannot remove runtime dependencies.</summary>
        private static void EnsureSurfaceMaterial()
        {
            const string materialPath = "Assets/DoorDemo/Resources/RoomSurface.mat";
            Directory.CreateDirectory(Path.GetDirectoryName(materialPath));
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("No se encontro el shader Standard en el editor.");
                AssetDatabase.CreateAsset(new Material(shader), materialPath);
            }
            const string meshPath = "Assets/DoorDemo/Resources/RoomCube.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) == null)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var mesh = UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
                AssetDatabase.CreateAsset(mesh, meshPath);
                UnityEngine.Object.DestroyImmediate(primitive);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Applies reproducible desktop defaults for the optional Unity client.</summary>
        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Independent Portfolio";
            PlayerSettings.productName = "Connected Room - BLE Demo";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,
                ScriptingImplementation.Mono2x);
        }

        /// <summary>Builds the optional Unity Windows client and copies its helper runtime alongside the player.</summary>
        [MenuItem("Door Demo/Build Windows demo")]
        public static void BuildWindows()
        {
            Prepare();
            if (!File.Exists("Artifacts/BleBridge/BleBridge.exe"))
                throw new FileNotFoundException("Ejecuta Tools/Publish-Bridge.ps1 antes de compilar.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/BleDoorDemo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("La compilacion de Windows fallo: " + report.summary.result);
            string bridgeOutput = "Builds/Windows/BleDoorDemo_Data/StreamingAssets/BleBridge";
            Directory.CreateDirectory(bridgeOutput);
            foreach (string file in Directory.GetFiles("Artifacts/BleBridge"))
                File.Copy(file, Path.Combine(bridgeOutput, Path.GetFileName(file)), true);
            Debug.Log("Door Demo: build Windows completado.");
        }

        /// <summary>Builds the Android ARM64 APK with the generated native BLE plugin and configured API targets.</summary>
        [MenuItem("Door Demo/Build Android APK")]
        public static void BuildAndroid()
        {
            Prepare();
            if (!File.Exists("Assets/Plugins/Android/ConnectedRoomBle.aar"))
                throw new FileNotFoundException("Ejecuta Tools/Build-AndroidPlugin.ps1 antes de compilar Android.");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,
                "com.portfolio.connectedroom");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Android/ConnectedRoom.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("La compilacion Android fallo: " + report.summary.result);
            Debug.Log("Connected Room: APK Android ARM64 completado.");
        }
    }
}
