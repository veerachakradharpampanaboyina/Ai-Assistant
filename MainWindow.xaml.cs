using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.IO;
using NHotkey;
using NHotkey.Wpf;

namespace AIAssistant;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private ClipboardMonitor? _clipboardMonitor;
    private string _lastCopiedCode = string.Empty;

    private string _workspacePath = string.Empty;
    private bool _isAutoLoopActive = false;
    private int _autonomousStepCount = 0;
    private int _consecutiveErrors = 0;
    private List<string> _recentCommands = new List<string>();
    private const int MaxAutonomousSteps = int.MaxValue;

    public MainWindow()
    {
        InitializeComponent();
        
        try 
        {
            HotkeyManager.Current.AddOrReplace("ToggleApp", System.Windows.Input.Key.A, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift, OnToggleAppHotkey);
            HotkeyManager.Current.AddOrReplace("FixCode", System.Windows.Input.Key.F, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift, OnFixCodeHotkey);
        } 
        catch (Exception ex) 
        {
            Debug.WriteLine($"Could not register hotkey: {ex.Message}");
        }

        this.SourceInitialized += MainWindow_SourceInitialized;
    }

    private async void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            _clipboardMonitor = new ClipboardMonitor();
            _clipboardMonitor.ClipboardChanged += ClipboardMonitor_ClipboardChanged;
            _clipboardMonitor.Start(this);

            await webView.EnsureCoreWebView2Async(null);
            webView.WebMessageReceived += WebView_WebMessageReceived;
            webView.NavigationCompleted += WebView_NavigationCompleted;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MainWindow_SourceInitialized error: {ex.Message}");
        }
    }

    private void AiSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (webView == null || AiSelector == null) return;
        
        string? selectedAi = (AiSelector.SelectedItem as ComboBoxItem)?.Content?.ToString();
        
        string newUrl = "https://chatgpt.com";
        switch (selectedAi)
        {
            case "ChatGPT":
                newUrl = "https://chatgpt.com";
                break;
            case "Gemini":
                newUrl = "https://gemini.google.com";
                break;
            case "Claude":
                newUrl = "https://claude.ai";
                break;
            case "Arena AI":
                newUrl = "https://arena.ai/";
                break;
            case "DeepSeek":
                newUrl = "https://chat.deepseek.com";
                break;
            case "Z.ai":
                newUrl = "https://z.ai";
                break;
        }
        
        if (webView.Source?.ToString() != newUrl)
        {
            webView.Source = new Uri(newUrl);
        }
    }

    private async void ClipboardMonitor_ClipboardChanged(object? sender, EventArgs e)
    {
        try
        {
            if (!_isAutoLoopActive) return;

            if (System.Windows.Clipboard.ContainsText())
            {
                string text = System.Windows.Clipboard.GetText();
                Debug.WriteLine($"Clipboard updated: {text}");
                
                if (text.Contains("\"agent_commands\"") || text.Contains("\"\"agent_commands\"\""))
                {
                    LogAgentActivity("Detected JSON payload via Clipboard copy! Processing fallback...");
                    await ProcessAgentCommandAsync(text);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Clipboard error: {ex.Message}");
        }
    }

    private IntPtr _previousWindow;

    private void OnToggleAppHotkey(object? sender, HotkeyEventArgs e)
    {
        if (this.IsVisible && this.IsActive)
        {
            this.Hide();
            if (_previousWindow != IntPtr.Zero)
            {
                WindowAutomation.SetForegroundWindow(_previousWindow);
            }
        }
        else
        {
            _previousWindow = WindowAutomation.GetForegroundWindow();
            this.Show();
            this.Activate();
            this.Topmost = true;
        }
        e.Handled = true;
    }

    private async void OnFixCodeHotkey(object? sender, HotkeyEventArgs e)
    {
        try
        {
        // Get the active window which should be the IDE
        _previousWindow = WindowAutomation.GetForegroundWindow();
        if (_previousWindow == IntPtr.Zero || _previousWindow == new System.Windows.Interop.WindowInteropHelper(this).Handle)
        {
            return;
        }

        // Copy text from the IDE
        string copiedCode = await WindowAutomation.CopyTextFromWindowAsync(_previousWindow);
        if (string.IsNullOrWhiteSpace(copiedCode))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show("Could not copy any code. Please make sure you have highlighted the code in your IDE before pressing Ctrl+Shift+F.");
            });
            return;
        }

        // Show our app
        Application.Current.Dispatcher.Invoke(() =>
        {
            this.Show();
            this.Activate();
            this.Topmost = true;
        });

        // Store the exact copied code so we can apply fixes to it later
        _lastCopiedCode = copiedCode;

        // Add line numbers so the AI can precisely target lines
        string[] lines = copiedCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        for(int i = 0; i < lines.Length; i++)
        {
            lines[i] = $"{i + 1}: {lines[i]}";
        }
        string numberedCode = string.Join("\n", lines);

        // Send to AI
        string prompt = @"Please review the following code and errors. Fix the issues and return your fixes in a strict JSON format. 
You MUST provide your response inside a markdown code block labeled `json` like this:
```json
{
  ""replacements"": [
    {
      ""startLine"": 13,
      ""endLine"": 37,
      ""newCode"": ""// The new corrected code here""
    }
  ]
}
```
CRITICAL INSTRUCTIONS:
1. `newCode` MUST PRESERVE the exact original indentation of the code. Python is very strict about indentation.
2. `startLine` and `endLine` must exactly match the line numbers from the provided code snippet.
3. DO NOT include line numbers in `newCode`.
Do not provide the entire file, ONLY provide the specific lines that need to be replaced.
Here is the code snippet (with line numbers added for reference):

" + numberedCode;
        string safePrompt = System.Text.Json.JsonSerializer.Serialize(prompt);

        string js = $@"
            var safeText = {safePrompt};
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
                
                // Try to click the send button after a short delay
                setTimeout(() => {{
                    var sendBtn = document.querySelector('button[data-testid=""send-button""]') || 
                                  document.querySelector('button[aria-label*=""end"" i]') || 
                                  document.querySelector('button[aria-label*=""ubmit"" i]') || 
                                  document.querySelector('button[title*=""end"" i]') || 
                                  document.querySelector('div[role=""button""][aria-label*=""end"" i]') || 
                                  document.querySelector('.send-button');
                    
                    if (sendBtn && !sendBtn.disabled) {{
                        sendBtn.click();
                    }} else {{
                        if (el) el.dispatchEvent(new KeyboardEvent('keydown', {{ 'key': 'Enter', 'code': 'Enter', 'keyCode': 13, 'which': 13, 'bubbles': true }}));
                    }}
                }}, 500);
            }}
        ";
        
        await webView.ExecuteScriptAsync(js);
        e.Handled = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnFixCodeHotkey error: {ex.Message}");
        }
    }

    private void AutoFix_Click(object sender, RoutedEventArgs e)
    {
        // Manual trigger for auto fix from UI if needed
        System.Windows.MessageBox.Show("To auto-fix code, highlight the code in your IDE and press Ctrl+Shift+F.");
    }

    private async void InsertResponse_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        if (_previousWindow == IntPtr.Zero)
        {
            System.Windows.MessageBox.Show("No target window found. Please invoke the app via the hotkey (Ctrl+Shift+A) from another window.");
            return;
        }

        string textToInsert = await webView.ExecuteScriptAsync("window.getSelection().toString();");
        
        // Remove JS quotes
        textToInsert = System.Text.Json.JsonSerializer.Deserialize<string>(textToInsert) ?? "";

        if (string.IsNullOrWhiteSpace(textToInsert))
        {
            // If no text is selected, try to get the last code block
            string jsGetLastCodeBlock = @"
                (function() {
                    var codeBlocks = document.querySelectorAll('pre code');
                    if (codeBlocks.length > 0) {
                        return codeBlocks[codeBlocks.length - 1].innerText;
                    }
                    return '';
                })();
            ";
            string lastCodeBlock = await webView.ExecuteScriptAsync(jsGetLastCodeBlock);
            lastCodeBlock = System.Text.Json.JsonSerializer.Deserialize<string>(lastCodeBlock) ?? "";
            
            if (!string.IsNullOrWhiteSpace(lastCodeBlock))
            {
                textToInsert = lastCodeBlock;
            }
            else
            {
                System.Windows.MessageBox.Show("Please select some text in the AI chat first, or ensure the AI has provided a code block.");
                return;
            }
        }

        string jsonContent = string.Empty;
        var match = Regex.Match(textToInsert, @"```json\s*(\{.*?\})\s*```", RegexOptions.Singleline);
        if (match.Success)
        {
            jsonContent = match.Groups[1].Value;
        }
        else
        {
            // Try to extract just the JSON object if backticks are missing
            var fallbackMatch = Regex.Match(textToInsert, @"\{[\s\S]*\}", RegexOptions.Singleline);
            if (fallbackMatch.Success && fallbackMatch.Value.Contains("\"replacements\""))
            {
                jsonContent = fallbackMatch.Value;
            }
        }

        this.Hide();

        if (!string.IsNullOrWhiteSpace(jsonContent) && !string.IsNullOrWhiteSpace(_lastCopiedCode))
        {
            // Auto-apply fixes in-memory to the snippet
            string patchedSnippet = ApplyFixesToSnippet(_lastCopiedCode, jsonContent);
            await WindowAutomation.PasteTextIntoWindowAsync(_previousWindow, patchedSnippet);
        }
        else
        {
            // Fallback: paste the raw text exactly
            await WindowAutomation.PasteTextIntoWindowAsync(_previousWindow, textToInsert);
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
            var captureWindow = new ScreenCaptureWindow();
            if (captureWindow.ShowDialog() == true && captureWindow.CapturedImage != null)
            {
                string extractedText = await OcrHelper.ExtractTextAsync(captureWindow.CapturedImage);
                
                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    // Escape for JS
                    string safeText = System.Text.Json.JsonSerializer.Serialize(extractedText);
                    
                    // Try to inject into ChatGPT's textarea
                    // Try to inject into the text box
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
                        }}
                    ";
                    await webView.ExecuteScriptAsync(js);
                    
                    // Also copy to clipboard for convenience
                    System.Windows.Clipboard.SetText(extractedText);
                }
                else
                {
                    System.Windows.MessageBox.Show("No text could be extracted from the image.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ScreenCapture_Click error: {ex.Message}");
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
        if (_previousWindow != IntPtr.Zero)
        {
            WindowAutomation.SetForegroundWindow(_previousWindow);
        }
    }

    private void SetWorkspace_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Workspace Folder"
        };
        if (dialog.ShowDialog() == true)
        {
            _workspacePath = dialog.FolderName;
            WorkspaceText.Text = System.IO.Path.GetFileName(_workspacePath);
        }
    }

    private void AutoLoopToggle_Checked(object sender, RoutedEventArgs e)
    {
        _isAutoLoopActive = true;
    }

    private void AutoLoopToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        _isAutoLoopActive = false;
    }

    private void CanvasSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (FormatSelector == null) return;
        FormatSelector.Items.Clear();
        int index = CanvasSelector.SelectedIndex;
        if (index == 0) // Code Project
        {
            FormatSelector.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Any" });
        }
        else if (index == 1) // Document
        {
            FormatSelector.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "DOCX" });
            FormatSelector.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "PDF" });
            FormatSelector.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "TXT" });
        }
        else if (index == 2) // Presentation
        {
            FormatSelector.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "PPTX" });
        }
        else if (index == 3) // Spreadsheet
        {
            FormatSelector.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "XLSX" });
            FormatSelector.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "CSV" });
        }
        FormatSelector.SelectedIndex = 0;
    }

    private async void InitAgent_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        if (string.IsNullOrWhiteSpace(_workspacePath) || !Directory.Exists(_workspacePath))
        {
            System.Windows.MessageBox.Show("Please set a valid workspace first.");
            return;
        }

        _autonomousStepCount = 0;
        _consecutiveErrors = 0;
        _recentCommands.Clear();

        string canvasType = ((System.Windows.Controls.ComboBoxItem)CanvasSelector.SelectedItem).Content?.ToString() ?? "Code Project";
        string formatType = ((System.Windows.Controls.ComboBoxItem)FormatSelector.SelectedItem).Content?.ToString() ?? "Any";

        string projectScope = $"Your objective is to produce a {canvasType}.";
        if (formatType != "Any") projectScope += $" The final output MUST include a {formatType} file.";

        string userTask = UserPromptText.Text;
        string taskSection = string.IsNullOrWhiteSpace(userTask) ? 
            "Acknowledge this prompt by saying 'Agent Initialized. Waiting for task.'" : 
            $"USER TASK: {userTask}\nBegin your execution immediately by outputting the first module's JSON.";

        string prompt = $@"SYSTEM PROMPT: You are an elite Senior Full Stack Software Engineer with 20+ years of professional experience.

You are acting as an Autonomous AI Agent with full access to the user's workspace at: {_workspacePath}

{projectScope}
If your goal requires generating complex binary files like PPTX, DOCX, PDF, or XLSX, you MUST do so by:
1. Writing a Python script using the appropriate library (e.g. python-pptx, python-docx, pandas, reportlab).
2. Using the `run_command` action to install dependencies and execute your script.

CRITICAL DESIGN REQUIREMENT FOR DOCUMENTS:
You are an expert designer. When generating PPTX, DOCX, or PDF files, DO NOT output plain, unaligned text. You MUST use the Python libraries to explicitly set:
- Professional alignments (e.g., center alignment for titles, justified for paragraphs, proper margins).
- Rich typography (custom font sizes, bold/italic formatting, aesthetic RGB font colors).
- Visual Structure (bullet points, structured tables, proper line spacing, modern shapes).
Your generated documents must look premium, modern, and enterprise-ready.

Your goal is to build long-running tasks and full project implementations step-by-step.
Break down requests into a queue of modules. Think step-by-step. Implement ONE module at a time.
After completing a module, wait for the system to execute it and confirm success before providing the next module.

You MUST output your commands strictly in the following JSON format inside a markdown code block:
```json
{{
  ""is_dummy_example"": true,
  ""agent_commands"": [
    {{ ""action"": ""create_folder"", ""path"": ""my_new_app"" }},
    {{ ""action"": ""create_file"", ""path"": ""my_new_app/main.py"", ""content"": ""print('hello')"" }},
    {{ ""action"": ""edit_file"", ""path"": ""my_new_app/main.py"", ""startLine"": 1, ""endLine"": 2, ""newCode"": ""print('world')"" }},
    {{ ""action"": ""read_file"", ""path"": ""my_new_app/main.py"" }},
    {{ ""action"": ""list_dir"", ""path"": ""my_new_app"" }},
    {{ ""action"": ""run_command"", ""content"": ""pip install python-pptx && python my_new_app/main.py"" }},
    {{ ""action"": ""task_complete"", ""message"": ""All modules finished."" }}
  ]
}}
```

CRITICAL RULES:
1. NEVER output more than one JSON code block per response.
2. Ensure valid JSON (escape quotes and newlines in content).
3. Do NOT include the `""is_dummy_example""` flag in your real output.
4. Do not attempt to access files outside the workspace folder.
4. IMPORTANT: All `path` fields MUST be relative to the workspace root. DO NOT copy the dummy paths from the example above. Design your own file structure based on the USER TASK.
5. Once you issue commands, STOP and wait for the execution results from the system.
6. When the entire project is completed, you MUST issue the 'task_complete' action to terminate the auto-loop.

{taskSection}";

        await InjectToChat(prompt);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"InitAgent_Click error: {ex.Message}");
        }
    }

    private async void ForceExecution_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LogAgentActivity("Force Execution triggered. Extracting JSON manually...");
            string js = @"
                (function() {
                    var codeBlocks = document.querySelectorAll('pre');
                    if (codeBlocks.length > 0) {
                        window.chrome.webview.postMessage(codeBlocks[codeBlocks.length - 1].innerText);
                    } else {
                        var msgs = document.querySelectorAll('.agent-turn, .assistant-message, div[data-message-author-role=""assistant""], message-content, .md-content');
                        if (msgs.length > 0) {
                            window.chrome.webview.postMessage(msgs[msgs.length - 1].innerText);
                        } else {
                            window.chrome.webview.postMessage(document.body.innerText.slice(-20000));
                        }
                    }
                })();
            ";
            await webView.ExecuteScriptAsync(js);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ForceExecution_Click error: {ex.Message}");
        }
    }

    private async void WebView_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        try
        {
        string js = @"
            (function() {
                if (window.__aiAgentObserverStarted) return;
                window.__aiAgentObserverStarted = true;

                function log(msg) {
                    window.chrome.webview.postMessage('LOG:' + msg);
                }

                function getDeepText(node) {
                    let text = '';
                    if (node.nodeType === Node.TEXT_NODE) {
                        text += node.textContent;
                    } else if (node.nodeType === Node.ELEMENT_NODE || node.nodeType === Node.DOCUMENT_FRAGMENT_NODE) {
                        if (node.tagName && (node.tagName.toLowerCase() === 'br' || node.tagName.toLowerCase() === 'p' || node.tagName.toLowerCase() === 'div')) {
                            text += '\n';
                        }
                        if (node.shadowRoot) {
                            text += getDeepText(node.shadowRoot);
                        }
                        for (let child of node.childNodes) {
                            text += getDeepText(child);
                        }
                        if (node.tagName && (node.tagName.toLowerCase() === 'p' || node.tagName.toLowerCase() === 'div')) {
                            text += '\n';
                        }
                    }
                    return text;
                }

                function extractCombinedJson(text, searchRoot = document.body) {
                    let combinedCommands = [];
                    
                    function tryParse(jsonString) {
                        try {
                            let clean = jsonString
                                .replace(/[\u2018\u2019]/g, ""'"")
                                .replace(/[\u201C\u201D]/g, '""');
                            
                            // State machine to escape literal newlines inside string literals
                            let escaped = '';
                            let inString = false;
                            let escapeNext = false;
                            for (let i = 0; i < clean.length; i++) {
                                let char = clean[i];
                                if (inString) {
                                    if (escapeNext) {
                                        escapeNext = false;
                                        escaped += char;
                                    } else if (char === '\\') {
                                        escapeNext = true;
                                        escaped += char;
                                    } else if (char === '""') {
                                        inString = false;
                                        escaped += char;
                                    } else if (char === '\n') {
                                        escaped += '\\n';
                                    } else if (char === '\r') {
                                        escaped += '\\r';
                                    } else if (char === '\t') {
                                        escaped += '\\t';
                                    } else if (char.charCodeAt(0) < 32) {
                                        // Ignore other unescaped control chars
                                    } else {
                                        escaped += char;
                                    }
                                } else {
                                    if (char === '""') {
                                        inString = true;
                                    }
                                    escaped += char;
                                }
                            }
                            
                            return JSON.parse(escaped);
                        } catch (e) {
                            try { return JSON.parse(jsonString); } catch(e2) { 
                                let posMatch = e.message.match(/position (\d+)/);
                                if (posMatch) {
                                    let pos = parseInt(posMatch[1], 10);
                                    let snippet = jsonString.substring(Math.max(0, pos - 40), pos + 40).replace(/\n/g, '\\n').replace(/\r/g, '\\r');
                                    log('JSON parse error at pos ' + pos + ' (' + e.message + ') Context: ...' + snippet + '...');
                                }
                                return null; 
                            }
                        }
                    }

                    // Strategy 1: Target code blocks directly (Bypasses syntax highlighter DOM fragmentation)
                    if (searchRoot && searchRoot.querySelectorAll) {
                        let codeBlocks = searchRoot.querySelectorAll('pre, code, .code-block');
                        for (let block of codeBlocks) {
                            let blockText = block.innerText || block.textContent;
                            if (blockText && blockText.includes('""agent_commands""')) {
                                let parsed = tryParse(blockText);
                                if (parsed && parsed.agent_commands && !parsed.is_dummy_example) {
                                    log('Successfully parsed JSON from a code block element!');
                                    combinedCommands = combinedCommands.concat(parsed.agent_commands);
                                }
                            }
                        }
                    }

                    // Strategy 2: Regex on the raw text
                    if (combinedCommands.length === 0) {
                        let regex = /```json\s*(\{[\s\S]*?\})\s*```/gi;
                        let matches = [...text.matchAll(regex)];
                        if (matches.length > 0) {
                            for (let m of matches) {
                                let parsed = tryParse(m[1]);
                                if (parsed && !parsed.is_dummy_example && parsed.agent_commands) {
                                    combinedCommands = combinedCommands.concat(parsed.agent_commands);
                                }
                            }
                        }
                    } 
                    
                    // Strategy 3: Fallback brace matching on the raw text
                    if (combinedCommands.length === 0) {
                        let searchPos = 0;
                        let keywordIdx = text.indexOf('""agent_commands""', searchPos);
                        while (keywordIdx !== -1) {
                            let startIdx = text.lastIndexOf('{', keywordIdx);
                            if (startIdx !== -1) {
                                let braceCount = 0;
                                let foundStart = false;
                                for (let i = startIdx; i < text.length; i++) {
                                    if (text[i] === '{') { braceCount++; foundStart = true; }
                                    else if (text[i] === '}') { braceCount--; }
                                    
                                    if (foundStart && braceCount === 0) {
                                        let extracted = text.substring(startIdx, i + 1);
                                        let parsed = tryParse(extracted);
                                        if (parsed) {
                                            if (parsed.is_dummy_example) break;
                                            if (parsed.agent_commands) {
                                                combinedCommands = combinedCommands.concat(parsed.agent_commands);
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            searchPos = keywordIdx + 1;
                            keywordIdx = text.indexOf('""agent_commands""', searchPos);
                        }
                    }

                    if (combinedCommands.length > 0) {
                        return JSON.stringify({ agent_commands: combinedCommands });
                    }
                    return '';
                }

                let initialText = getDeepText(document.body).slice(-30000);
                let lastPostedJson = extractCombinedJson(initialText);
                let lastParsedJsonString = lastPostedJson;
                let stillnessTimer = null;
                
                log('AI Observer started. Initial JSON length: ' + lastPostedJson.length);

                function checkAndExtract() {
                    let textToPost = '';
                    var msgs = document.querySelectorAll('.agent-turn, .assistant-message, div[data-message-author-role=""assistant""], message-content, .md-content, .ds-message, div[class*=""assistant"" i], div[class*=""response"" i], .message.bot, .chatbot .message, .bot, div[class*=""message"" i][class*=""bot"" i]');
                    if (msgs.length > 0) {
                        for (let i = 0; i < msgs.length; i++) {
                            textToPost += msgs[i].innerText + '\n';
                        }
                    } else {
                        textToPost = getDeepText(document.body).slice(-30000);
                    }

                    if (!textToPost) return;

                    let currentJson = extractCombinedJson(textToPost);
                    
                    if (currentJson && currentJson !== lastParsedJsonString) {
                        log('Detected new JSON commands. Starting 4s timer...');
                        lastParsedJsonString = currentJson;
                        clearTimeout(stillnessTimer);
                        stillnessTimer = setTimeout(() => {
                            if (lastParsedJsonString !== lastPostedJson) {
                                lastPostedJson = lastParsedJsonString;
                                log('Timer finished. Submitting payload to C#!');
                                window.chrome.webview.postMessage(lastParsedJsonString);
                            }
                        }, 4000); // 4 seconds of JSON stillness
                    }
                }

                const observer = new MutationObserver(checkAndExtract);
                observer.observe(document.body, { childList: true, subtree: true, characterData: true });
                
                setInterval(checkAndExtract, 2000);
            })();
        ";
        await webView.ExecuteScriptAsync(js);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebView_NavigationCompleted error: {ex.Message}");
        }
    }

    private async void WebView_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            string message = e.TryGetWebMessageAsString();
            if (string.IsNullOrWhiteSpace(message)) return;

            if (message.StartsWith("LOG:"))
            {
                LogAgentActivity("JS Bridge: " + message.Substring(4));
                return;
            }

            if (!_isAutoLoopActive) return;

            await ProcessAgentCommandAsync(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebView_WebMessageReceived error: {ex.Message}");
        }
    }

    private void LogAgentActivity(string message)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            AgentLogList.Items.Add($"[{time}] {message}");
            if (AgentLogList.Items.Count > 1000) AgentLogList.Items.RemoveAt(0);
            AgentLogList.ScrollIntoView(AgentLogList.Items[AgentLogList.Items.Count - 1]);
        });
    }

    private async Task ProcessAgentCommandAsync(string textToInsert)
    {
        LogAgentActivity($"--- Message Received ({textToInsert.Length} chars) ---");
        
        string jsonContent = string.Empty;
        
        // Try parsing directly first (since JS now sends pure JSON)
        try {
            var directParse = JsonSerializer.Deserialize<AgentCommandsResponse>(textToInsert, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (directParse?.Agent_Commands != null && directParse.Agent_Commands.Count > 0) {
                jsonContent = textToInsert;
                LogAgentActivity("Found pure JSON directly.");
            }
        } catch { }

        if (string.IsNullOrEmpty(jsonContent))
        {
            var match = Regex.Match(textToInsert, @"```json\s*(\{[\s\S]*?\})\s*```", RegexOptions.RightToLeft);
            if (match.Success)
            {
                jsonContent = match.Groups[1].Value;
                LogAgentActivity("Found JSON via ```json block.");
            }
            else
            {
                var fallbackMatch = Regex.Match(textToInsert, @"\{[\s\S]*\}", RegexOptions.RightToLeft);
                if (fallbackMatch.Success && fallbackMatch.Value.Contains("\"agent_commands\""))
                {
                    jsonContent = fallbackMatch.Value;
                    LogAgentActivity("Found raw JSON (no backticks).");
                }
                else
                {
                    LogAgentActivity("FAILED to detect JSON in message.");
                    return;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(jsonContent)) return;

        string hash = GetHash(jsonContent);
        if (_recentCommands.Count > 0 && _recentCommands.Last() == hash)
        {
            _consecutiveErrors++;
            if (_consecutiveErrors >= 3)
            {
                LogAgentActivity("STUCK DETECTED. Sending warning to chat.");
                await InjectToChat("SYSTEM ERROR: You are stuck in a loop repeating the same exact JSON command 3 times. Please pause, re-evaluate, and change your strategy.");
                _consecutiveErrors = 0;
                return;
            }
        }
        else
        {
            _consecutiveErrors = 0;
            _recentCommands.Add(hash);
            if (_recentCommands.Count > 5) _recentCommands.RemoveAt(0);
        }

        _autonomousStepCount++;
        if (_autonomousStepCount > MaxAutonomousSteps)
        {
            _isAutoLoopActive = false;
            Application.Current.Dispatcher.Invoke(() => AutoLoopToggle.IsChecked = false);
            LogAgentActivity("Max autonomous steps reached. Halting.");
            System.Windows.MessageBox.Show("Max autonomous steps reached. Auto-Loop disabled.");
            return;
        }

        try
        {
            var fixData = JsonSerializer.Deserialize<AgentCommandsResponse>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (fixData?.Agent_Commands == null || fixData.Agent_Commands.Count == 0)
            {
                LogAgentActivity("JSON parsed but found 0 agent_commands.");
                return;
            }

            LogAgentActivity($"Parsed {fixData.Agent_Commands.Count} commands.");

            StringBuilder results = new StringBuilder();
            results.AppendLine("SYSTEM EXECUTION RESULTS:");

            foreach (var cmd in fixData.Agent_Commands)
            {
                if (cmd.Action == "task_complete")
                {
                    results.AppendLine($"- task_complete: {cmd.Message}");
                    LogAgentActivity($"TASK COMPLETE: {cmd.Message}");
                    _isAutoLoopActive = false;
                    Application.Current.Dispatcher.Invoke(() => AutoLoopToggle.IsChecked = false);
                    break;
                }

                string safePath = cmd.Path?.TrimStart('/', '\\') ?? "";
                string targetPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(_workspacePath, safePath));
                if (!targetPath.StartsWith(System.IO.Path.GetFullPath(_workspacePath), StringComparison.OrdinalIgnoreCase))
                {
                    string err = $"- ERROR: Path Traversal Blocked for {cmd.Path}";
                    results.AppendLine(err);
                    LogAgentActivity(err);
                    continue;
                }

                try
                {
                    switch (cmd.Action)
                    {
                        case "create_folder":
                            Directory.CreateDirectory(targetPath);
                            results.AppendLine($"- create_folder: {cmd.Path} (Success)");
                            LogAgentActivity($"Created folder: {cmd.Path}");
                            break;
                        case "create_file":
                            BackupFile(targetPath);
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targetPath)!);
                            File.WriteAllText(targetPath, cmd.Content);
                            results.AppendLine($"- create_file: {cmd.Path} (Success)");
                            LogAgentActivity($"Created file: {cmd.Path}");
                            break;
                        case "read_file":
                            if (File.Exists(targetPath))
                            {
                                string content = File.ReadAllText(targetPath);
                                string[] fileLines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                                for (int i = 0; i < fileLines.Length; i++)
                                {
                                    fileLines[i] = $"{i + 1}: {fileLines[i]}";
                                }
                                string numberedContent = string.Join("\n", fileLines);
                                results.AppendLine($"- read_file: {cmd.Path}\n```\n{numberedContent}\n```");
                                LogAgentActivity($"Read file: {cmd.Path}");
                            }
                            else
                            {
                                results.AppendLine($"- ERROR: read_file: {cmd.Path} (File not found)");
                                LogAgentActivity($"ERROR: read_file '{cmd.Path}' not found.");
                            }
                            break;
                        case "list_dir":
                            if (Directory.Exists(targetPath))
                            {
                                var files = Directory.GetFileSystemEntries(targetPath).Select(p => System.IO.Path.GetFileName(p));
                                results.AppendLine($"- list_dir: {cmd.Path}\n[{string.Join(", ", files)}]");
                                LogAgentActivity($"Listed directory: {cmd.Path}");
                            }
                            else
                            {
                                results.AppendLine($"- ERROR: list_dir: {cmd.Path} (Directory not found)");
                                LogAgentActivity($"ERROR: list_dir '{cmd.Path}' not found.");
                            }
                            break;
                        case "run_command":
                            if (cmd.Content != null)
                            {
                                var processInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + cmd.Content)
                                {
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    WorkingDirectory = _workspacePath
                                };
                                using (var proc = System.Diagnostics.Process.Start(processInfo))
                                {
                                    proc?.WaitForExit();
                                    string output = proc?.StandardOutput.ReadToEnd() ?? "";
                                    string error = proc?.StandardError.ReadToEnd() ?? "";
                                    results.AppendLine($"- run_command: {cmd.Content} (ExitCode: {proc?.ExitCode})");
                                    if (!string.IsNullOrWhiteSpace(output)) results.AppendLine($"OUTPUT:\n```\n{output}\n```");
                                    if (!string.IsNullOrWhiteSpace(error)) results.AppendLine($"ERROR:\n```\n{error}\n```");
                                    LogAgentActivity($"Ran command: {cmd.Content}");
                                }
                            }
                            break;
                        case "edit_file":
                            if (File.Exists(targetPath))
                            {
                                BackupFile(targetPath);
                                string originalContent = File.ReadAllText(targetPath);
                                string fakeJson = $@"{{ ""replacements"": [ {{ ""startLine"": {cmd.StartLine}, ""endLine"": {cmd.EndLine}, ""newCode"": {JsonSerializer.Serialize(cmd.NewCode)} }} ] }}";
                                string newContent = ApplyFixesToSnippet(originalContent, fakeJson);
                                File.WriteAllText(targetPath, newContent);
                                results.AppendLine($"- edit_file: {cmd.Path} (Success)");
                                LogAgentActivity($"Edited file: {cmd.Path}");
                            }
                            else
                            {
                                results.AppendLine($"- ERROR: edit_file: {cmd.Path} (File not found)");
                                LogAgentActivity($"ERROR: edit_file '{cmd.Path}' not found.");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"- ERROR on {cmd.Action}: {ex.Message}");
                    LogAgentActivity($"ERROR during {cmd.Action}: {ex.Message}");
                }
            }

            if (_isAutoLoopActive)
            {
                results.AppendLine();
                results.AppendLine("---");
                results.AppendLine("Please continue with the next step. REMINDER: You MUST output ONLY a valid JSON code block with an `agent_commands` array. Available actions: create_folder, create_file, edit_file, read_file, list_dir, run_command, task_complete.");
                
                LogAgentActivity("Sending results back to AI.");
                await InjectToChat(results.ToString());
            }
            else
            {
                LogAgentActivity("Auto-loop terminated. Final results not sent to chat.");
            }
        }
        catch (Exception ex)
        {
            LogAgentActivity($"JSON PARSE ERROR: {ex.Message}");
            await InjectToChat($"SYSTEM ERROR parsing JSON: {ex.Message}");
        }
    }

    private void BackupFile(string path)
    {
        if (File.Exists(path))
        {
            string backupDir = System.IO.Path.Combine(_workspacePath, ".agent_history");
            Directory.CreateDirectory(backupDir);
            string backupPath = System.IO.Path.Combine(backupDir, System.IO.Path.GetFileName(path) + "_" + DateTime.Now.Ticks + ".bak");
            File.Copy(path, backupPath);
        }
    }

    private string GetHash(string text)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }

    private async Task InjectToChat(string text)
    {
        string safeText = JsonSerializer.Serialize(text);
        string js = $@"
            var el = document.getElementById('prompt-textarea') || 
                     document.querySelector('div[contenteditable=""true""]') || 
                     document.querySelector('textarea');
            if (el) {{
                el.focus();
                if (!document.execCommand('insertText', false, {safeText})) {{
                    if (el.tagName === 'TEXTAREA') el.value = {safeText};
                    else el.textContent = {safeText};
                    el.dispatchEvent(new Event('input', {{ bubbles: true }}));
                }}
                setTimeout(() => {{
                    var sendBtn = document.querySelector('button[data-testid=""send-button""]') || 
                                  document.querySelector('button[aria-label*=""end"" i]') || 
                                  document.querySelector('button[aria-label*=""ubmit"" i]') || 
                                  document.querySelector('button[title*=""end"" i]') || 
                                  document.querySelector('div[role=""button""][aria-label*=""end"" i]') || 
                                  document.querySelector('.send-button');
                    
                    if (sendBtn && !sendBtn.disabled) {{
                        sendBtn.click();
                    }} else {{
                        if (el) el.dispatchEvent(new KeyboardEvent('keydown', {{ 'key': 'Enter', 'code': 'Enter', 'keyCode': 13, 'which': 13, 'bubbles': true }}));
                    }}
                }}, 500);
            }}
        ";
        await webView.ExecuteScriptAsync(js);
    }

    protected override void OnClosed(EventArgs e)
    {
        _clipboardMonitor?.Dispose();
        base.OnClosed(e);
    }

    private string ApplyFixesToSnippet(string originalSnippet, string jsonContent)
    {
        try
        {
            var fixData = JsonSerializer.Deserialize<AiFixResponse>(jsonContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (fixData?.Replacements == null || fixData.Replacements.Count == 0) 
            {
                System.Windows.MessageBox.Show("No valid replacements found in the AI response.");
                return originalSnippet;
            }

            var lines = originalSnippet.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            var sortedReplacements = fixData.Replacements.OrderByDescending(r => r.StartLine).ToList();

            foreach (var rep in sortedReplacements)
            {
                int startIndex = rep.StartLine - 1;
                int endIndex = rep.EndLine - 1;
                int linesToRemove = (endIndex - startIndex) + 1;

                if (startIndex >= 0 && startIndex < lines.Count)
                {
                    // Find original indentation
                    string originalIndent = new string(lines[startIndex].TakeWhile(char.IsWhiteSpace).ToArray());

                    lines.RemoveRange(startIndex, Math.Min(linesToRemove, lines.Count - startIndex));
                    var newLines = rep.NewCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

                    // Adjust indentation if necessary
                    if (newLines.Count > 0)
                    {
                        string newIndent = new string(newLines[0].TakeWhile(char.IsWhiteSpace).ToArray());
                        if (originalIndent.Length > newIndent.Length && originalIndent.StartsWith(newIndent))
                        {
                            string indentToAdd = originalIndent.Substring(newIndent.Length);
                            for (int i = 0; i < newLines.Count; i++)
                            {
                                if (!string.IsNullOrWhiteSpace(newLines[i])) // don't indent empty lines
                                    newLines[i] = indentToAdd + newLines[i];
                            }
                        }
                    }

                    lines.InsertRange(startIndex, newLines);
                }
            }

            return string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error applying fixes: {ex.Message}");
            return originalSnippet;
        }
    }
}

public class AiFixResponse
{
    public List<CodeReplacement>? Replacements { get; set; } = new List<CodeReplacement>();
}

public class CodeReplacement
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string NewCode { get; set; } = string.Empty;
}

public class AgentCommandsResponse
{
    public List<AgentCommand>? Agent_Commands { get; set; } = new List<AgentCommand>();
}

public class AgentCommand
{
    public string Action { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string NewCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}