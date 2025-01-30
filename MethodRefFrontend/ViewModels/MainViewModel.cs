using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using AvaloniaHelpers.Navigation.MVVM;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MethodRefFrontend.Services;

namespace MethodRefFrontend.ViewModels;

public partial class MainViewModel : BaseViewModel
{

    public ObservableCollection<string> Logs { get; set; } = [];
    
    [ObservableProperty] private bool _isIndexing = false;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PublicSearchPath))]
    private string? _internalSearchPath;
    
    

    public string PublicSearchPath => InternalSearchPath ?? "No Path Selected";
    [RelayCommand]
    public async Task ChooseFolder() {
        var level = TopLevel.GetTopLevel(App.NavService.CurrentVVModel!.Value.View)!;

        var storageService = level.StorageProvider;

        var folder = await storageService.OpenFolderPickerAsync(new () {
            Title = "Select a folder to index",
            AllowMultiple = false
        });

        if (folder is not [var path] ){
            return;
        }

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
            var channel = Channel.CreateUnbounded<string>();


            await Task.WhenAll(FileIndexing.IndexFilesAsync(InternalSearchPath, channel.Writer), Task.Run(async () =>
            {
                await foreach (var item in channel.Reader.ReadAllAsync())
                {
                    Logs.Add(item);
                }
            }));
        }finally
        {
            IsIndexing = false;
        }
    }

}