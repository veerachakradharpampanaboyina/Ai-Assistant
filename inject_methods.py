import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

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

    private void RecentWorkspacesCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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

# Find SetWorkspace_Click and replace it with our block
# We use regex to match SetWorkspace_Click and its body.
# Assumes the body is fairly simple and we can match until the next method `PopulateFileExplorer`

pattern = r"private\s+void\s+SetWorkspace_Click\(object\s+sender,\s+RoutedEventArgs\s+e\)\s*\{[\s\S]*?(?=private\s+void\s+PopulateFileExplorer)"
new_content = re.sub(pattern, workspace_methods + "\n\n    ", content)

if new_content != content:
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(new_content)
    print("Methods successfully injected.")
else:
    print("Could not find SetWorkspace_Click to replace. The file might already have the async version or the regex failed.")

