import re

# Fix IdeToolsControl.xaml.cs
file_path_ide = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\Features\IdeTools\IdeToolsControl.xaml.cs"
with open(file_path_ide, "r", encoding="utf-8") as f:
    content_ide = f.read()

content_ide = content_ide.replace("AppServices.Instance.CurrentWorkspacePath", "AppServices.Instance.WorkspacePath")
content_ide = content_ide.replace("var artifactManager = AppServices.Instance.Get<ArtifactManager>();", "var artifactManager = AppServices.Instance.ArtifactManager;")
content_ide = content_ide.replace("index.Values.Sum(v => v.Count)", "System.Linq.Enumerable.Sum(index.Values, v => v.Count)")

with open(file_path_ide, "w", encoding="utf-8") as f:
    f.write(content_ide)

# Fix MainWindow.xaml.cs
file_path_main = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path_main, "r", encoding="utf-8") as f:
    content_main = f.read()

# Replace CanvasSelector and FormatSelector variables with defaults
content_main = re.sub(r"string canvasType = \(\(System\.Windows\.Controls\.ComboBoxItem\)CanvasSelector\.SelectedItem\)\.Content\?\.ToString\(\) \?\? \"Code Project\";", 'string canvasType = "Code Project"; // Handled by WorkspaceAgent', content_main)
content_main = re.sub(r"string formatType = \(\(System\.Windows\.Controls\.ComboBoxItem\)FormatSelector\.SelectedItem\)\.Content\?\.ToString\(\) \?\? \"Any\";", 'string formatType = "Any"; // Handled by WorkspaceAgent', content_main)

with open(file_path_main, "w", encoding="utf-8") as f:
    f.write(content_main)

print("Fixes applied.")
