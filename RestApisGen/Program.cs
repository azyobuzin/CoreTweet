using System;
using System.IO;
using System.Linq;

namespace RestApisGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var basePath = args[0];

            Console.WriteLine("Reading API templates");
            var apis = Directory.GetFiles(Path.Combine(basePath, "ApiTemplates")).Where(x => !x.Contains("test.api"))
                .Select(ApiParent.Parse);

            Console.WriteLine("Generating RestApis.cs");
            using (var writer = File.CreateText(Path.Combine(basePath, "CoreTweet.Shared", "RestApis.cs")))
            {
                RestApisCs.Generate(apis, writer);
            }
        }
    }
}
