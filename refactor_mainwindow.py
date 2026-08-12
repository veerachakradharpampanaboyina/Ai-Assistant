import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Remove the dummy field
content = re.sub(r"private dynamic\? webView;\s*", "", content)

# Replace all occurrences of webView.ExecuteScriptAsync
content = content.replace("webView.ExecuteScriptAsync", "AppServices.Instance.BrowserAgent?.ExecuteScriptAsync")

# Remove AiSelector because that was moved to BrowserAgentControl (or WorkspaceAgent?)
# Let's check if AiSelector is still in MainWindow.xaml.cs
content = content.replace("if (webView == null || AiSelector == null) return;", "if (AppServices.Instance.BrowserAgent == null) return;")

# Also need to fix webView.Source assignment in MainWindow.xaml.cs (Wait, is this still there?)
# It's in `AiSelector_SelectionChanged`. Let's look for `AiSelector_SelectionChanged`.
# I should just delete `AiSelector_SelectionChanged` if it exists, since AiSelector is gone.
content = re.sub(r"private void AiSelector_SelectionChanged\([^)]+\)\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", "", content)

# There is a `WebView_NavigationCompleted` which we can remove as it was moved.
content = re.sub(r"private async void WebView_NavigationCompleted\([^)]+\)\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", "", content)

# I should route the MessageReceived event!
# In MainWindow constructor, we need to subscribe to AppServices.Instance.BrowserAgent.MessageReceived IF it's possible.
# Wait, AppServices.Instance.BrowserAgent might not be set in the constructor. We can subscribe to it during a Loaded event or in the constructor but since it's an interface on AppServices...
# Let's add a setup method or do it directly.

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("MainWindow.xaml.cs refactored (Phase 1).")
