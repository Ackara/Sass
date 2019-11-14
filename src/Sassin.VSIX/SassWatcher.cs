using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Acklann.Sassin
{
    public class SassWatcher : IVsRunningDocTableEvents3
    {
        public SassWatcher(VSPackage package, IVsOutputWindowPane pane, EnvDTE.StatusBar statusBar)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            _vsOutWindow = pane ?? throw new ArgumentNullException(nameof(pane));
            _statusBar = statusBar ?? throw new ArgumentNullException(nameof(statusBar));
            _msbulidProjects = new Dictionary<string, Microsoft.Build.Evaluation.Project>();

            _runningDocumentTable = new RunningDocumentTable(package);
            _runningDocumentTable.Advise(this);

            _errorList = new ErrorListProvider(package)
            {
                ProviderName = $"{Symbols.ProductName} Error List",
                ProviderGuid = new Guid("6e63fa03-9f4e-47da-9cf9-5efd22799c28")
            };
        }

        internal void Compile(string documentPath, IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (hierarchy == null) throw new ArgumentNullException(nameof(hierarchy));

            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object objProj);
            string projectPath = (objProj as EnvDTE.Project)?.FullName;
            if (!File.Exists(projectPath) || !File.Exists(documentPath)) return;

            Microsoft.Build.Evaluation.Project config;

            if (_msbulidProjects.ContainsKey(projectPath))
                config = _msbulidProjects[projectPath];
            else
                _msbulidProjects.Add(projectPath, config = new Microsoft.Build.Evaluation.Project(projectPath));

            string[] documents;
            if (Path.GetFileName(documentPath).StartsWith("_"))
                documents = SassCompiler.FindFiles(Path.GetDirectoryName(projectPath)).ToArray();
            else
                documents = new string[] { documentPath };

            var options = new CompilerOptions
            {
                SourceMapDirectory = config.GetProperty("SassCompilerSourceMapDirectory")?.EvaluatedValue,
                OutputDirectory = config.GetProperty("SassCompilerOutputDirectory")?.EvaluatedValue,
                Suffix = config.GetProperty("SassCompilerOutputFileSuffix")?.EvaluatedValue,

                AddSourceComments = ConfigurationPage.ShouldAddSourceMapComment,
                GenerateSourceMaps = ConfigurationPage.ShouldGenerateSourceMap,
                Minify = ConfigurationPage.ShouldMinifyFile,
            };

            int n = documents.Length;
            for (int i = 0; i < n; i++)
            {
                CompilerResult result = SassCompiler.Compile(documents[i], options);
                foreach (CompilerError item in result.Errors)
                {
                    if (item.Severity == ErrorSeverity.Debug)
                        _vsOutWindow.OutputStringThreadSafe(string.Concat(item.Message, Environment.NewLine));
                    else
                        HandleError(item, hierarchy);
                }
            }
        }

        private void HandleError(CompilerError error, IVsHierarchy hierarchy)
        {
            ErrorTask task;
            bool shouldAdd = true;
            int n = _errorList.Tasks.Count;

            for (int i = 0; i < n; i++)
            {
                task = (ErrorTask)_errorList.Tasks[i];

                if (task.Document == error.File)
                {
                    _errorList.Tasks.RemoveAt(i);
                    n--;
                }

                if (task.Text == error.Message && task.Document == error.File && task.Line == error.Line && task.Column == error.Column)
                {
                    shouldAdd = false;
                }
            }

            if (shouldAdd) _errorList.Tasks.Add(new ErrorTask
            {
                Text = error.Message,
                HierarchyItem = hierarchy,
                Document = error.File,
                Line = (error.Line - 1),
                Column = error.Column,
                Category = TaskCategory.BuildCompile,
                ErrorCategory = ToCatetory(error.Severity)
            });
        }

        #region IVsRunningDocTableEvents3

        public int OnAfterSave(uint docCookie)
        {
            RunningDocumentInfo document = _runningDocumentTable.GetDocumentInfo(docCookie);
            string fileName = Path.GetFileName(document.Moniker);

            if (fileName.EndsWith(".scss", StringComparison.OrdinalIgnoreCase))
            {
                Compile(document.Moniker, document.Hierarchy);
            }

            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => VSConstants.S_OK;

        public int OnBeforeSave(uint docCookie) => VSConstants.S_OK;

        #endregion IVsRunningDocTableEvents3

        #region Backing Members

        private readonly IDictionary<string, Microsoft.Build.Evaluation.Project> _msbulidProjects;
        private readonly RunningDocumentTable _runningDocumentTable;
        private readonly ErrorListProvider _errorList;
        private readonly IVsOutputWindowPane _vsOutWindow;
        private readonly EnvDTE.StatusBar _statusBar;

        private static TaskErrorCategory ToCatetory(ErrorSeverity severity)
        {
            switch (severity)
            {
                default:
                case ErrorSeverity.Debug:
                    return TaskErrorCategory.Message;

                case ErrorSeverity.Warning:
                    return TaskErrorCategory.Warning;

                case ErrorSeverity.Error:
                    return TaskErrorCategory.Error;
            }
        }

        #endregion Backing Members
    }
}