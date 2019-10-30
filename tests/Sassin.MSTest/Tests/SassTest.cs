using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using System.IO;

namespace Acklann.Sassin.Tests
{
    [TestClass]
    public class SassTest
    {
        [DataTestMethod]
        [DynamicData(nameof(GetCompilierOptions), DynamicDataSourceType.Method)]
        public void Can_compile_sass_file(string label, CompilerOptions options)
        {
            // Arrange
            var cwd = Path.Combine(Path.GetTempPath(), nameof(Sassin), label);
            if (Directory.Exists(cwd)) Directory.Delete(cwd, recursive: true);
            Directory.CreateDirectory(cwd);

            var sassFile = Sample.GetBasicSCSS().CopyTo(Path.Combine(cwd, "test.scss"), overwrite: true).FullName;

            // Act
            var results = Sass.Compile(sassFile, options);

            // Assert
            results.Success.ShouldBeTrue();
        }

        [TestMethod]
        public void Can_generate_sass_to_css_map_file()
        {
            throw new System.NotImplementedException();
        }

        #region Backing Members

        private static IEnumerable<object[]> GetCompilierOptions()
        {
            yield return new object[]{"minimal", new CompilerOptions
            {
                Minify = false,
                AddSourceComments = false,
                GenerateSourceMaps = false,
                KeepIntermediateFiles = false,
            }};

            yield return new object[]{"map-1", new CompilerOptions
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