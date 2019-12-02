using Microsoft.Build.Framework;
using System.IO;
using System.Linq;

namespace Acklann.Sassin.MSBuild
{
    public class CompileSassFiles : ITask
    {
        public CompileSassFiles()
        {
            Minify = GenerateSourceMaps = AddSourceComments = true;
        }

        public string ProjectDirectory;

        public string OptionsFile { get; set; }
        public string OutputDirectory { get; set; }
        public string SourceMapDirectory { get; set; }

        public bool Minify { get; set; }
        public bool GenerateSourceMaps { get; set; }
        public bool AddSourceComments { get; set; }

        public bool Execute()
        {
            if (string.IsNullOrEmpty(ProjectDirectory)) ProjectDirectory = Path.GetDirectoryName(BuildEngine.ProjectFileOfTaskNode);
            if (!Directory.Exists(ProjectDirectory)) throw new DirectoryNotFoundException($"Could not find directory at '{ProjectDirectory}'.");

            NodeJS.Install((message, _, __) =>
            {
                Message($"{nameof(CompileSassFiles)}: {message}", MessageImportance.High);
            });

            var options = new CompilerOptions
            {
                Minify = Minify,
                OutputDirectory = OutputDirectory,
                AddSourceComments = AddSourceComments,
                GenerateSourceMaps = GenerateSourceMaps,
                SourceMapDirectory = SourceMapDirectory,
                ConfigurationFile = (File.Exists(OptionsFile) ? OptionsFile : null)
            };

            int failures = 0;
            foreach (string sassFile in SassCompiler.GetSassFiles(ProjectDirectory))
            {
                CompilerResult result = SassCompiler.Compile(sassFile, options);

                foreach (CompilerError err in result.Errors) LogError(err);
                if (result.Success) LogResult(result); else failures++;
            }

            return failures == 0;
        }

        private void Message(string message, MessageImportance importance = MessageImportance.Normal)
        {
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, nameof(CompileSassFiles), importance));
        }

        private void LogResult(CompilerResult result)
        {
            string rel(string x) => (x == null ? "null" : string.Format("{0}\\{1}", Path.GetDirectoryName(x).Replace(ProjectDirectory, string.Empty), Path.GetFileName(x)));

            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
                string.Format(
                    "sass -> in:{0}  out:{1}  elapse:{2}",

                    rel(result.SourceFile),
                    (result.GeneratedFiles.Length > 1 ? string.Format("[{0}]", string.Join(", ", result.GeneratedFiles.Select(x => rel(x)))) : rel(result.OutputFile)),
                    result.Elapse.ToString("hh\\:mm\\:ss\\.fff")),
                null,
                nameof(CompileSassFiles),
                MessageImportance.Normal
                ));
        }

        private void LogError(CompilerError error)
        {
            switch (error.Severity)
            {
                case ErrorSeverity.Error:
                    BuildEngine.LogErrorEvent(new BuildErrorEventArgs(
                        string.Empty,
                        $"{error.StatusCode}",
                        error.File,
                        error.Line,
                        error.Column,
                        0, 0,
                        error.Message,
                        string.Empty,
                        nameof(CompileSassFiles)));
                    break;

                case ErrorSeverity.Warning:
                    BuildEngine.LogWarningEvent(new BuildWarningEventArgs(
                        string.Empty,
                        $"{error.StatusCode}",
                        error.File,
                        error.Line,
                        error.Column,
                        0, 0,
                        error.Message,
                        string.Empty,
                        nameof(CompileSassFiles)));
                    break;
            }
        }

        #region ITask

        public ITaskHost HostObject { get; set; }

        public IBuildEngine BuildEngine { get; set; }

        #endregion ITask
    }
}