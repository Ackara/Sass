using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
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
            foreach (var item in Directory.EnumerateFiles(NodeJS.InstallationDirectory, "*.js"))
            {
                File.Delete(item);
            }

            NodeJS.Install();
        }

        [DataTestMethod]
        [DynamicData(nameof(GetCompilierOptions), DynamicDataSourceType.Method)]
        public void Can_compile_sass_file(string label, int expectedFiles, CompilerOptions options)
        {
            // Arrange
            var cwd = Path.Combine(AppContext.BaseDirectory, "generated", label);
            if (Directory.Exists(cwd)) Directory.Delete(cwd, recursive: true);
            Directory.CreateDirectory(cwd);

            options.OutputDirectory = cwd;

            // Act
            var result = Sass.Compile(Sample.GetBasicSCSS().FullName, options);
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
            totalFiles.ShouldBe(expectedFiles);
            result.GeneratedFiles.Length.ShouldBe(expectedFiles);

            //Diff.Approve(builder);
        }

        // ==================== DATA ==================== //

        private static IEnumerable<object[]> GetCompilierOptions()
        {
            yield return new object[]{"css", 1, new CompilerOptions
            {
                Minify = true,
                AddSourceComments = false,
                GenerateSourceMaps = true,
                KeepIntermediateFiles = false
            }};
        }
    }
}