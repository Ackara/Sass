using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Acklann.Sassin
{
    public class NodeJS
    {
        public static readonly string InstallationDirectory = Path.Combine(AppContext.BaseDirectory, "tools");

        private static readonly string[] _dependencies = new string[]
                {
            "node-sass@4.13.0", "csso@4.0.1", "multi-stage-sourcemap@0.3.1"
        };

        public static bool CheckInstallation()
        {
            Process npm = GetStartInfo("/c npm --version");

            try
            {
                npm.Start();
                npm.WaitForExit();
                return npm.ExitCode == 0;
            }
#if DEBUG
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
                return false;
            }
#else
            catch { return false; }
#endif
            finally { npm.Dispose(); }
        }

        public static void Install(bool overwrite = false)
        {
            string modulesFolder = Path.Combine(InstallationDirectory, "node_modules");
            if (!Directory.Exists(modulesFolder))
            {
                InstallModules();
                ExtractBinaries(overwrite);
            }
        }

        public static Process Execute(string command, bool doNotWait = false)
        {
            if (string.IsNullOrEmpty(command)) throw new ArgumentNullException(nameof(command));

            Process cmd = GetStartInfo(command);
            cmd.Start();
            if (doNotWait == false) cmd.WaitForExit();
            return cmd;
        }

        private static Process GetStartInfo(string command = null)
        {
            var info = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = command,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            return new Process() { StartInfo = info };
        }

        private static void ExtractBinaries(bool overwrite = false)
        {
            Assembly assembly = typeof(NodeJS).Assembly;
            string extension;

            foreach (string name in assembly.GetManifestResourceNames())
                switch (extension = Path.GetExtension(name).ToLowerInvariant())
                {
                    case ".js":
                    case ".json":
                        string baseName = Path.GetFileNameWithoutExtension(name);
                        string fullPath = Path.Combine(InstallationDirectory, $"{baseName.Substring(baseName.LastIndexOf('.') + 1)}{extension}");

                        if (baseName.EndsWith("-lock.", StringComparison.OrdinalIgnoreCase)) continue;
                        else if (overwrite || !File.Exists(fullPath))
                            using (Stream stream = assembly.GetManifestResourceStream(name))
                            using (var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                            {
                                stream.CopyTo(file);
                                stream.Flush();
                            }
                        break;
                }
        }

        private static void InstallModules()
        {
            Process npm = null;

            try
            {
                npm = GetStartInfo();
                npm.StartInfo.WorkingDirectory = InstallationDirectory;
                if (!Directory.Exists(InstallationDirectory)) Directory.CreateDirectory(InstallationDirectory);

                foreach (string item in _dependencies)
                {
                    npm.StartInfo.Arguments = $"/c npm install {item} --save-dev";
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
    }
}