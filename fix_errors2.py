import re

# 1. Fix MainWindow.xaml.cs using AIAssistant.Core
file_path_cs = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path_cs, "r", encoding="utf-8") as f:
    content_cs = f.read()

if "using AIAssistant.Core;" not in content_cs:
    content_cs = "using AIAssistant.Core;\n" + content_cs

with open(file_path_cs, "w", encoding="utf-8") as f:
    f.write(content_cs)

# 2. Fix MainWindow.xaml XAML errors
file_path_xaml = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml"
with open(file_path_xaml, "r", encoding="utf-8") as f:
    content_xaml = f.read()

content_xaml = content_xaml.replace('SelectionChanged="AiSelector_SelectionChanged"', "")

with open(file_path_xaml, "w", encoding="utf-8") as f:
    f.write(content_xaml)

print("Errors fixed.")
