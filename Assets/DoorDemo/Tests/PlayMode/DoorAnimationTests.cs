using System.Collections;
using DoorDemo.Application;
using DoorDemo.Infrastructure;
using DoorDemo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DoorDemo.Tests
{
    public sealed class DoorAnimationTests
    {
        /// <summary>Verifies that simulated commands animate door and disconnect closes it.</summary>
        [UnityTest]
        public IEnumerator SimulatedCommandsAnimateDoorAndDisconnectClosesIt()
        {
            var root = new GameObject("Demo under test");
            DoorController controller = null;
            try
            {
                var view = root.AddComponent<DemoSceneFactory>().Create(720);
                var transport = new SimulatedDoorTransport();
                controller = new DoorController(transport);
                controller.DoorChanged += view.Show;
                var hinge = root.transform.Find("Door Stage/Door Hinge");
                controller.Connect();
                transport.SetOpen(true);
                yield return new WaitForSeconds(0.3f);
                Assert.That(Quaternion.Angle(hinge.localRotation, Quaternion.Euler(0, -100, 0)), Is.LessThan(1));
                transport.SetOpen(false);
                yield return new WaitForSeconds(0.3f);
                Assert.That(Quaternion.Angle(hinge.localRotation, Quaternion.identity), Is.LessThan(1));
                transport.SetOpen(true);
                yield return new WaitForSeconds(0.3f);
                controller.Disconnect();
                yield return new WaitForSeconds(0.3f);
                Assert.That(Quaternion.Angle(hinge.localRotation, Quaternion.identity), Is.LessThan(1));
            }
            finally
            {
                controller?.Dispose();
                Object.Destroy(root);
            }
        }
    }
}
