using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Dnx.Compilation.CSharp;
using RestApisGen;

namespace LibAzyotter.Compiler.Preprocess
{
    public class RestApisCompileModule : ICompileModule
    {
        public void BeforeCompile(BeforeCompileContext context)
        {
            var apis = Directory.GetFiles(Path.Combine(context.ProjectContext.ProjectDirectory, "..", "ApiTemplates"))
               .Where(x => !x.Contains("test.api"))
               .Select(ApiParent.Parse);
            var writer = new StringWriter();
            RestApisCs.Generate(apis, writer);

            var targetFramework = context.ProjectContext.TargetFramework;
            var preprocessorSymbols = new List<string>(2);
            switch (targetFramework.Identifier)
            {
                case ".NETFramework":
                case "DNX":
                    preprocessorSymbols.Add("NET45");
                    break;
                case ".NETPlatform":
                    preprocessorSymbols.Add("DOTNET" + targetFramework.Version.Major + "_" + targetFramework.Version.Minor);
                    goto case "DNXCore";
                case "DNXCore":
                    preprocessorSymbols.Add("DOTNET");
                    break;
                default:
                    throw new Exception(context.ProjectContext.TargetFramework.FullName);
            }
            var parseOptions = new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols);
            context.Compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(writer.ToString(), parseOptions));
        }

        public void AfterCompile(AfterCompileContext context) { }
    }
}
