import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Remove the Browser Agent tab
tab_pattern = r'            <TabItem Header="Browser Agent" FontSize="14" Name="BrowserAgentTab">\s*<browser:BrowserAgentControl x:Name="BrowserAgentFeature" />\s*</TabItem>'
content = re.sub(tab_pattern, '', content)

# 2. Add BrowserAgentFeature into Grid.Column="2" of Grid.Row="4"
# Look for the empty comment <!-- WebView moved to BrowserAgentControl -->
# which is right after the CodeEditorSplitter
injection_target = '            <!-- WebView moved to BrowserAgentControl -->'
browser_xml = '            <browser:BrowserAgentControl x:Name="BrowserAgentFeature" Grid.Column="2" />'
content = content.replace(injection_target, browser_xml)

# 3. Add Storyboard resources to MainWindow
# We need to add Window.Resources if it doesn't exist, or just put it in a Grid.
storyboard_xml = """    <ui:FluentWindow.Resources>
        <Storyboard x:Key="ChatPulseAnimation" RepeatBehavior="Forever">
            <DoubleAnimation Storyboard.TargetName="ChatInputBorder" Storyboard.TargetProperty="BorderBrush.(SolidColorBrush.Opacity)" From="0.3" To="1.0" Duration="0:0:0.8" AutoReverse="True"/>
            <ColorAnimation Storyboard.TargetName="ChatInputBorder" Storyboard.TargetProperty="BorderBrush.(SolidColorBrush.Color)" To="#007ACC" Duration="0:0:0.8" AutoReverse="True"/>
        </Storyboard>
    </ui:FluentWindow.Resources>

    <Grid>"""
if "<ui:FluentWindow.Resources>" not in content:
    content = content.replace("    <Grid>", storyboard_xml, 1)

# 4. Wrap the Chat Input Box in a border we can animate, and add ProgressRing
chat_input_xml = """                <!-- Chat Input Box -->
                <Border x:Name="ChatInputBorder" Grid.Row="3" Background="#252526" Padding="10" BorderThickness="0,2,0,0" BorderBrush="#333">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        
                        <!-- Pending Changes UI -->"""

content = content.replace("""                <!-- Chat Input Box -->
                <Border Grid.Row="3" Background="#252526" Padding="10" BorderThickness="0,1,0,0" BorderBrush="#333">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        
                        <!-- Pending Changes UI -->""", chat_input_xml)

# 5. Add the ProgressRing next to the Submit button
submit_xml_old = """                            <ui:Button Content="Submit" Click="InitAgent_Click" Appearance="Primary" Height="28"/>
                        </StackPanel>"""
submit_xml_new = """                            <ui:ProgressRing x:Name="ChatProcessingRing" IsIndeterminate="True" Visibility="Collapsed" Width="20" Height="20" Margin="0,0,10,0"/>
                            <TextBlock x:Name="ChatProcessingText" Text="Processing..." Foreground="#007ACC" VerticalAlignment="Center" Visibility="Collapsed" Margin="0,0,10,0" FontWeight="SemiBold"/>
                            <ui:Button x:Name="SubmitBtn" Content="Submit" Click="InitAgent_Click" Appearance="Primary" Height="28"/>
                        </StackPanel>"""
content = content.replace(submit_xml_old, submit_xml_new)


with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("MainWindow.xaml updated with structural changes and animations.")
