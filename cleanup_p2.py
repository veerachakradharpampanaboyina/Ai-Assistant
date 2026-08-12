import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Remove references to WorkspaceText
content = re.sub(r"WorkspaceText\.Text\s*=\s*[^;]+;", "// WorkspaceText updated via event", content)
content = re.sub(r"Application\.Current\.Dispatcher\.Invoke\(\(\) => WorkspaceText\.Text = [^;]+\);", "// WorkspaceText updated via event", content)

# Also disable methods that use FormatSelector, CanvasSelector
# CanvasSelector_SelectionChanged
content = re.sub(
    r"private void CanvasSelector_SelectionChanged[\s\S]*?(?=\n\s+private async Task<string> BuildWorkspaceContextAsync)",
    "// CanvasSelector_SelectionChanged extracted to WorkspaceAgentControl",
    content
)

# IdeTools methods: AutoFix_Click, InsertResponse_Click, ScreenCapture_Click, IndexRepo_Click
content = re.sub(
    r"private void AutoFix_Click[\s\S]*?(?=\n\s+private async void InsertResponse_Click)",
    "// AutoFix_Click extracted",
    content
)
content = re.sub(
    r"private async void InsertResponse_Click[\s\S]*?(?=\n\s+private async void ScreenCapture_Click)",
    "// InsertResponse_Click extracted",
    content
)
content = re.sub(
    r"private async void ScreenCapture_Click[\s\S]*?(?=\n\s+protected override void OnClosing)",
    "// ScreenCapture_Click extracted\n",
    content
)
content = re.sub(
    r"private async void IndexRepo_Click[\s\S]*?(?=\n\s+// ================================================)",
    "// IndexRepo_Click extracted",
    content
)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
print("Cleanup applied.")
