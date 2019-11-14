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
                commandService.AddCommand(new OleMenuCommand(OnCompileSassFileCommandInvoked, null, OnCompileSassFileCommandStatusQueried, new CommandID(Symbols.CmdSet.Guid, Symbols.CmdSet.CompileSassFileCommandId)));
                commandService.AddCommand(new OleMenuCommand(OnInstallNugetPackageCommandInvoked, new CommandID(Symbols.CmdSet.Guid, Symbols.CmdSet.InstallNugetPackageCommandId)));
                commandService.AddCommand(new MenuCommand(OnGotoSettingCommandInvoked, new CommandID(Symbols.CmdSet.Guid, Symbols.CmdSet.GotoSettingsCommandId)));

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var outputWindow = (IVsOutputWindow)await GetServiceAsync(typeof(SVsOutputWindow));
                if (outputWindow != null)
                {
                    var guid = new Guid("455e712c-1c5a-43b9-8c70-7f8e9e0ec4f6");
                    outputWindow.CreatePane(ref guid, Symbols.ProductName, 1, 1);
                    outputWindow.GetPane(ref guid, out IVsOutputWindowPane pane);

                    _fileWatcher = new SassWatcher(this, pane, VS?.StatusBar);
                }

                TryInitializeSolution();
                Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnAfterOpenSolution += OnSolutionOpened;
                return;
            }

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            VS.StatusBar.Text = $"{nameof(Symbols.ProductName)} is ready.";
            System.Windows.Forms.MessageBox.Show($"{Symbols.ProductName} was not loaded; Node Package Manager (NPM) is not installed on this machine.");
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

            if (VS != null
                && VS.TryGetSelectedFile(out EnvDTE.ProjectItem selectedFile)
                && selectedFile.FileNames[0].IsSassFile())
            {
                _solution.GetProjectOfUniqueName(selectedFile.ContainingProject.FullName, out IVsHierarchy hierarchy);
                _fileWatcher.Compile(selectedFile.FileNames[0], hierarchy);
            }
        }

        private void OnCompileSassFileCommandStatusQueried(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand command)
            {
                command.Visible = (
                    VS != null
                    && VS.TryGetSelectedFile(out EnvDTE.ProjectItem selectedFile)
                    && selectedFile.FileNames[0].IsSassFile());
            }
        }

        private void OnInstallNugetPackageCommandInvoked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (VS != null && VS.TryGetSelectedProject(out EnvDTE.Project project))
            {
                IComponentModel componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
                IVsPackageInstallerServices nuget = componentModel.GetService<IVsPackageInstallerServices>();
                IVsPackageInstaller installer = componentModel.GetService<IVsPackageInstaller>();
                EnvDTE.StatusBar status = VS.StatusBar;

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

        internal DTE2 VS;
        private IVsSolution _solution;
        private SassWatcher _fileWatcher;

        #endregion Backing Members
    }
}