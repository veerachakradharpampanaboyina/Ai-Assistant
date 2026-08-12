import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

method_to_add = """
    private void RecentWorkspacesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentWorkspacesCombo.SelectedItem is string path && !string.IsNullOrEmpty(path))
        {
            _ = LoadWorkspaceAsync(path);
        }
    }

    private async Task LoadWorkspaceAsync(string folderPath)
    {
        _workspacePath = folderPath;
        _artifactManager.SetWorkspace(_workspacePath);
        _gitManager.SetWorkspace(_workspacePath);
        _hookManager.SetWorkspace(_workspacePath);
        
        AppServices.Instance.SetWorkspace(_workspacePath);

        PopulateFileExplorer(_workspacePath);
        _problems.Clear();
        StartIdeTerminal();
        UpdateRecentWorkspacesUI();

        WorkspaceAgentMessages.Clear();
        WorkspaceAgentMessages.Add(new AgentMessage { Role = "System", Content = $"Workspace set to: {_workspacePath}" });
        _artifactManager.CreateArtifact(ArtifactType.AgentLog, "workspace_loaded.md", $"Switched workspace to {_workspacePath}");
    }

    private void UpdateRecentWorkspacesUI()
    {
        RecentWorkspacesCombo.SelectionChanged -= RecentWorkspacesCombo_SelectionChanged;
        RecentWorkspacesCombo.ItemsSource = _hookManager.RecentWorkspaces.Select(w => w.Path).ToList();
        if (_hookManager.RecentWorkspaces.Count > 0)
        {
            RecentWorkspacesCombo.SelectedItem = _workspacePath;
        }
        RecentWorkspacesCombo.SelectionChanged += RecentWorkspacesCombo_SelectionChanged;
    }
}
"""

content = content.rstrip()
if content.endswith("}"):
    content = content[:-1] + method_to_add
    
with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
