using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Acklann.Sassin
{
    public delegate void ProgressHandler(string message, int progress, int max);

    public class NodeJS
    {
        static NodeJS()
        {
            Assembly assembly = typeof(NodeJS).Assembly;

            InstallationDirectory = Path.Combine(Path.GetDirectoryName(assembly.Location));
            InstallationDirectory = Path.Combine(InstallationDirectory, "tools", assembly.GetName().Version.ToString());
        }

        public static readonly string InstallationDirectory;

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
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
#endif
                return false;
            }
            finally { npm.Dispose(); }
        }

        public static Process Execute(string command)
        {
            if (string.IsNullOrEmpty(command)) throw new ArgumentNullException(nameof(command));

            Process cmd = GetStartInfo(command);
            cmd.Start();
            cmd.WaitForExit();
            System.Diagnostics.Debug.WriteLine($"exit: {cmd.ExitCode}");
            return cmd;
        }

        public static void Install(ProgressHandler handler = default, bool overwrite = false)
        {
            int progress = 1, goal = (_dependencies.Length + 2);

            string modulesFolder = Path.Combine(InstallationDirectory, "node_modules");
            if (!Directory.Exists(modulesFolder))
                InstallModules(modulesFolder, handler, ref progress, goal);

            if (!Directory.EnumerateFiles(InstallationDirectory, "*.js").Any())
                ExtractBinaries(handler, ref progress, goal, overwrite);

            handler?.Invoke(string.Empty, goal, goal);
        }

        public static Task InstallAsync(ProgressHandler handler = default, bool overwrite = false)
            => Task.Run(() => { Install(handler, overwrite); });

        public static bool TryInstall(ProgressHandler handler = default, bool overwrite = false)
        {
            try
            {
                Install(handler, overwrite);
                return true;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }

            return false;
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

            System.Diagnostics.Debug.WriteLine($"{info.FileName}> {command}");
            return new Process() { StartInfo = info };
        }

        private static void InstallModules(string node_modules, ProgressHandler handler, ref int progress, int goal)
        {
            Process npm = null;

            try
            {
                npm = GetStartInfo();
                npm.StartInfo.WorkingDirectory = node_modules;
                if (!Directory.Exists(node_modules)) Directory.CreateDirectory(node_modules);

                foreach (string item in _dependencies)
                {
                    handler?.Invoke(string.Format(messageFormat, progress, goal, (progress / (float)goal), $"npm install {item}"), progress, goal);
                    progress++;

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

        private static void ExtractBinaries(ProgressHandler handler, ref int progress, int goal, bool overwrite = false)
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

                        handler?.Invoke(string.Format(messageFormat, progress, goal, (progress / (float)goal), $"extracting {name}"), progress, goal);
                        progress++;

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

        #region Backing Members

        private const string messageFormat = "Loading dependencies [{0}/{1} {2:0%}]; {3} ...";

        #endregion Backing Members
    }
}