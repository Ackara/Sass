using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Acklann.Sassin
{
    [Guid(Metadata.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Metadata.Version, IconResourceID = 500)]
    [ProvideOptionPage(typeof(ConfigurationPage), Metadata.ProductName, ConfigurationPage.Catagory, 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _stillLoading = true;
            vs = (DTE2)await GetServiceAsync(typeof(EnvDTE.DTE));

            var commandService = (await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService);
            commandService.AddCommand(new OleMenuCommand(OnCompileSassFileCommandInvoked, null, OnCompileSassFileCommandStatusQueried, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.CompileSassFileCommandId)));
            commandService.AddCommand(new OleMenuCommand(OnInstallNugetPackageCommandInvoked, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.InstallNugetPackageCommandId)));
            commandService.AddCommand(new MenuCommand(OnGotoSettingCommandInvoked, new CommandID(Metadata.CmdSet.Guid, Metadata.CmdSet.GotoSettingsCommandId)));

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            GetDialogPage(typeof(ConfigurationPage));
            var outputWindow = (IVsOutputWindow)await GetServiceAsync(typeof(SVsOutputWindow));
            if (outputWindow != null)
            {
                var guid = new Guid("455e712c-1c5a-43b9-8c70-7f8e9e0ec4f6");
                outputWindow.CreatePane(ref guid, Metadata.ProductName, 1, 1);
                outputWindow.GetPane(ref guid, out IVsOutputWindowPane pane);

                _fileWatcher = new SassWatcher(this, pane);

                await NodeJS.InstallAsync((message, counter, goal) =>
                {
                    progress?.Report(new ServiceProgressData(message, message, counter, goal));
                    System.Diagnostics.Debug.WriteLine(message);
                    pane.Writeline(message);

                    if (counter >= goal)
                    {
                        _stillLoading = false;
                        _fileWatcher.Start();
                        pane.OutputStringThreadSafe("\n\n");
                    }
                });
            }

            TryInitializeSolution();
            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnAfterOpenSolution += OnSolutionOpened;
        }

        private bool TryInitializeSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _solution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (_solution != null)
            {
                ErrorHandler.ThrowOnFailure(_solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));
                return value is bool isOpen && isOpen;
            }

            return false;
        }

        // ==================== Event Handlers ==================== //

        private void OnCompileSassFileCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_stillLoading) return;

            if (vs != null
                && vs.TryGetSelectedFile(out EnvDTE.ProjectItem selectedFile)
                && selectedFile.FileNames[0].IsSassFile())
            {
                _solution.GetProjectOfUniqueName(selectedFile.ContainingProject.FullName, out IVsHierarchy hierarchy);
                _fileWatcher.Compile(selectedFile.FileNames[0], hierarchy);
            }
        }

        private void OnCompileSassFileCommandStatusQueried(object sender, EventArgs e)
        {
            if (_stillLoading) return;

            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand btn)
            {
                bool fileSelected = vs.TryGetSelectedFile(out EnvDTE.ProjectItem selectedFile);

                btn.Enabled = fileSelected && Helper.IsSassFile(selectedFile?.Name);
                btn.Text = string.Format("Compile {0}", (selectedFile?.Name ?? "Sass File"));
            }
        }

        private void OnInstallNugetPackageCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vs != null && vs.TryGetSelectedProject(out EnvDTE.Project project))
            {
                IComponentModel componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
                IVsPackageInstallerServices nuget = componentModel.GetService<IVsPackageInstallerServices>();
                IVsPackageInstaller installer = componentModel.GetService<IVsPackageInstaller>();
                EnvDTE.StatusBar status = vs.StatusBar;

                if (!nuget.IsPackageInstalled(project, nameof(Sassin)))
                    try
                    {
                        status.Text = $"{Metadata.ProductName} installing {nameof(Sassin)}...";
                        status.Animate(true, EnvDTE.vsStatusAnimation.vsStatusAnimationBuild);

                        installer.InstallPackage(null, project, nameof(Sassin), Convert.ToString(null), false);
                    }
                    catch { status.Text = $"{Metadata.ProductName} failed to install {nameof(Sassin)}."; }
                    finally { status.Animate(false, EnvDTE.vsStatusAnimation.vsStatusAnimationBuild); }
            }
        }

        private void OnGotoSettingCommandInvoked(object sender, EventArgs e)
        {
            ShowOptionPage(typeof(ConfigurationPage));
        }

        private void OnSolutionOpened(object sender, Microsoft.VisualStudio.Shell.Events.OpenSolutionEventArgs e)
        {
            if (e.IsNewSolution) TryInitializeSolution();
        }

        #region Backing Members

        internal DTE2 vs;
        private IVsSolution _solution;
        private SassWatcher _fileWatcher;
        private bool _stillLoading = true;

        #endregion Backing Members
    }
}