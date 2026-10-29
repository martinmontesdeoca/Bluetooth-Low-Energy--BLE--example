using System.Collections.Generic;
using UnityEngine;

namespace DoorDemo.Presentation
{
    /// <summary>Builds the small demo set from primitives; no downloaded art is needed.</summary>
    public sealed class DemoSceneFactory : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private Material template;
        private Mesh cube;

        /// <summary>Builds the lightweight room from included resources so IL2CPP cannot strip dynamically needed assets.</summary>
        public DoorView Create(float speed)
        {
            template = Resources.Load<Material>("RoomSurface");
            cube = Resources.Load<Mesh>("RoomCube");
            if (template == null || template.shader == null || cube == null)
                throw new System.InvalidOperationException("Faltan recursos de la sala. Ejecuta Door Demo/Prepare.");
            RenderSettings.ambientLight = new Color(0.35f, 0.42f, 0.52f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.09f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.025f;

            var cameraObject = new GameObject("Demo Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(5.2f, 3.5f, -7.8f);
            cameraObject.transform.LookAt(new Vector3(1.1f, 1.4f, 0));
            var camera = cameraObject.GetComponent<Camera>();
            camera.backgroundColor = RenderSettings.fogColor;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 42;

            var lightObject = new GameObject("Key Light", typeof(Light));
            lightObject.transform.SetParent(transform);
            lightObject.transform.rotation = Quaternion.Euler(45, -25, 0);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;

            Material floor = MakeMaterial(new Color(0.08f, 0.12f, 0.18f));
            Material frame = MakeMaterial(new Color(0.2f, 0.3f, 0.38f));
            Material door = MakeMaterial(new Color(0.08f, 0.55f, 0.65f));
            Material trim = MakeMaterial(new Color(0.65f, 0.9f, 0.95f));
            Material wall = MakeMaterial(new Color(0.4f, 0.55f, 0.62f));
            Box("Floor", transform, new Vector3(0, -0.16f, 0), new Vector3(18, 0.3f, 18), floor);
            for (int i = -6; i <= 6; i++)
                Box("Floor Line", transform, new Vector3(i, 0.002f, 0),
                    new Vector3(0.015f, 0.008f, 12), frame);

            var stage = new GameObject("Door Stage").transform;
            stage.SetParent(transform);
            stage.position = new Vector3(1.7f, 0, 0);
            Box("Room Back Wall", stage, new Vector3(0, 1.6f, 3.4f), new Vector3(5.5f, 3.2f, 0.15f), wall);
            Box("Room Side Wall", stage, new Vector3(-2.7f, 1.6f, 1.7f), new Vector3(0.15f, 3.2f, 3.4f), wall);
            Box("Desk", stage, new Vector3(0.3f, 0.9f, 2.4f), new Vector3(2.2f, 0.14f, 0.8f), trim);
            Box("Desk Base", stage, new Vector3(0.3f, 0.4f, 2.4f), new Vector3(0.15f, 0.8f, 0.6f), frame);
            Box("Monitor", stage, new Vector3(0.3f, 1.22f, 2.6f), new Vector3(0.8f, 0.5f, 0.12f), door);
            Box("Wall Accent", stage, new Vector3(0, 2.3f, 3.28f), new Vector3(3.2f, 0.08f, 0.04f), door);
            Box("Left Post", stage, new Vector3(-1.15f, 1.55f, 0), new Vector3(0.22f, 3.1f, 0.4f), frame);
            Box("Right Post", stage, new Vector3(1.15f, 1.55f, 0), new Vector3(0.22f, 3.1f, 0.4f), frame);
            Box("Lintel", stage, new Vector3(0, 3.1f, 0), new Vector3(2.52f, 0.22f, 0.4f), frame);
            var hinge = new GameObject("Door Hinge").transform;
            hinge.SetParent(stage, false);
            hinge.localPosition = new Vector3(-1.02f, 0, 0);
            Box("Door Panel", hinge, new Vector3(1.02f, 1.49f, 0), new Vector3(2.04f, 2.94f, 0.15f), door);
            Box("Handle", hinge, new Vector3(1.8f, 1.4f, -0.14f), new Vector3(0.08f, 0.4f, 0.12f), trim);
            var indicator = Box("Indicator", stage, new Vector3(0, 3.12f, -0.23f),
                new Vector3(0.65f, 0.07f, 0.07f), MakeMaterial(Color.red));
            var view = stage.gameObject.AddComponent<DoorView>();
            view.Initialize(hinge, indicator, speed);
            return view;
        }

        /// <summary>Clones the included material template and tracks ownership of the generated instance.</summary>
        private Material MakeMaterial(Color color)
        {
            var material = new Material(template) { color = color };
            material.SetFloat("_Glossiness", 0.35f);
            materials.Add(material);
            return material;
        }

        /// <summary>Creates one mesh-rendered room element without depending on runtime primitive collider creation.</summary>
        private Renderer Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<MeshFilter>().sharedMesh = cube;
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        /// <summary>Releases the generated material instances when the room is destroyed.</summary>
        private void OnDestroy()
        {
            foreach (Material material in materials)
                Destroy(material);
        }
    }
}
