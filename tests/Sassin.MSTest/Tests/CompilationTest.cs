using Acklann.Diffa;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Acklann.Sassin.Tests
{
    [TestClass]
    public class CompilationTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext _)
        {
            if (Directory.Exists(NodeJS.InstallationDirectory))
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

            Diff.Approve(builder, ".txt", label);
        }

        [DataTestMethod]
        [DynamicData(nameof(GetInvalidFiles), DynamicDataSourceType.Method)]
        public void Can_detect_sass_errors(string documentPath, int errorLine)
        {
            // Arrange
            var cwd = Path.Combine(AppContext.BaseDirectory, "generated", "errors");
            if (Directory.Exists(cwd)) Directory.Delete(cwd, recursive: true);
            Directory.CreateDirectory(cwd);

            var options = new CompilerOptions
            {
                Minify = false,
                OutputDirectory = cwd,
                GenerateSourceMaps = false,
            };

            // Act
            var result = Sass.Compile(documentPath, options);
            var error = result.Errors.FirstOrDefault();

            // Assert
            result.Success.ShouldBeFalse();
            result.Errors.ShouldNotBeEmpty();

            error.Line.ShouldBe(errorLine);
            error.Column.ShouldBeGreaterThan(0);
            error.StatusCode.ShouldBeGreaterThan(0);
            error.Message.ShouldNotBeNullOrEmpty();
        }

        [TestMethod]
        public void Can_find_sass_files_within_folder()
        {
            // Arrange
            var folder = Sample.DirectoryName;

            // Act
            var result = Sass.FindFiles(folder);

            // Assert
            result.ShouldNotBeEmpty();
            result.ShouldAllBe(x => x.EndsWith(".scss"));
            result.ShouldAllBe(x => Path.GetFileName(x).StartsWith('_') == false);
        }

        // ==================== DATA ==================== //

        private static IEnumerable<object[]> GetCompilierOptions()
        {
            yield return new object[]{"css", 1, new CompilerOptions
            {
                Minify = false,
                AddSourceComments = false,
                GenerateSourceMaps = false
            }};

            yield return new object[]{"css-map", 2, new CompilerOptions
            {
                Minify = false,
                Suffix = string.Empty,
                AddSourceComments = true,
                GenerateSourceMaps = true
            }};

            yield return new object[]{"css-min-map", 2, new CompilerOptions
            {
                Minify = true,
                AddSourceComments = false,
                GenerateSourceMaps = true
            }};
        }

        private static IEnumerable<object[]> GetInvalidFiles()
        {
            var pattern = new Regex(@"\d+");
            foreach (string file in Directory.EnumerateFiles(Sample.DirectoryName, "error-*.scss"))
            {
                Match match = pattern.Match(Path.GetFileNameWithoutExtension(file));
                if (match.Success && int.TryParse(match.Value, out int lineNo))
                {
                    yield return new object[] { file, lineNo };
                }
            }
        }
    }
}