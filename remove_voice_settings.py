import re

file_path_cs = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path_cs, "r", encoding="utf-8") as f:
    content_cs = f.read()

# Remove the synthesizer init in PopulateSettingsAsync
old_init = """
        if (_synthesizer == null)
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
        }

        if (VoiceSelector.Items.Count == 0)
        {
            var voices = _synthesizer.GetInstalledVoices();
            foreach (var v in voices)
            {
                VoiceSelector.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = v.VoiceInfo.Name, Tag = v.VoiceInfo.Name });
            }
            if (VoiceSelector.Items.Count > 0)
                VoiceSelector.SelectedIndex = 0;
        }"""
content_cs = content_cs.replace(old_init, "")

with open(file_path_cs, "w", encoding="utf-8") as f:
    f.write(content_cs)

# Now remove the Voice & Audio settings from MainWindow.xaml
file_path_xaml = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml"
with open(file_path_xaml, "r", encoding="utf-8") as f:
    content_xaml = f.read()

# I will use a precise replace to remove the Voice & Audio section
voice_section_start = """                            <TextBlock Text="Voice &amp; Audio" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10"/>"""
voice_section_end = """                            <Separator Margin="0,10,0,20"/>

                            <!-- Security Settings -->"""

import collections
if voice_section_start in content_xaml and voice_section_end in content_xaml:
    idx_start = content_xaml.find(voice_section_start)
    idx_end = content_xaml.find(voice_section_end)
    content_xaml = content_xaml[:idx_start] + "<!-- Voice & Audio settings moved to VoiceAssistantControl -->\n                            " + content_xaml[idx_end:]

with open(file_path_xaml, "w", encoding="utf-8") as f:
    f.write(content_xaml)

print("Remaining Voice & Audio settings and initialization removed.")
