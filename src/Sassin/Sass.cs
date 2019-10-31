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

            using (Process node = NodeJS.Execute($"/c node \"{compiler}\" \"{sassFilePath}\" {options}"))
            {
                return new CompilerResult
                {
                    SourceFile = sassFilePath,
                    Success = (node.ExitCode == 0),
                    Errors = GetErrors(node.StandardError),
                    GeneratedFiles = GetGeneratedFiles(node.StandardOutput).ToArray()
                };
            }
        }

        private static IEnumerable<string> GetGeneratedFiles(StreamReader reader)
        {
            JArray json; string line = null;

            while (reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("[")) continue;

                json = JArray.Parse(line);
                return json.Values<string>();
            }

            return new string[0];
        }

        private static CompilerError[] GetErrors(StreamReader reader)
        {
            if (reader == null) return new CompilerError[0];

            JObject json; string line = null;
            var errors = new List<CompilerError>();
            while (reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("{")) continue;

                json = JObject.Parse(line);
                errors.Add(new CompilerError
                (
                    json["message"].Value<string>(),
                    json["file"].Value<string>(),
                    (json["line"]?.Value<int>() ?? -1),
                    (json["column"]?.Value<int>() ?? -1),
                    ((ErrorLevel)json["level"].Value<int>()),
                    json["status"].Value<string>()
                ));
            }

            return errors.ToArray();
        }
    }
}