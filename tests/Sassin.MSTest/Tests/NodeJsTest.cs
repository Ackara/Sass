using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.IO;

namespace Acklann.Sassin.Tests
{
    [TestClass]
    public class NodeJsTest
    {
        [TestMethod]
        public void Can_check_nodejs_availibility()
        {
            // Arrange
            if (Directory.Exists(NodeJsController.WorkingDirectory)) Directory.Delete(NodeJsController.WorkingDirectory, recursive: true);

            // Act
            NodeJsController.Install();
            var afterInstalltion = NodeJsController.CheckInstallation();

            // Assert

            afterInstalltion.ShouldBeTrue();
        }
    }
}