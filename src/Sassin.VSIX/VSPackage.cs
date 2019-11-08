using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Acklann.Sassin;
using Microsoft.VisualStudio.Shell.Events;
using Constants = Microsoft.VisualStudio.Shell.Interop.Constants;
using Task = System.Threading.Tasks.Task;
using EnvDTE80;

namespace Acklann.Sassin
{
    [Guid(Symbols.Package.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : AsyncPackage
    {


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);


            _dte = (DTE2)await GetServiceAsync(typeof(EnvDTE.DTE));

            _dte?.StatusBar.Animate(true, Constants.SBAI_General);
            if (NodeJS.TryInstall(onProgressUpdate))
            {
                
            }

            _dte?.StatusBar.Animate(false, Constants.SBAI_General);
            void onProgressUpdate(string message, int progres, int goal)
            {
                _dte?.StatusBar.Progress(progres != goal, $"{Symbols.ProductName} {message} ...", progres, goal);
            }
        }

        private void OnSolutionLoaded(object sender, OpenSolutionEventArgs e = null)
        {

        }

        #region Backing Members

        private DTE2 _dte;
        private IVsSolution _solution;
        private ErrorListProvider _errorList;


        #endregion
    }
}
