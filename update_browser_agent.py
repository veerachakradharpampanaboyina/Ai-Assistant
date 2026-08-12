import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\Features\BrowserAgent\BrowserAgentControl.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

nav_method = """        public void Navigate(string url)
        {
            try
            {
                webView.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating WebView: {ex.Message}");
            }
        }"""

# Insert before public event EventHandler<string>? MessageReceived;
if "public void Navigate" not in content:
    content = content.replace("        public event EventHandler<string>? MessageReceived;", nav_method + "\n\n        public event EventHandler<string>? MessageReceived;")
    
with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("BrowserAgentControl updated.")
