using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Acklann.Sassin
{
	public static class Helper
	{
        public static string GetSelectedFile(this EnvDTE80.DTE2 dte)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (dte.SelectedItems != null)
                foreach (EnvDTE.SelectedItem item in dte.SelectedItems)
                {
                    if (item?.ProjectItem.FileCount > 0)
                    {
                        return item.ProjectItem.FileNames[0];
                    }
                }

            return null;
        }
    }
}