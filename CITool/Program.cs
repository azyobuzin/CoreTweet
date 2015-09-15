using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CITool
{
    public class Program
    {
        public void Main(string[] args)
        {
            var version = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_VERSION");
            if (string.IsNullOrEmpty(version)) return;

            var projectFile = args[0];

            var j = JObject.Parse(File.ReadAllText(projectFile));
            j["version"] = version;

            File.WriteAllText(projectFile, j.ToString());
        }
    }
}
