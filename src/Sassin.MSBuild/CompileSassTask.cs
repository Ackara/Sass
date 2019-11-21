using Microsoft.Build.Framework;
using System.IO;
using System.Linq;

namespace Acklann.Sassin.MSBuild
{
    public class CompileSassTask : ITask
    {
        public CompileSassTask()
        {
            Suffix = string.Empty;
            Minify = GenerateSourceMaps = AddSourceComments = true;
        }

        public string ProjectDirectory { get; set; }

        public bool Minify { get; set; }
        public string Suffix { get; set; }
        public string OutputDirectory { get; set; }

        public string SourceMapDirectory { get; set; }
        public bool GenerateSourceMaps { get; set; }
        public bool AddSourceComments { get; set; }

        public bool Execute()
        {
            if (string.IsNullOrEmpty(ProjectDirectory)) ProjectDirectory = Path.GetDirectoryName(BuildEngine.ProjectFileOfTaskNode);
            if (!Directory.Exists(ProjectDirectory)) throw new DirectoryNotFoundException($"Could not find directory at '{ProjectDirectory}'.");

            NodeJS.Install((message, _, __) =>
            {
                Message($"{nameof(CompileSassTask)}: {message}", MessageImportance.High);
            });

            var options = new CompilerOptions
            {
                Minify = Minify,
                Suffix = Suffix,
                OutputDirectory = OutputDirectory,
                AddSourceComments = AddSourceComments,
                GenerateSourceMaps = GenerateSourceMaps,
                SourceMapDirectory = SourceMapDirectory
            };

            int failures = 0;
            foreach (string sassFile in SassCompiler.FindFiles(ProjectDirectory))
            {
                CompilerResult result = SassCompiler.Compile(sassFile, options);

                if (result.Success) LogResult(result); else failures++;
                foreach (CompilerError err in result.Errors) LogError(err);
            }

            return failures == 0;
        }

        private void Message(string message, MessageImportance importance = MessageImportance.Normal)
        {
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, nameof(CompileSassTask), importance));
        }

        private void LogResult(CompilerResult result)
        {
            string rel(string x) => string.Format("{0}\\{1}", Path.GetDirectoryName(x).Replace(ProjectDirectory, string.Empty), Path.GetFileName(x));

            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
                string.Format(
                    "sass -> in:{0}  out:{1}  elapse:{2}",

                    rel(result.SourceFile),
                    (result.GeneratedFiles.Length > 1 ? string.Format("[{0}]", string.Join(", ", result.GeneratedFiles.Select(x => rel(x)))) : rel(result.OutputFile)),
                    result.Elapse.ToString("hh\\:mm\\:ss\\.fff")),
                null,
                nameof(CompileSassTask),
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
                        nameof(CompileSassTask)));
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
                        nameof(CompileSassTask)));
                    break;
            }
        }

        #region ITask

        public ITaskHost HostObject { get; set; }

        public IBuildEngine BuildEngine { get; set; }

        #endregion ITask
    }
}