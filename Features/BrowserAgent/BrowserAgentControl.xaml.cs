using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using AIAssistant.Core;
using Microsoft.Web.WebView2.Core;



namespace AIAssistant.Features.BrowserAgent
{
    public partial class BrowserAgentControl : UserControl, IBrowserAgent
    {
        private bool _isBrowserRecording = false;
        private ObservableCollection<SecretItem> _secrets = new();

        public BrowserAgentControl()
        {
            InitializeComponent();
            AppServices.Instance.BrowserAgent = this;
            this.Loaded += BrowserAgentControl_Loaded;
            AppServices.Instance.WorkspaceChanged += OnWorkspaceChanged;
        }

        private async void BrowserAgentControl_Loaded(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.WebMessageReceived += WebView_WebMessageReceived;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            LoadSecrets();
        }

        private void OnWorkspaceChanged(object? sender, EventArgs e)
        {
            // Update browser state if needed based on workspace
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsColumn.MaxWidth == 0)
            {
                SettingsColumn.MaxWidth = 350;
                SettingsColumn.Width = new GridLength(350);
            }
            else
            {
                SettingsColumn.MaxWidth = 0;
                SettingsColumn.Width = new GridLength(0);
            }
        }

        private void LoadSecrets()
        {
            var loaded = SecretManager.LoadSecrets();
            _secrets = new ObservableCollection<SecretItem>(loaded);
            SecretsDataGrid.ItemsSource = _secrets;
        }

        private void AddSecret_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SecretDomainInput.Text) && 
                !string.IsNullOrWhiteSpace(SecretUserInput.Text) && 
                !string.IsNullOrWhiteSpace(SecretPassInput.Password))
            {
                var secret = new SecretItem
                {
                    Domain = SecretDomainInput.Text.Trim(),
                    Username = SecretUserInput.Text.Trim(),
                    Password = SecretPassInput.Password
                };
                
                _secrets.Add(secret);
                SecretManager.SaveSecrets(System.Linq.Enumerable.ToList(_secrets));
                
                SecretDomainInput.Clear();
                SecretUserInput.Clear();
                SecretPassInput.Clear();
            }
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            return await webView.ExecuteScriptAsync(script);
        }

        private async void BrowserScreenshot_Click(object sender, RoutedEventArgs e)
        {
            string workspacePath = AppServices.Instance.WorkspacePath;
            if (string.IsNullOrEmpty(workspacePath)) return;

            try
            {
                string screenshotPath = System.IO.Path.Combine(workspacePath, ".agent_history", "screenshots");
                Directory.CreateDirectory(screenshotPath);
                string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string fullPath = System.IO.Path.Combine(screenshotPath, fileName);
    
                string pythonScript = $@"
from autonomous_agent import AutonomousAgent
agent = AutonomousAgent()
agent.screenshot(r'{fullPath.Replace("'", "\\'")}', full_page=True)
print('Screenshot saved to: {fullPath}')
agent.close()
";
                string scriptPath = System.IO.Path.Combine(workspacePath, "_temp_screenshot.py");
                await File.WriteAllTextAsync(scriptPath, pythonScript);
    
                var psi = new ProcessStartInfo("python", $"\"{scriptPath}\"")
                {
                    WorkingDirectory = workspacePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = await proc.StandardOutput.ReadToEndAsync();
                    await proc.WaitForExitAsync();
                    AppServices.Instance.ArtifactManager.CreateArtifact(ArtifactType.Screenshot, fileName, $"Screenshot saved at: {fullPath}");
                }
                try { File.Delete(scriptPath); } catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Screenshot error: {ex.Message}");
            }
        }

        private void BrowserRecord_Click(object sender, RoutedEventArgs e)
        {
            _isBrowserRecording = !_isBrowserRecording;
            if (_isBrowserRecording)
            {
                RecordBtn.Content = "⏹ Stop Record";
                RecordBtn.Appearance = Wpf.Ui.Controls.ControlAppearance.Danger;
                // Add recording start logic here
            }
            else
            {
                RecordBtn.Content = "🎥 Record";
                RecordBtn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                // Add recording stop logic here
            }
        }

        public void Navigate(string url)
        {
            try
            {
                webView.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating WebView: {ex.Message}");
            }
        }

        public event EventHandler<string>? MessageReceived;

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebView_WebMessageReceived error: {ex.Message}");
            }
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && AllowBrowserNavCheckbox.IsChecked == true)
            {
                // Verify URL against allowlist/denylist
            }
        }
    }
}
