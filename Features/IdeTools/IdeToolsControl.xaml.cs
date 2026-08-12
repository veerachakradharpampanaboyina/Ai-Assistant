using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AIAssistant.Core;

namespace AIAssistant.Features.IdeTools
{
    public partial class IdeToolsControl : UserControl
    {
        public IdeToolsControl()
        {
            InitializeComponent();
        }

        private void AutoFix_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("To auto-fix code, highlight the code in your IDE and press Ctrl+Shift+F.");
        }

        private async void InsertResponse_Click(object sender, RoutedEventArgs e)
        {
            var browserAgent = AppServices.Instance.BrowserAgent;
            if (browserAgent == null)
            {
                MessageBox.Show("Browser Agent is not available.");
                return;
            }

            try
            {
                // This requires BrowserAgentControl to expose an ExecuteScriptAsync method
                string textToInsert = await browserAgent.ExecuteScriptAsync("window.getSelection().toString();");
                
                textToInsert = JsonSerializer.Deserialize<string>(textToInsert) ?? "";

                if (string.IsNullOrWhiteSpace(textToInsert))
                {
                    string jsGetLastCodeBlock = @"
                        (function() {
                            var codeBlocks = document.querySelectorAll('pre code');
                            if (codeBlocks.length > 0) {
                                return codeBlocks[codeBlocks.length - 1].innerText;
                            }
                            return '';
                        })();";
                    textToInsert = await browserAgent.ExecuteScriptAsync(jsGetLastCodeBlock);
                    textToInsert = JsonSerializer.Deserialize<string>(textToInsert) ?? "";
                }

                if (!string.IsNullOrWhiteSpace(textToInsert))
                {
                    // Copy to clipboard
                    Clipboard.SetText(textToInsert);
                    MessageBox.Show("Copied to clipboard. Now paste it into your IDE.");
                }
                else
                {
                    MessageBox.Show("Could not find any text to insert.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InsertResponse_Click error: {ex.Message}");
            }
        }

        private async void ScreenCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var captureWindow = new AIAssistant.ScreenCaptureWindow();
                if (captureWindow.ShowDialog() == true && captureWindow.CapturedImage != null)
                {
                    string extractedText = await AIAssistant.OcrHelper.ExtractTextAsync(captureWindow.CapturedImage);
                    
                    if (!string.IsNullOrWhiteSpace(extractedText))
                    {
                        var browserAgent = AppServices.Instance.BrowserAgent;
                        if (browserAgent != null)
                        {
                            string safeText = JsonSerializer.Serialize(extractedText);
                            string js = $@"
                                var safeText = {safeText};
                                var el = document.getElementById('prompt-textarea') || 
                                         document.querySelector('div[contenteditable=""true""]') || 
                                         document.querySelector('textarea');
                                if (el) {{
                                    el.focus();
                                    if (!document.execCommand('insertText', false, safeText)) {{
                                        if (el.tagName === 'TEXTAREA') {{
                                            el.value = safeText;
                                        }} else {{
                                            el.textContent = safeText;
                                        }}
                                        el.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                    }}
                                }}";
                            await browserAgent.ExecuteScriptAsync(js);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScreenCapture_Click error: {ex.Message}");
            }
        }

        private async void IndexRepo_Click(object sender, RoutedEventArgs e)
        {
            var workspace = AppServices.Instance.WorkspacePath;
            if (string.IsNullOrEmpty(workspace))
            {
                MessageBox.Show("Please set a workspace first.");
                return;
            }

            // We need a way to log to the agent output from here, or we just rely on ArtifactManager logs.
            var artifactManager = AppServices.Instance.ArtifactManager;
            var index = await artifactManager.BuildRepoIndexAsync(workspace);
            
            MessageBox.Show($"Indexed {index.Count} files with {System.Linq.Enumerable.Sum(index.Values, v => v.Count)} symbols.");
            artifactManager.CreateArtifact(ArtifactType.Report, "Repo Index", $"Indexed {index.Count} files with {System.Linq.Enumerable.Sum(index.Values, v => v.Count)} symbols at {DateTime.Now:g}");
        }
    }
}
