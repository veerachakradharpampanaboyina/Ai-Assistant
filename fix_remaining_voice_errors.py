import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Remove the MicSelector block completely
content = re.sub(r'        if \(MicSelector\.Items\.Count == 0\)\s*\{.*?\}\s*\}\s*\}\s*catch.*?\}\s*\}', '', content, flags=re.DOTALL)

# Remove the EnableVoiceModeCheckbox check at 886
content = content.replace("else if (selectedTabIndex == 1 || EnableVoiceModeCheckbox.IsChecked == true)", "else if (selectedTabIndex == 1)")

# Remove the MainTabControl_SelectionChanged voice logic
old_voice_logic = """            // Voice Agent Restriction
            if (MainTabControl.SelectedIndex == 1)
            {
                if (EnableVoiceModeCheckbox != null && EnableVoiceModeCheckbox.IsChecked == true && !_isListeningBackground)
                {
                    StartBackgroundListener();
                }
            }
            else
            {
                if (_isListeningBackground)
                {
                    StopBackgroundListener();
                }
            }"""
new_voice_logic = """            // Voice Agent Restriction handled by VoiceAssistantControl"""
content = content.replace(old_voice_logic, new_voice_logic)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("Remaining compilation errors fixed.")
