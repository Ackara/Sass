using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Linq;

namespace Acklann.Sassin
{
    public class SassWatcher : IVsRunningDocTableEvents3
    {
        public SassWatcher(VSPackage package, IVsOutputWindowPane pane)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            _vsOutWindow = pane ?? throw new ArgumentNullException(nameof(pane));

            _runningDocumentTable = new RunningDocumentTable(package);
            _runningDocumentTable.Advise(this);

            _errorList = new ErrorListProvider(package)
            {
                ProviderName = $"{Metadata.ProductName} Error List",
                ProviderGuid = new Guid("6e63fa03-9f4e-47da-9cf9-5efd22799c28")
            };
        }

        public void Start()
        {
            _stillLoading = false;
        }

        public async void Compile(string documentPath, IVsHierarchy hierarchy)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (hierarchy == null) return;

            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object objProj);
            string projectPath = (objProj as EnvDTE.Project)?.FullName;
            if (!File.Exists(projectPath) || !File.Exists(documentPath)) return;

            string[] documents;
            if (Path.GetFileName(documentPath).StartsWith("_"))
                documents = SassCompiler.GetSassFiles(Path.GetDirectoryName(projectPath)).ToArray();
            else
                documents = new string[] { documentPath };

            string configPath = Path.Combine(Path.GetDirectoryName(projectPath), ConfigurationPage.ConfigurationFileDefaultName);
            var options = new CompilerOptions
            {
                ConfigurationFile = (File.Exists(configPath) ? configPath : null),
                AddSourceComments = ConfigurationPage.ShouldAddSourceMapComment,
                GenerateSourceMaps = ConfigurationPage.ShouldGenerateSourceMap,
                Minify = ConfigurationPage.ShouldMinifyFile
            };

            int n = documents.Length;
            for (int i = 0; i < n; i++)
            {
                CompilerResult result = await SassCompiler.CompileAsync(documents[i], options);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ShowOutput(result, Path.GetDirectoryName(projectPath));
                ShowErrors(documents[i], result.Errors, hierarchy);
            }
        }

        private void ShowErrors(string sourceFile, CompilerError[] result, IVsHierarchy hierarchy)
        {
            if (ConfigurationPage.ShouldShowErrors == false) return;

            string document;
            int nErrors = _errorList.Tasks.Count;

            // Removing the errors for the document and all parital files.
            for (int i = 0; i < nErrors; i++)
            {
                document = _errorList.Tasks[i].Document;
                if (Helper.SamePath(document, sourceFile) || Path.GetFileName(document).StartsWith("_"))
                {
                    _errorList.Tasks.RemoveAt(i);
                    nErrors--; i--;
                }
            }

            CompilerError error;
            nErrors = result.Length;
            for (int i = 0; i < nErrors; i++)
            {
                error = result[i];
                if (error.Severity == ErrorSeverity.Info)
                    _vsOutWindow.Writeline(error.Message);
                else
                    _errorList.Tasks.Add(ToError(error, hierarchy));
            }
        }

        private void ShowOutput(CompilerResult result, string projectFolder)
        {
            string rel(string x) => (x == null ? "null" : string.Format("{0}\\{1}", Path.GetDirectoryName(x)?.Replace(projectFolder, string.Empty), Path.GetFileName(x)));

            _vsOutWindow.Writeline(
                "sass -> in:{0}  out:{1}  elapse:{2}",

                rel(result.SourceFile),
                (result.GeneratedFiles.Length > 1 ? string.Format("[{0}]", string.Join(" + ", result.GeneratedFiles.Select(x => rel(x)))) : rel(result.OutputFile)),
                result.Elapse.ToString("hh\\:mm\\:ss\\.fff")
                );
        }

        #region IVsRunningDocTableEvents3

        public int OnAfterSave(uint docCookie)
        {
            if (_stillLoading) return VSConstants.S_OK;

            RunningDocumentInfo document = _runningDocumentTable.GetDocumentInfo(docCookie);
            string fileName = Path.GetFileName(document.Moniker);

            if (fileName.EndsWith(".scss", StringComparison.OrdinalIgnoreCase))
            {
                Compile(document.Moniker, document.Hierarchy);
            }
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => VSConstants.S_OK;

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

        public int OnBeforeSave(uint docCookie) => VSConstants.S_OK;

        #endregion IVsRunningDocTableEvents3

        #region Backing Members

        private readonly Guid _textViewGuid = new Guid("{7651A703-06E5-11D1-8EBD-00A0C90F26EA}");
        private readonly RunningDocumentTable _runningDocumentTable;
        private readonly IVsOutputWindowPane _vsOutWindow;
        private readonly ErrorListProvider _errorList;
        private bool _stillLoading = true;

        private static TaskErrorCategory ToCatetory(ErrorSeverity severity)
        {
            switch (severity)
            {
                default:
                case ErrorSeverity.Info:
                    return TaskErrorCategory.Message;

                case ErrorSeverity.Warning:
                    return TaskErrorCategory.Warning;

                case ErrorSeverity.Error:
                    return TaskErrorCategory.Error;
            }
        }

        private ErrorTask ToError(CompilerError error, IVsHierarchy hierarchy)
        {
            var task = new ErrorTask
            {
                Text = error.Message,
                HierarchyItem = hierarchy,
                Document = error.File,
                Line = (error.Line - 1),
                Column = error.Column,
                Category = TaskCategory.BuildCompile,
                ErrorCategory = ToCatetory(error.Severity)
            };
            task.Navigate += GotoLine;
            return task;
        }

        private void GotoLine(object sender, EventArgs e)
        {
            if (sender is ErrorTask task)
            {
                task.Line++;
                _errorList.Navigate(task, _textViewGuid);
                task.Line--;
            }
        }

        #endregion Backing Members
    }
}