using System;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using HarfBuzzSharp;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MethodRefFrontend.Services;

public static class FileIndexing
{
    public static async Task IndexFilesAsync(string path, ChannelWriter<string> outputChannel)
    {
        var files = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories);
        
        await Task.WhenAll(files.AsParallel().Select(async file =>
        {
            using var reader = new StreamReader(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(await reader.ReadToEndAsync());
            var root = await syntaxTree.GetRootAsync();
            
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {

                var modifiers = string.Join(" ", method.Modifiers.Select(m => m.Text));
                var returnType = method.ReturnType.ToString();
                var methodName = method.Identifier.Text;
                var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => p.ToString()));

                var methodHeader = $"{modifiers} {returnType} {methodName}({parameters})";
                await outputChannel.WriteAsync($"{methodHeader} in {Path.GetFileName(file)}");
            }
            
        }));
        outputChannel.Complete();


    }
    
}