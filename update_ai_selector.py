import re

# Update MainWindow.xaml
file_path_xaml = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml"
with open(file_path_xaml, "r", encoding="utf-8") as f:
    content_xaml = f.read()

old_combobox = """<ComboBox x:Name="AiSelector"  Width="100" SelectedIndex="0" Background="#333" Foreground="White" BorderThickness="0" HorizontalAlignment="Right">"""
new_combobox = """<ComboBox x:Name="AiSelector"  Width="100" SelectedIndex="0" Background="#333" Foreground="White" BorderThickness="0" HorizontalAlignment="Right" SelectionChanged="AiSelector_SelectionChanged">"""
if new_combobox not in content_xaml:
    content_xaml = content_xaml.replace(old_combobox, new_combobox)

with open(file_path_xaml, "w", encoding="utf-8") as f:
    f.write(content_xaml)

# Update MainWindow.xaml.cs
file_path_cs = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path_cs, "r", encoding="utf-8") as f:
    content_cs = f.read()

selection_changed_code = """
    private void AiSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrowserAgentFeature == null || AiSelector == null) return;
        
        if (AiSelector.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Content != null)
        {
            string selectedAi = item.Content.ToString()!;
            string url = "https://chatgpt.com"; // default
            
            if (selectedAi.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                url = "https://gemini.google.com/app";
            else if (selectedAi.Equals("Claude", StringComparison.OrdinalIgnoreCase))
                url = "https://claude.ai";
            else if (selectedAi.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase))
                url = "https://chat.deepseek.com";
            else if (selectedAi.Equals("Arena AI", StringComparison.OrdinalIgnoreCase))
                url = "https://chat.lmsys.org";
            else if (selectedAi.Equals("Z.ai", StringComparison.OrdinalIgnoreCase))
                url = "https://z.ai"; // example
                
            BrowserAgentFeature.Navigate(url);
        }
    }
"""

# Insert right after MainTabControl_SelectionChanged
if "private void AiSelector_SelectionChanged" not in content_cs:
    content_cs = content_cs.replace("    private bool _isZenMode = false;", selection_changed_code + "\n    private bool _isZenMode = false;")

with open(file_path_cs, "w", encoding="utf-8") as f:
    f.write(content_cs)

print("MainWindow files updated.")
