import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Remove BackgroundListenLoop entirely
content = re.sub(r'    private async Task BackgroundListenLoop\(CancellationToken token\)\s*\{.*?\n    \}\n', '', content, flags=re.DOTALL)

# Remove VoiceCommand_Click entirely
content = re.sub(r'    private async void VoiceCommand_Click\(object sender, RoutedEventArgs e\)\s*\{.*?\n    \}\n', '', content, flags=re.DOTALL)

# In ProcessAgentCommandAsync, the ask_question case references VoiceCommand_Click and VoiceCommandBtn
content = re.sub(
    r'                        Application.Current.Dispatcher.Invoke\(\(\) =>\s*\{\s*VoiceCommand_Click\(VoiceCommandBtn, new RoutedEventArgs\(\)\);\s*\}\);',
    r'                        // Voice command click moved to VoiceAssistantControl',
    content, flags=re.DOTALL
)

# Search for any remaining VoiceCommandBtn references and comment them out
lines = content.split('\n')
for i, line in enumerate(lines):
    if 'VoiceCommandBtn' in line and not line.strip().startswith('//'):
        lines[i] = '// ' + line
    elif 'LanguageSelector' in line and not line.strip().startswith('//'):
        lines[i] = '// ' + line

content = '\n'.join(lines)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("Voice methods and references aggressively stripped.")
