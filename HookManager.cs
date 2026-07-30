using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AIAssistant;

public class HookDefinition
{
    public string Event { get; set; } = string.Empty; // before_create_file, after_create_file, before_run_command, after_run_command, on_error
    public string Command { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class HookConfig
{
    public List<HookDefinition> Hooks { get; set; } = new();
}

public class SecurityConfig
{
    public List<string> UrlAllowlist { get; set; } = new();
    public List<string> UrlDenylist { get; set; } = new()
    {
        "*.bank.*",
        "*.paypal.com",
        "mail.google.com"
    };
    public List<string> CommandBlocklist { get; set; } = new()
    {
        "format c:",
        "rd /s /q c:\\",
        "rm -rf /",
        "del /f /s /q c:\\*",
        "shutdown",
        "reg delete",
        "net user"
    };
    public bool AllowFileCreation { get; set; } = true;
    public bool AllowFileDeletion { get; set; } = true;
    public bool AllowCommandExecution { get; set; } = true;
    public bool AllowBrowserNavigation { get; set; } = true;
    public bool AllowPackageInstall { get; set; } = true;
    public bool RequireConfirmationForDeletion { get; set; } = true;
    public bool RequireConfirmationForInstall { get; set; } = false;
}

public class WorkspaceConfig
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime LastOpened { get; set; } = DateTime.Now;
    public List<string> OpenFiles { get; set; } = new();
    public string DisplayText => $"📁 {Name} — {Path}";
}

public class HookManager
{
    private string _workspacePath = string.Empty;
    private HookConfig _config = new();
    private SecurityConfig _security = new();
    private List<WorkspaceConfig> _recentWorkspaces = new();

    private static string GlobalConfigDir => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AIAssistant");

    public SecurityConfig Security => _security;
    public HookConfig Hooks => _config;
    public List<WorkspaceConfig> RecentWorkspaces => _recentWorkspaces;

    public void SetWorkspace(string path)
    {
        _workspacePath = path;
        LoadHooks();
        LoadSecurity();
        LoadRecentWorkspaces();

        // Update recent workspaces
        var existing = _recentWorkspaces.FirstOrDefault(w => w.Path == path);
        if (existing != null)
        {
            existing.LastOpened = DateTime.Now;
        }
        else
        {
            _recentWorkspaces.Insert(0, new WorkspaceConfig
            {
                Path = path,
                Name = System.IO.Path.GetFileName(path),
                LastOpened = DateTime.Now
            });
        }
        if (_recentWorkspaces.Count > 10) _recentWorkspaces.RemoveAt(10);
        SaveRecentWorkspaces();
    }

    // --- Hooks ---
    public List<string> GetHookCommands(string eventName)
    {
        return _config.Hooks
            .Where(h => h.Event == eventName && h.Enabled)
            .Select(h => h.Command)
            .ToList();
    }

    private void LoadHooks()
    {
        try
        {
            string hooksPath = System.IO.Path.Combine(_workspacePath, "hooks.json");
            if (File.Exists(hooksPath))
            {
                _config = JsonSerializer.Deserialize<HookConfig>(File.ReadAllText(hooksPath)) ?? new HookConfig();
            }
        }
        catch { _config = new HookConfig(); }
    }

    public void SaveHooks()
    {
        try
        {
            string hooksPath = System.IO.Path.Combine(_workspacePath, "hooks.json");
            File.WriteAllText(hooksPath, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    // --- Security ---
    public bool IsUrlAllowed(string url)
    {
        // Check denylist first
        foreach (var pattern in _security.UrlDenylist)
        {
            if (MatchWildcard(url, pattern)) return false;
        }

        // If allowlist has entries, only those are allowed
        if (_security.UrlAllowlist.Count > 0)
        {
            foreach (var pattern in _security.UrlAllowlist)
            {
                if (MatchWildcard(url, pattern)) return true;
            }
            return false;
        }

        return true;
    }

    public bool IsCommandAllowed(string command)
    {
        if (!_security.AllowCommandExecution) return false;

        string lowerCmd = command.ToLower().Trim();
        foreach (var blocked in _security.CommandBlocklist)
        {
            if (lowerCmd.Contains(blocked.ToLower())) return false;
        }

        // Check for package install
        if (!_security.AllowPackageInstall)
        {
            if (lowerCmd.StartsWith("pip install") || lowerCmd.StartsWith("npm install") ||
                lowerCmd.StartsWith("dotnet add") || lowerCmd.StartsWith("yarn add"))
                return false;
        }

        return true;
    }

    private bool MatchWildcard(string text, string pattern)
    {
        string regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private void LoadSecurity()
    {
        try
        {
            string secPath = System.IO.Path.Combine(GlobalConfigDir, "security.json");
            if (File.Exists(secPath))
            {
                _security = JsonSerializer.Deserialize<SecurityConfig>(File.ReadAllText(secPath)) ?? new SecurityConfig();
            }
        }
        catch { _security = new SecurityConfig(); }
    }

    public void SaveSecurity()
    {
        try
        {
            Directory.CreateDirectory(GlobalConfigDir);
            string secPath = System.IO.Path.Combine(GlobalConfigDir, "security.json");
            File.WriteAllText(secPath, JsonSerializer.Serialize(_security, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    // --- Workspace Config ---
    private void LoadRecentWorkspaces()
    {
        try
        {
            Directory.CreateDirectory(GlobalConfigDir);
            string wsPath = System.IO.Path.Combine(GlobalConfigDir, "workspaces.json");
            if (File.Exists(wsPath))
            {
                _recentWorkspaces = JsonSerializer.Deserialize<List<WorkspaceConfig>>(File.ReadAllText(wsPath)) ?? new List<WorkspaceConfig>();
            }
        }
        catch { _recentWorkspaces = new List<WorkspaceConfig>(); }
    }

    private void SaveRecentWorkspaces()
    {
        try
        {
            Directory.CreateDirectory(GlobalConfigDir);
            string wsPath = System.IO.Path.Combine(GlobalConfigDir, "workspaces.json");
            File.WriteAllText(wsPath, JsonSerializer.Serialize(_recentWorkspaces, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public void SaveWorkspaceState(string workspacePath, List<string> openFiles)
    {
        var ws = _recentWorkspaces.FirstOrDefault(w => w.Path == workspacePath);
        if (ws != null)
        {
            ws.OpenFiles = openFiles;
            ws.LastOpened = DateTime.Now;
            SaveRecentWorkspaces();
        }
    }
}
