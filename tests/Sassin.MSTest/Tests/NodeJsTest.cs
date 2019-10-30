using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.IO;

namespace Acklann.Sassin.Tests
{
    [TestClass]
    public class NodeJsTest
    {
        [TestMethod]
        public void Can_load_modules()
        {
            // Arrange
            var node_modules = Path.Combine(NodeJS.InstallationDirectory, "node_modules");
            //if (Directory.Exists(node_modules)) Directory.Delete(node_modules, recursive: true);

            // Act
            NodeJS.Install();
            var installed = NodeJS.CheckInstallation();
            var modulesExist = Directory.Exists(node_modules);

            // Assert
            installed.ShouldBeTrue();
            modulesExist.ShouldBeTrue();
        }
    }
}