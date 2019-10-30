using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Acklann.Sassin
{
    public class NodeJsController
    {
        private static readonly string[] _dependencies = new string[]
        {
            "node-sass@4.13.0", "csso@4.0.1", "multi-stage-sourcemap@0.3.1"
        };

        public static void Install()
        {
            if (string.IsNullOrEmpty(_nodejs)) ExtractBinaries();

            string modulesFolder = Path.Combine(WorkingDirectory, "node_modules");
            if (Directory.Exists(modulesFolder)) return;

            Process npm = null;

            try
            {
                npm = new Process() { StartInfo = GetStartInfo() };
                foreach (string item in _dependencies)
                {
                    npm.StartInfo.WorkingDirectory = WorkingDirectory;
                    npm.StartInfo.Arguments = $"npm install {item} --save-dev";

                    npm.Start();
                    npm.WaitForExit();

                    if (npm.ExitCode != 0)
                    {
                        throw new Exception($"Unable to install {item}.");
                    }
                }
            }
            finally { npm?.Dispose(); }
        }

        public static Process Execute(string command)
        {
            if (string.IsNullOrEmpty(command)) throw new ArgumentNullException(nameof(command));

            ProcessStartInfo info = GetStartInfo();
            info.Arguments = command;

            return Process.Start(GetStartInfo());
        }

        public static bool CheckInstallation()
        {
            if (string.IsNullOrEmpty(_nodejs))
            {
                _nodejs = ResolveNodeJsPath();
            }

            return string.IsNullOrEmpty(_nodejs) == false;
        }

        internal static void ExtractBinaries(bool overwrite = false)
        {
            Assembly assembly = typeof(NodeJsController).Assembly;
            bool isFile(string filePath) => !(filePath.EndsWith("\\") || filePath.EndsWith("/"));

            foreach (string name in assembly.GetManifestResourceNames())
                switch (Path.GetExtension(name).ToLowerInvariant())
                {
                    case ".js":

                        break;

                    case ".zip":
                        using (Stream stream = assembly.GetManifestResourceStream(name))
                        using (var archive = new ZipArchive(stream))
                        {
                            string destination, dir;

                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (isFile(entry.FullName))
                                {
                                    destination = Path.Combine(WorkingDirectory, entry.FullName);
                                    if (overwrite || !File.Exists(destination))
                                    {
                                        dir = Path.GetDirectoryName(destination);
                                        if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);
                                        entry.ExtractToFile(destination, overwrite: true);
                                    }
                                }
                            }
                        }
                        break;
                }

            _nodejs = ResolveNodeJsPath();
        }

        internal static string ResolveNodeJsPath()
        {
            if (!Directory.Exists(WorkingDirectory)) Directory.CreateDirectory(WorkingDirectory);
            string fullPath = Directory.EnumerateFiles(WorkingDirectory, "node.*", SearchOption.AllDirectories).FirstOrDefault();

            if (!File.Exists(fullPath))
                using (Process node = Execute("--version"))
                {
                    fullPath = (node.ExitCode == 0 ? "node" : null);
                }

            return fullPath;
        }

        private static ProcessStartInfo GetStartInfo()
        {
            var info = new ProcessStartInfo
            {
                FileName = _nodejs,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            return info;
        }

        #region Backing Members

        public static readonly string WorkingDirectory = Path.Combine(AppContext.BaseDirectory, "tools");
        private static string _nodejs = null;

        #endregion Backing Members
    }
}