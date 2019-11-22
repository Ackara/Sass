using Acklann.Sassin.MSBuild;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.IO;
using System.Linq;

namespace Acklann.Sassin.Tests
{
    [TestClass]
    public class MSBuildTest
    {
        [TestMethod]
        public void Can_invoke_compile_sass_task()
        {
            // Arrange
            var projectFolder = Path.Combine(Path.GetTempPath(), "sassin-temp");
            if (Directory.Exists(projectFolder)) Directory.Delete(projectFolder, recursive: true);
            Directory.CreateDirectory(projectFolder);

            File.Copy(Sample.GetBasicSCSS().FullName, Path.Combine(projectFolder, Sample.File.BasicSCSS));

            var mockEngine = A.Fake<Microsoft.Build.Framework.IBuildEngine>();
            A.CallTo(() => mockEngine.ProjectFileOfTaskNode).Returns(Path.Combine(projectFolder, "sassin.proj"));

            var sut = new CompileSassFiles
            {
                BuildEngine = mockEngine,
                GenerateSourceMaps = true,
                Minify = true
            };

            // Act
            var success = sut.Execute();
            var generatedFiles = Directory.EnumerateFiles(projectFolder).Select(x => Path.GetFileName(x)).ToArray();

            // Asser
            success.ShouldBeTrue();
            generatedFiles.ShouldNotBeEmpty();
            generatedFiles.Length.ShouldBe(3);
        }
    }
}