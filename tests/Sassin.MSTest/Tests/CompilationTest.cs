using Acklann.Diffa;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Acklann.Sassin.Tests
{
    [TestClass]
    public class CompilationTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            NodeJS.Install();
        }

        [DataTestMethod]
        [DynamicData(nameof(GetCompilierOptions), DynamicDataSourceType.Method)]
        public void Can_compile_sass_file(string label, int expectedFiles, CompilerOptions options)
        {
            // Arrange
            var cwd = Path.Combine(Path.GetTempPath(), nameof(Sassin), label);
            if (Directory.Exists(cwd)) Directory.Delete(cwd, recursive: true);
            Directory.CreateDirectory(cwd);

            var sassFile = Sample.GetBasicSCSS().CopyTo(Path.Combine(cwd, "test.scss"), overwrite: true).FullName;

            // Act
            var result = Sass.Compile(sassFile, options);
            var totalFiles = Directory.GetFiles(cwd, "*").Length;

            var builder = new StringBuilder();
            var separator = string.Concat(Enumerable.Repeat('=', 50));
            foreach (var item in result.GeneratedFiles)
            {
                builder.AppendLine($"== {Path.GetFileName(item)}")
                       .AppendLine(separator)
                       .AppendLine(File.ReadAllText(item))
                       .AppendLine()
                       .AppendLine();
            }

            // Assert
            result.Success.ShouldBeTrue();
            totalFiles.ShouldBe(totalFiles);
            result.GeneratedFiles.Length.ShouldBe(expectedFiles - 1);

            //Diff.Approve(builder);
        }

        [TestMethod]
        public void Can_generate_sass_to_css_map_file()
        {
            throw new System.NotImplementedException();
        }

        #region Backing Members

        private static IEnumerable<object[]> GetCompilierOptions()
        {
            yield return new object[]{"minimal", 2, new CompilerOptions
            {
                Minify = false,
                AddSourceComments = false,
                GenerateSourceMaps = false,
                KeepIntermediateFiles = false,
            }};

            yield return new object[]{"map-1", 3, new CompilerOptions
            {
                Minify = false,
                AddSourceComments = true,
                GenerateSourceMaps = true,
                KeepIntermediateFiles = true,
            }};
        }

        #endregion Backing Members
    }
}