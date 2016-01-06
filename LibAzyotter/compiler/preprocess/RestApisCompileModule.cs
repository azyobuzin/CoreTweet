using System;
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
             var apis = Directory.GetFiles(Path.Combine(context.ProjectContext.ProjectDirectory,"..", "ApiTemplates"))
                .Where(x => !x.Contains("test.api"))
                .Select(ApiParent.Parse);
             var writer = new StringWriter();
             RestApisCs.Generate(apis, writer);
             
             string preprocessorSymbol;
             switch (context.ProjectContext.TargetFramework.Identifier)
             {
                 case ".NETFramework":
                 case "DNX":
                    preprocessorSymbol = "NET45";
                    break;
                case ".NETPlatform":
                case "DNXCore":
                    preprocessorSymbol = "DOTNET";
                    break;
                default:
                    throw new Exception(context.ProjectContext.TargetFramework.FullName);
             }
             var parseOptions = new CSharpParseOptions(preprocessorSymbols: new[] { preprocessorSymbol });
             context.Compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(writer.ToString(), parseOptions));
        }

        public void AfterCompile(AfterCompileContext context) { }
    }
}
