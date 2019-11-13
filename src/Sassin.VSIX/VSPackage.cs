using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Acklann.Sassin
{
    [Guid(Symbols.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [InstalledProductRegistration("#110", "#112", "0.0.1", IconResourceID = 400)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideOptionPage(typeof(ConfigurationPage), Symbols.ProductName, "General", 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            bool hasDependencies = NodeJS.CheckInstallation();

            if (hasDependencies)
            {
                NodeJS.Install((msg, _, __) => { System.Diagnostics.Debug.WriteLine(msg); });

                VS = (DTE2)await GetServiceAsync(typeof(EnvDTE.DTE));

                var commandService = (await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService);
                commandService.AddCommand(new OleMenuCommand(OnCompileSassFileCommandInvoked, new CommandID(Symbols.CmdSet.Guid, Symbols.CmdSet.CompileSassFileCommandId)));

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var outputWindow = (IVsOutputWindow)await GetServiceAsync(typeof(SVsOutputWindow));
                if (outputWindow != null)
                {
                    var guid = new Guid("455e712c-1c5a-43b9-8c70-7f8e9e0ec4f6");
                    outputWindow.CreatePane(ref guid, Symbols.ProductName, 1, 1);
                    outputWindow.GetPane(ref guid, out IVsOutputWindowPane pane);

                    _fileWatcher = new SassWatcher(this, pane, VS?.StatusBar);
                }

                return;
            }

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            System.Windows.Forms.MessageBox.Show($"{Symbols.ProductName} was not loaded; Node Package Manager (NPM) is not installed on this machine.");
        }

        // ==================== Event Handlers ==================== //

        private void OnCompileSassFileCommandInvoked(object sender, EventArgs e)
        {
            string selectedFile = VS.GetSelectedFile();
        }

        #region Backing Members

        internal DTE2 VS;
        private SassWatcher _fileWatcher;

        #endregion Backing Members
    }
}