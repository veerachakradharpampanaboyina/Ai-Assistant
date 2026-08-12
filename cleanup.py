import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    if "webView." in line or "VoiceCommandBtn" in line or "LanguageSelector" in line or "VoiceModeMainToggle" in line:
        new_lines.append("// " + line)
    else:
        new_lines.append(line)

with open(file_path, "w", encoding="utf-8") as f:
    f.writelines(new_lines)

print("Targeted cleanup complete.")
