import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Add the toggle button
old_stack = """<StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
                            <ToggleButton x:Name="AutoLoopToggle" """

new_stack = """<StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
                            <ToggleButton x:Name="AutoAcceptToggle" Content="Auto-Accept" Margin="0,0,5,0" Height="28" ToolTip="Automatically accept and save all file changes"/>
                            <ToggleButton x:Name="AutoLoopToggle" """

if "x:Name=\"AutoAcceptToggle\"" not in content:
    content = content.replace(old_stack, new_stack)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
print("MainWindow.xaml updated.")
