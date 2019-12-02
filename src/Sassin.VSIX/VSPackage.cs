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
    [Guid(Symbols.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Symbols.Version, IconResourceID = 500)]
    [ProvideOptionPage(typeof(ConfigurationPage), Symbols.ProductName, ConfigurationPage.Catagory, 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            vs = (DTE2)await GetServiceAsync(typeof(EnvDTE.DTE));

            var commandService = (await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService);
            commandService.AddCommand(new OleMenuCommand(OnCompileSassFileCommandInvoked, null, OnCompileSassFileCommandStatusQueried, new CommandID(Symbols.CmdSet.Guid, Symbols.CmdSet.CompileSassFileCommandId)));
            commandService.AddCommand(new OleMenuCommand(OnInstallNugetPackageCommandInvoked, new CommandID(Symbols.CmdSet.Guid, Symbols.CmdSet.InstallNugetPackageCommandId)));
            commandService.AddCommand(new MenuCommand(OnGotoSettingCommandInvoked, new CommandID(Symbols.CmdSet.Guid, Symbols.CmdSet.GotoSettingsCommandId)));

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            GetDialogPage(typeof(ConfigurationPage));
            var outputWindow = (IVsOutputWindow)await GetServiceAsync(typeof(SVsOutputWindow));
            if (outputWindow != null)
            {
                var guid = new Guid("455e712c-1c5a-43b9-8c70-7f8e9e0ec4f6");
                outputWindow.CreatePane(ref guid, Symbols.ProductName, 1, 1);
                outputWindow.GetPane(ref guid, out IVsOutputWindowPane pane);

                _fileWatcher = new SassWatcher(this, pane);

                await NodeJS.InstallAsync((msg, counter, goal) =>
                {
                    progress?.Report(new ServiceProgressData(msg, null, counter, goal));
                    pane.OutputStringThreadSafe(msg + "\n");
                    System.Diagnostics.Debug.WriteLine(msg);
                    if (counter >= goal)
                    {
                        notReady = false;
                        _fileWatcher.Activate();
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
            if (notReady) return;

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
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand btn)
            {
                bool fileSelected = vs.TryGetSelectedFile(out EnvDTE.ProjectItem selectedFile);

                btn.Enabled = fileSelected && Helper.IsSassFile(selectedFile?.Name);
                btn.Text = string.Format("Compile {0}", (selectedFile?.Name ?? "File"));
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
                        status.Text = $"{Symbols.ProductName} installing {nameof(Sassin)}...";
                        status.Animate(true, EnvDTE.vsStatusAnimation.vsStatusAnimationBuild);

                        installer.InstallPackage(null, project, nameof(Sassin), Convert.ToString(null), false);
                    }
                    catch { status.Text = $"{Symbols.ProductName} failed to install {nameof(Sassin)}."; }
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
        private bool notReady = true;
        private IVsSolution _solution;
        private SassWatcher _fileWatcher;

        #endregion Backing Members
    }
}