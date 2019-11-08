using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Acklann.Sassin
{
    public class Sass
    {
        public static CompilerResult Compile(string sassFilePath, CompilerOptions options)
        {
            if (!File.Exists(sassFilePath)) throw new FileNotFoundException($"Could not find file at '{sassFilePath}'.");

            string compiler = Path.Combine(NodeJS.InstallationDirectory, "compiler.js");
            if (!File.Exists(compiler)) throw new FileNotFoundException($"Could not find file at '{compiler}'.");

            if (!string.IsNullOrEmpty(options.OutputDirectory) && !Directory.Exists(options.OutputDirectory))
                Directory.CreateDirectory(options.OutputDirectory);

            if (!string.IsNullOrEmpty(options.SourceMapDirectory) && !Directory.Exists(options.SourceMapDirectory))
                Directory.CreateDirectory(options.SourceMapDirectory);

            using (Process node = NodeJS.Execute($"/c node \"{compiler}\" \"{sassFilePath}\" {options.ToArgs()}"))
            {
                return new CompilerResult
                {
                    SourceFile = sassFilePath,
                    Success = (node.ExitCode == 0),
                    Errors = GetErrors(node.StandardError).ToArray(),
                    GeneratedFiles = GetGeneratedFiles(node.StandardOutput).ToArray()
                };
            }
        }

        public static IEnumerable<string> FindFiles(string fullPath)
        {
            if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException($"Could not find directory at '{fullPath}'.");

            return from x in Directory.EnumerateFiles(fullPath, "*.scss", SearchOption.AllDirectories)
                   where Path.GetFileName(x).StartsWith("_") == false
                   select x;
        }

        private static IEnumerable<string> GetGeneratedFiles(StreamReader reader)
        {
            JArray json; string line = null;

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
#if DEBUG
                System.Diagnostics.Debug.WriteLine(line);
#endif
                if (string.IsNullOrEmpty(line) || !line.StartsWith("[")) continue;

                json = JArray.Parse(line);
                return json.Values<string>();
            }

            return new string[0];
        }

        private static IEnumerable<CompilerError> GetErrors(StreamReader reader)
        {
            if (reader == null) yield break;

            JObject json; string line = null;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
#if DEBUG
                System.Diagnostics.Debug.WriteLine(line);
#endif
                if (string.IsNullOrEmpty(line) || !line.StartsWith("{")) continue;

                json = JObject.Parse(line);
                yield return new CompilerError(
                    json["message"].Value<string>(),
                    (json["file"]?.Value<string>() ?? default),
                    (json["line"]?.Value<int>() ?? default),
                    (json["column"]?.Value<int>() ?? default),
                    ((ErrorSeverity)(json["level"]?.Value<int>() ?? (int)ErrorSeverity.Error)),
                    (json["status"]?.Value<int>() ?? default)
                );
            }
        }
    }
}