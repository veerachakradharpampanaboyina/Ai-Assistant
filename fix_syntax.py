import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

content = content.replace("Application.Current.Dispatcher.Invoke(() => // WorkspaceText updated via event", "// WorkspaceText updated via event")

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
