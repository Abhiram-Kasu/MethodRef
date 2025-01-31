using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using AvaloniaHelpers.Navigation.MVVM;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MethodRefFrontend.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MethodRefFrontend.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private const string DbPath = "./index.db";
    private readonly DatabaseService _databaseService = new(DbPath);
    public ObservableCollection<MethodRefItem> Logs { get; set; } = [];
    
    [ObservableProperty] private bool _isIndexing = false;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PublicSearchPath))]
    private string? _internalSearchPath;

    public string PublicSearchPath => InternalSearchPath ?? "No Path Selected";

    [RelayCommand]
    public async Task ChooseFolder()
    {
        var level = TopLevel.GetTopLevel(App.NavService.CurrentVVModel!.Value.View)!;
        var storageService = level.StorageProvider;

        var folder = await storageService.OpenFolderPickerAsync(new()
        {
            Title = "Select a folder to index",
            AllowMultiple = false
        });

        if (folder is not [var path]) return;
        InternalSearchPath = path.Path.AbsolutePath;
    }

    [RelayCommand]
    public async Task StartIndexing()
    {
        if (IsIndexing) return;
        IsIndexing = true;
        Logs.Clear();
        try
        {
            if (InternalSearchPath is null) return;
            var channel = Channel.CreateUnbounded<(MethodDeclarationSyntax, string)>();

            await Task.WhenAll(FileIndexing.IndexFilesAsync(InternalSearchPath, channel.Writer), Task.Run(async () =>
            {
                await foreach (var item in channel.Reader.ReadAllAsync())
                {
                    var (method, file) = item;
                    var methodRef = MethodRef.FromMethodReference(method, file);
                    _databaseService.AddMethodRef(methodRef);
                    Logs.Add(new MethodRefItem(methodRef));
                }
            }));
        }
        finally
        {
            _databaseService.GetAllMethodRefs().ForEach(x => Logs.Add(new MethodRefItem(x)));
            IsIndexing = false;
        }
    }

    public async Task Search(string text)
    {
        var res = await _databaseService.Search(text);
        Logs.Clear();
        res.ForEach(x => Logs.Add(new MethodRefItem(x)));
    }
}

public partial class MethodRefItem(MethodRef methodRef) : ObservableObject
{
    private bool _isExpanded;

    public MethodRef MethodRef { get; } = methodRef;

    public string MethodHeader => MethodRef.MethodHeader;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value) && value)
            {
                LoadContent();
            }
        }
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Document))] private string _content = string.Empty;


    public TextDocument Document => new (Content); 
    private void LoadContent()
    {
        try
        {
            var fileContent = File.ReadAllText(MethodRef.FileName);
            
            var start = (int)MethodRef.TextSpanStart;
            var length = (int)MethodRef.TextSpanEnd;
            Content = fileContent.Substring(start, length);
            Console.WriteLine("Content: " + Content);
        }
        catch (Exception ex)
        {
            Content = $"Error loading content: {ex.Message}";
        }
    }
}