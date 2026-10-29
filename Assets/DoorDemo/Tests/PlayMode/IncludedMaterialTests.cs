using NUnit.Framework;
using UnityEngine;

namespace DoorDemo.Tests
{
    public sealed class IncludedMaterialTests
    {
        /// <summary>Verifies that runtime material is an included resource with a shader.</summary>
        [Test]
        public void RuntimeMaterialIsAnIncludedResourceWithAShader()
        {
            var material = Resources.Load<Material>("RoomSurface");
            Assert.That(material, Is.Not.Null, "El material debe estar en Resources para incluir su shader en el player.");
            Assert.That(material.shader, Is.Not.Null);
            var cube = Resources.Load<Mesh>("RoomCube");
            Assert.That(cube, Is.Not.Null, "La geometria debe incluirse sin crear primitivas que dependan del stripping.");
            Assert.That(cube.vertexCount, Is.GreaterThan(0));
        }
    }
}
