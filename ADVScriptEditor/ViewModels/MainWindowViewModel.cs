﻿using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ADVScriptEditor.ADVScript;
using ADVScriptEditor.Views;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace ADVScriptEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static FilePickerFileType ADVScriptBin { get; } = new("ADVScript Binary")
    {
        Patterns = new[] { "*.advbin" }
    };
    private static FilePickerFileType Json { get; } = new("JSON")
    {
        Patterns = new[] { "*.json" }
    };
    
    private CScriptData? _advScript;
    private EditorViewModel? _editorView;

    public EditorViewModel? EditorView
    {
        get => _editorView;
        set => this.RaiseAndSetIfChanged(ref _editorView, value);
    }
    
    public MainWindowViewModel()
    {
        _editorView = new DefaultViewModel();
    }

    public async Task<bool> OpenConfig()
    {
        if (Avalonia.Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return false;
        // Start async operation to open the dialog.
        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Config JSON (.json)",
            AllowMultiple = false,
            FileTypeFilter = [ Json ] 
        });

        if (files.Count < 1) return false;
        // Open reading stream from the first file.
        await using var stream = await files[0].OpenReadAsync();
        using var streamReader = new StreamReader(stream);
        var text = await streamReader.ReadToEndAsync();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        var config = JsonSerializer.Deserialize<AdvConfig>(text, options);

        AdvConfig.Instance.Commands = config!.Commands;
        return true;
    }
    
    public async void OpenFile()
    {
        var configSuccess = await OpenConfig();
        if (!configSuccess) return;
        
        if (Avalonia.Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        
        // Start async operation to open the dialog.
        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open ADVScript binary (.advbin)",
            AllowMultiple = false,
            FileTypeFilter = [ ADVScriptBin ] 
        });

        if (files.Count < 1) return;
        // Open reading stream from the first file.
        await using var stream = await files[0].OpenReadAsync();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var buffer = memoryStream.ToArray();

        _advScript = new CScriptData();
        _advScript.Read(buffer);
        EditorView = new ADVScriptViewModel(_advScript);
    }
    
    public async void SaveFile()
    {
        if (_advScript is null) return;
        if (EditorView is null) return;
        if (Avalonia.Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        EditorView.PrepareSave();
        
        // Start async operation to open the dialog.
        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save ADVScript Binary (.advbin)",
            DefaultExtension = ".advbin",
            FileTypeChoices = [ ADVScriptBin ],
        });

        if (file is null) return;
        // Open writing stream from the file.
        await using var stream = await file.OpenWriteAsync();
        await using var binaryWriter = new BinaryWriter(stream);
        binaryWriter.Write(_advScript.Write());
    }
    
    public async void ImportFile()
    {
        var configSuccess = await OpenConfig();
        if (!configSuccess) return;
        
        if (Avalonia.Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        // Start async operation to open the dialog.
        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open ADVScript JSON (.json)",
            AllowMultiple = false,
            FileTypeFilter = [ Json ] 
        });

        if (files.Count < 1) return;
        // Open reading stream from the first file.
        await using var stream = await files[0].OpenReadAsync();
        using var streamReader = new StreamReader(stream);
        var text = await streamReader.ReadToEndAsync();
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        var script = JsonSerializer.Deserialize<CParsedScript>(text, options);

        if (script == null) return;
        EditorView = new ADVScriptViewModel(script);
        _advScript = script.Compile();
    }
    
    public async void ExportFile()
    {
        if (_advScript is null) return;
        if (EditorView is null) return;
        if (Avalonia.Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        EditorView.PrepareSave();
        
        // Start async operation to open the dialog.
        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save ADVScript JSON (.json)",
            DefaultExtension = ".json",
            FileTypeChoices = [ Json ],
        });

        if (file is null) return;
        // Open writing stream from the file.
        await using var stream = await file.OpenWriteAsync();
        await using var streamWriter = new StreamWriter(stream);
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        var script = JsonSerializer.Serialize(((ADVScriptViewModel)EditorView).ParsedScript, options);
        await streamWriter.WriteAsync(script);
    }
}