import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update Tab Indexing
# MainTabControl_SelectionChanged checks `if (MainTabControl.SelectedIndex == 4)` for Code Editor
content = content.replace("if (MainTabControl.SelectedIndex == 4)", "if (MainTabControl.SelectedIndex == 3) // Code Editor is now index 3")

# 2. Add animation logic in InitAgent_Click
# I need to start the animation and show the ProgressRing
init_agent_old = """    private async void InitAgent_Click(object sender, RoutedEventArgs e)
    {
        string prompt = UserPromptText.Text.Trim();
        if (string.IsNullOrEmpty(prompt)) return;
        
        UserPromptText.Clear();
        InjectToChat("User", prompt);
        
        // Disable AutoLoop when starting a fresh prompt manually (unless already checked)
        // AutoLoopToggle.IsChecked = false; // We can let user keep it checked if they want
        
        await ProcessAgentCommandAsync(prompt);
    }"""

init_agent_new = """    private async void InitAgent_Click(object sender, RoutedEventArgs e)
    {
        string prompt = UserPromptText.Text.Trim();
        if (string.IsNullOrEmpty(prompt)) return;
        
        UserPromptText.Clear();
        InjectToChat("User", prompt);
        
        // Start Chat Processing Animation
        ToggleChatAnimation(true);
        
        try 
        {
            await ProcessAgentCommandAsync(prompt);
        }
        finally
        {
            // Stop Chat Processing Animation
            ToggleChatAnimation(false);
        }
    }

    private void ToggleChatAnimation(bool isProcessing)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (isProcessing)
            {
                SubmitBtn.IsEnabled = false;
                UserPromptText.IsEnabled = false;
                ChatProcessingRing.Visibility = Visibility.Visible;
                ChatProcessingText.Visibility = Visibility.Visible;
                
                var storyboard = (System.Windows.Media.Animation.Storyboard)this.Resources["ChatPulseAnimation"];
                if (storyboard != null)
                {
                    storyboard.Begin();
                }
            }
            else
            {
                SubmitBtn.IsEnabled = true;
                UserPromptText.IsEnabled = true;
                ChatProcessingRing.Visibility = Visibility.Collapsed;
                ChatProcessingText.Visibility = Visibility.Collapsed;
                
                var storyboard = (System.Windows.Media.Animation.Storyboard)this.Resources["ChatPulseAnimation"];
                if (storyboard != null)
                {
                    storyboard.Stop();
                }
                
                // Reset border brush manually just in case
                ChatInputBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 51, 51, 51)); // #333
            }
        });
    }"""

content = content.replace(init_agent_old, init_agent_new)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("MainWindow.xaml.cs updated with chat animation logic.")
