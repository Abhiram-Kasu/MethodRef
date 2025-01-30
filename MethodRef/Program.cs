// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

const string dir = "/Users/abhiramkasu/RiderProjects/";



var files = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories);


var stopwatch = Stopwatch.StartNew();




await Task.WhenAll(files.AsParallel().Select(async file =>
{
    using var reader = new StreamReader(file);
    var syntaxTree = CSharpSyntaxTree.ParseText(await reader.ReadToEndAsync());
    var root = syntaxTree.GetRoot();
    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

    foreach (var method in methods)
    {
        Console.WriteLine($"{method.Identifier} in {Path.GetFileName(file)}");
    }
}));

Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds}");