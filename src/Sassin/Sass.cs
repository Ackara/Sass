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
                    json["file"].Value<string>(),
                    (json["line"]?.Value<int>() ?? -1),
                    (json["column"]?.Value<int>() ?? -1),
                    ((ErrorLevel)(json["level"]?.Value<int>() ?? (int)ErrorLevel.Error)),
                    json["status"].Value<string>()
                );
            }
        }
    }
}