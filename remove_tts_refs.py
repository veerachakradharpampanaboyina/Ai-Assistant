import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Remove SpeakAloud completely
content = re.sub(r'    private void SpeakAloud\(string text\)\s*\{.*?\n    \}\n', '', content, flags=re.DOTALL)

# Remove SpeakAloudSync completely
content = re.sub(r'    private void SpeakAloudSync\(string text\)\s*\{.*?\n    \}\n', '', content, flags=re.DOTALL)

# Remove the private SpeechSynthesizer? _synthesizer; field
content = re.sub(r'    private SpeechSynthesizer\? _synthesizer;\n', '', content)

# Update references to use VoiceAssistantFeature
content = content.replace('SpeakAloud(msg);', 'VoiceAssistantFeature.SpeakAloud(msg);')
content = content.replace('SpeakAloudSync(questionText);', 'VoiceAssistantFeature.SpeakAloudSync(questionText);')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("TTS methods successfully stripped and delegated.")
