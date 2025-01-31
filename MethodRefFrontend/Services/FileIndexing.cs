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
    public static async Task IndexFilesAsync(string path, ChannelWriter<(MethodDeclarationSyntax, string)> outputChannel)
    {
        var files = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories);
        
        await Task.WhenAll(files.Select(async file =>
        {
            using var reader = new StreamReader(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(await reader.ReadToEndAsync());
            var root = await syntaxTree.GetRootAsync();
            
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {

                
                await outputChannel.WriteAsync((method, file));
            }
            
        }));
        outputChannel.Complete();


    }
    
}