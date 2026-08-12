import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Add Dummy Variables
dummy_vars = """
#pragma warning disable CS0169, CS0649
    private dynamic? webView;
    private dynamic? VoiceCommandBtn;
    private dynamic? LanguageSelector;
    private dynamic? VoiceModeMainToggle;
#pragma warning restore CS0169, CS0649
"""
content = re.sub(
    r"(private System\.Collections\.ObjectModel\.ObservableCollection<PendingChange> _pendingChanges = new\(\);)",
    r"\1\n" + dummy_vars,
    content
)

# 2. Disable WebView Initialization
content = re.sub(
    r"(await webView\.EnsureCoreWebView2Async\(null\);)",
    r"// \1",
    content
)
content = re.sub(
    r"(webView\.WebMessageReceived \+= WebView_WebMessageReceived;)",
    r"// \1",
    content
)
content = re.sub(
    r"(webView\.NavigationCompleted \+= WebView_NavigationCompleted;)",
    r"// \1",
    content
)

# 3. Disable VoiceModeMainToggle events
content = re.sub(r"(VoiceModeMainToggle_Checked\(.*?\);)", r"// \1", content)
content = re.sub(r"(VoiceModeMainToggle_Unchecked\(.*?\);)", r"// \1", content)


# 4. Inject Missing Workspace Methods
workspace_methods = """
    private async void SetWorkspace_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Workspace Folder",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadWorkspaceAsync(dialog.FolderName);
        }
    }

    private async Task LoadWorkspaceAsync(string folderPath)
    {
        _workspacePath = folderPath;
        _artifactManager.SetWorkspace(_workspacePath);
        _gitManager.SetWorkspace(_workspacePath);
        _hookManager.SetWorkspace(_workspacePath);
        
        AIAssistant.Core.AppServices.Instance.SetWorkspace(_workspacePath);

        PopulateFileExplorer(_workspacePath);
        _problems.Clear();
        StartIdeTerminal();
        UpdateRecentWorkspacesUI();

        WorkspaceAgentMessages.Clear();
        WorkspaceAgentMessages.Add(new AgentMessage { Role = "System", Content = $"Workspace set to: {_workspacePath}" });
        _artifactManager.CreateArtifact(ArtifactType.AgentLog, "workspace_loaded.md", $"Switched workspace to {_workspacePath}");
    }

    private void RecentWorkspacesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentWorkspacesCombo.SelectedItem is string path && !string.IsNullOrEmpty(path))
        {
            _ = LoadWorkspaceAsync(path);
        }
    }

    private void UpdateRecentWorkspacesUI()
    {
        RecentWorkspacesCombo.SelectionChanged -= RecentWorkspacesCombo_SelectionChanged;
        RecentWorkspacesCombo.ItemsSource = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(_hookManager.RecentWorkspaces, w => w.Path));
        if (_hookManager.RecentWorkspaces.Count > 0)
        {
            RecentWorkspacesCombo.SelectedItem = _workspacePath;
        }
        RecentWorkspacesCombo.SelectionChanged += RecentWorkspacesCombo_SelectionChanged;
    }
"""
# Replace SetWorkspace_Click and anything until the next private method with our block
content = re.sub(
    r"private void SetWorkspace_Click\(object sender, RoutedEventArgs e\)[\s\S]*?(?=private void PopulateFileExplorer)",
    workspace_methods + "\n\n    ",
    content
)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("Patch applied.")
