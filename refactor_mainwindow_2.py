import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Replace the handler signature and logic
old_handler = r"private async void WebView_WebMessageReceived\(object\? sender, Microsoft\.Web\.WebView2\.Core\.CoreWebView2WebMessageReceivedEventArgs e\)\s*\{\s*try\s*\{\s*string message = e\.TryGetWebMessageAsString\(\);"
new_handler = "private async void OnBrowserAgentMessageReceived(object? sender, string message)\n    {\n        try\n        {"
content = re.sub(old_handler, new_handler, content)

# In MainWindow(), find where to subscribe.
# Since AppServices.Instance.BrowserAgent might not be set when MainWindow constructor runs,
# we can subscribe in the Loaded event, or we can check when it's accessed, but actually BrowserAgentControl is loaded inside MainWindow's XAML!
# So we can just subscribe in MainWindow_Loaded or constructor. Let's do it in the constructor just after InitializeComponent().
# Wait, BrowserAgentControl registers itself to AppServices.Instance.BrowserAgent in its own constructor.
# So by the time MainWindow constructor finishes InitializeComponent(), AppServices.Instance.BrowserAgent should be set!
# Let's add it right after InitializeComponent();
init_comp = r"InitializeComponent\(\);"
subscribe_code = "InitializeComponent();\n\n        if (AppServices.Instance.BrowserAgent != null)\n        {\n            AppServices.Instance.BrowserAgent.MessageReceived += OnBrowserAgentMessageReceived;\n        }"
content = re.sub(init_comp, subscribe_code, content, count=1)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("MainWindow.xaml.cs refactored (Phase 2).")
