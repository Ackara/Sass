using System;
using System.IO;

namespace Acklann.Sassin
{
    public static class Helper
    {
        public static bool IsSassFile(this string filename)
        {
            return filename?.EndsWith(".scss", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public static bool SamePath(string a, string b)
        {
            return string.Equals(
                a.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar),
                b.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase
                );
        }

        public static void Writeline(this Microsoft.VisualStudio.Shell.Interop.IVsOutputWindowPane pane, string message, params object[] args)
        {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            pane?.OutputStringThreadSafe(string.Format(string.Concat(message, '\n'), args));
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }

        public static bool TryGetSelectedFile(this EnvDTE80.DTE2 dte, out EnvDTE.ProjectItem file)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            file = null;
            if (dte.SelectedItems != null)
                foreach (EnvDTE.SelectedItem item in dte.SelectedItems)
                {
                    if (item.ProjectItem?.FileCount > 0)
                    {
                        file = item.ProjectItem;
                        return true;
                    }
                }

            return false;
        }

        public static bool TryGetSelectedProject(this EnvDTE80.DTE2 dte, out EnvDTE.Project project)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            string selectedFile = null;
            if (dte.SelectedItems != null)
                foreach (EnvDTE.SelectedItem item in dte.SelectedItems)
                {
                    if (item.Project != null)
                    {
                        selectedFile = item.Project.FullName;
                        break;
                    }
                }

            if (string.IsNullOrEmpty(selectedFile))
                foreach (string relativePath in (Array)dte.Solution.SolutionBuild.StartupProjects)
                {
                    string rootDir = Path.GetDirectoryName(dte.Solution.FullName);
                    selectedFile = Path.Combine(rootDir, relativePath);
                    break;
                }

            project = dte.Solution.FindProjectItem(selectedFile)?.ContainingProject;
            return project != null;
        }
    }
}