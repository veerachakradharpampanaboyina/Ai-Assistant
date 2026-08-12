import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# ============================================================
# RC-1: Make _isAutoLoopActive volatile
# ============================================================
content = content.replace(
    "    private bool _isAutoLoopActive = false;",
    "    private volatile bool _isAutoLoopActive = false;"
)

# ============================================================
# RC-2: Make _recentCommands thread-safe (use lock instead of ConcurrentQueue for simplicity)
# ============================================================
content = content.replace(
    "    private List<string> _recentCommands = new List<string>();",
    "    private readonly object _commandsLock = new object();\n    private List<string> _recentCommands = new List<string>();"
)

# ============================================================
# RC-4: Make _autonomousStepCount thread-safe
# ============================================================
content = content.replace(
    "    private int _autonomousStepCount = 0;",
    "    private volatile int _autonomousStepCount = 0;"
)
content = content.replace(
    "    private int _consecutiveErrors = 0;",
    "    private volatile int _consecutiveErrors = 0;"
)

# ============================================================
# Add SemaphoreSlim to prevent concurrent ProcessAgentCommandAsync
# ============================================================
content = content.replace(
    "    private const int MaxAutonomousSteps = int.MaxValue;",
    "    private const int MaxAutonomousSteps = int.MaxValue;\n    private readonly SemaphoreSlim _agentProcessLock = new SemaphoreSlim(1, 1);"
)

# ============================================================
# RC-5: Remove dynamic? field declarations that shadow XAML controls
# ============================================================
content = content.replace(
    """#pragma warning disable CS0169, CS0649
    private dynamic? VoiceCommandBtn;
    private dynamic? LanguageSelector;
    private dynamic? VoiceModeMainToggle;
#pragma warning restore CS0169, CS0649""",
    "    // Voice controls are now in VoiceAssistantControl (dynamic fields removed)"
)

# ============================================================
# RC-7: Fix BrowserAgent subscription timing - use Loaded event
# ============================================================
content = content.replace(
    """        InitializeComponent();

        if (AppServices.Instance.BrowserAgent != null)
        {
            AppServices.Instance.BrowserAgent.MessageReceived += OnBrowserAgentMessageReceived;
        }""",
    """        InitializeComponent();

        // Subscribe to BrowserAgent after Loaded to ensure XAML controls are initialized
        this.Loaded += (s, ev) =>
        {
            if (AppServices.Instance.BrowserAgent != null)
            {
                AppServices.Instance.BrowserAgent.MessageReceived += OnBrowserAgentMessageReceived;
            }
        };"""
)

# ============================================================
# BUG-2: Fix InjectToChat null crash
# ============================================================
content = content.replace(
    "        await AppServices.Instance.BrowserAgent?.ExecuteScriptAsync(js);",
    "        var browser = AppServices.Instance.BrowserAgent;\n        if (browser != null) await browser.ExecuteScriptAsync(js);"
)

# ============================================================
# RC-6: Remove duplicate voice loop from MainWindow
# The BackgroundListenLoop references VoiceCommandBtn, LanguageSelector, MicSelector
# which are now null dynamic fields -> crash. Replace with delegating to VoiceAssistantControl.
# ============================================================

# Replace the old StartBackgroundListener
content = content.replace(
    """    private void StartBackgroundListener()
    {
        if (_isListeningBackground) return;
        _isListeningBackground = true;
        _voiceCts = new CancellationTokenSource();
        Task.Run(() => BackgroundListenLoop(_voiceCts.Token));
    }""",
    """    private void StartBackgroundListener()
    {
        // Voice listening is now handled by VoiceAssistantControl
        // This is a no-op stub to prevent compile errors from legacy callers
    }"""
)

# Replace StopBackgroundListener
content = content.replace(
    """    private void StopBackgroundListener()
    {
        _isListeningBackground = false;
        _voiceCts?.Cancel();
    }""",
    """    private void StopBackgroundListener()
    {
        // Voice listening is now handled by VoiceAssistantControl
    }"""
)

# Fix the VoiceModeMainToggle handlers that reference EnableVoiceModeCheckbox
content = content.replace(
    """    private void VoiceModeMainToggle_Checked(object sender, RoutedEventArgs e)
    {
        EnableVoiceModeCheckbox.IsChecked = true;
        StartBackgroundListener();
    }

    private void VoiceModeMainToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        EnableVoiceModeCheckbox.IsChecked = false;
        StopBackgroundListener();
    }""",
    """    private void VoiceModeMainToggle_Checked(object sender, RoutedEventArgs e)
    {
        // Voice mode is handled by VoiceAssistantControl
    }

    private void VoiceModeMainToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        // Voice mode is handled by VoiceAssistantControl
    }"""
)

# Fix startup reference to EnableVoiceModeCheckbox and VoiceModeMainToggle
content = content.replace(
    "            if (EnableVoiceModeCheckbox.IsChecked == true || VoiceModeMainToggle.IsChecked == true)\n            {\n                StartBackgroundListener();\n            }",
    "            // Voice mode auto-start is handled by VoiceAssistantControl"
)

# ============================================================
# Wrap _recentCommands access with locks
# ============================================================
# In InitAgent_Click
content = content.replace(
    "        _recentCommands.Clear();",
    "        lock (_commandsLock) { _recentCommands.Clear(); }"
)

# In ProcessAgentCommandAsync - the block that checks for stuck loops
old_stuck = """        string hash = GetHash(jsonContent);
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
        }"""

new_stuck = """        string hash = GetHash(jsonContent);
        bool isStuck = false;
        lock (_commandsLock)
        {
            if (_recentCommands.Count > 0 && _recentCommands.Last() == hash)
            {
                _consecutiveErrors++;
                if (_consecutiveErrors >= 3)
                {
                    isStuck = true;
                    _consecutiveErrors = 0;
                }
            }
            else
            {
                _consecutiveErrors = 0;
                _recentCommands.Add(hash);
                if (_recentCommands.Count > 5) _recentCommands.RemoveAt(0);
            }
        }
        if (isStuck)
        {
            LogAgentActivity("STUCK DETECTED. Sending warning to chat.");
            await InjectToChat("SYSTEM ERROR: You are stuck in a loop repeating the same exact JSON command 3 times. Please pause, re-evaluate, and change your strategy.");
            return;
        }"""

content = content.replace(old_stuck, new_stuck)

# ============================================================
# Add SemaphoreSlim guard around ProcessAgentCommandAsync
# ============================================================
content = content.replace(
    """    private async Task ProcessAgentCommandAsync(string textToInsert)
    {
        LogAgentActivity($"--- Message Received ({textToInsert.Length} chars) ---");""",
    """    private async Task ProcessAgentCommandAsync(string textToInsert)
    {
        if (!await _agentProcessLock.WaitAsync(0)) 
        {
            LogAgentActivity("Agent command processing already in progress. Skipping duplicate.");
            return;
        }
        try
        {
        LogAgentActivity($"--- Message Received ({textToInsert.Length} chars) ---");"""
)

# Close the try block at the very end of ProcessAgentCommandAsync
# Find the closing catch block of ProcessAgentCommandAsync
content = content.replace(
    """        catch (Exception ex)
        {
            LogAgentActivity($"JSON PARSE ERROR: {ex.Message}");
            await InjectToChat($"SYSTEM ERROR parsing JSON: {ex.Message}");
        }
    }

    private void BackupFile(string path)""",
    """        catch (Exception ex)
        {
            LogAgentActivity($"JSON PARSE ERROR: {ex.Message}");
            await InjectToChat($"SYSTEM ERROR parsing JSON: {ex.Message}");
        }
        }
        finally
        {
            _agentProcessLock.Release();
        }
    }

    private void BackupFile(string path)"""
)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("MainWindow.xaml.cs - All race condition fixes applied.")
