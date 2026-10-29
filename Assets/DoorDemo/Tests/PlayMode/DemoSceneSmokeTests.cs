using System.Collections;
using System.IO;
using DoorDemo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DoorDemo.Tests
{
    public sealed class DemoSceneSmokeTests
    {
        /// <summary>Verifies that saved scene starts with camera and hud without runtime errors.</summary>
        [UnityTest]
        public IEnumerator SavedSceneStartsWithCameraAndHudWithoutRuntimeErrors()
        {
            yield return SceneManager.LoadSceneAsync("DoorDemo");
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<DemoBootstrap>();
            try
            {
                Assert.That(bootstrap, Is.Not.Null);
                Assert.That(Object.FindAnyObjectByType<DemoHud>(), Is.Not.Null);
                Assert.That(Camera.main, Is.Not.Null);
                yield return null;
                Directory.CreateDirectory("Artifacts");
                string preview = Path.GetFullPath("Artifacts/ConnectedRoom-preview.png");
                CaptureScene(Camera.main, preview);
                Assert.That(File.Exists(preview), Is.True, "No se genero la captura de la escena.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (bootstrap != null) Object.Destroy(bootstrap.gameObject);
            }
        }

        /// <summary>Renders the camera explicitly because batch-mode tests do not present a Game View.</summary>
        private static void CaptureScene(Camera camera, string path)
        {
            // Batch-mode tests do not present a Game View, so render the camera explicitly.
            var target = new RenderTexture(1280, 800, 24);
            var image = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previous;
                Object.Destroy(image);
                Object.Destroy(target);
            }
        }
    }
}
