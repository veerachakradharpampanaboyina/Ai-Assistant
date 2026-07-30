using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIAssistant;

public class GitChange
{
    public string Status { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string StatusIcon => Status switch
    {
        "M" => "✏️",
        "A" => "➕",
        "D" => "🗑️",
        "?" => "❓",
        "U" => "⚠️",
        "R" => "🔄",
        _ => "📄"
    };
    public string StatusText => Status switch
    {
        "M" => "Modified",
        "A" => "Added",
        "D" => "Deleted",
        "?" => "Untracked",
        "U" => "Conflict",
        "R" => "Renamed",
        _ => "Unknown"
    };
    public bool IsStaged { get; set; }
}

public class GitLogEntry
{
    public string Hash { get; set; } = string.Empty;
    public string ShortHash => Hash.Length >= 7 ? Hash[..7] : Hash;
    public string Author { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class GitManager
{
    private string _workspacePath = string.Empty;
    public bool IsGitRepo { get; private set; }
    public string CurrentBranch { get; private set; } = string.Empty;
    public ObservableCollection<GitChange> StagedChanges { get; } = new();
    public ObservableCollection<GitChange> UnstagedChanges { get; } = new();
    public ObservableCollection<GitLogEntry> RecentCommits { get; } = new();
    public List<string> Branches { get; } = new();

    public void SetWorkspace(string path)
    {
        _workspacePath = path;
        IsGitRepo = Directory.Exists(Path.Combine(path, ".git"));
    }

    private async Task<(string output, string error, int exitCode)> RunGitAsync(string arguments)
    {
        if (string.IsNullOrEmpty(_workspacePath)) return ("", "No workspace", -1);

        var psi = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = _workspacePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null) return ("", "Failed to start git", -1);

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (output.Trim(), error.Trim(), process.ExitCode);
    }

    public async Task InitAsync()
    {
        var (output, _, exitCode) = await RunGitAsync("init");
        if (exitCode == 0) IsGitRepo = true;
    }

    public async Task RefreshStatusAsync()
    {
        if (!IsGitRepo) return;

        // Get current branch
        var (branch, _, _) = await RunGitAsync("branch --show-current");
        CurrentBranch = string.IsNullOrEmpty(branch) ? "HEAD" : branch;

        // Get branches
        var (branchList, _, _) = await RunGitAsync("branch --list --no-color");
        Branches.Clear();
        foreach (var b in branchList.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            Branches.Add(b.Trim().TrimStart('*').Trim());
        }

        // Get status
        var (status, _, _) = await RunGitAsync("status --porcelain=v1");
        StagedChanges.Clear();
        UnstagedChanges.Clear();

        foreach (var line in status.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length < 4) continue;
            char indexStatus = line[0];
            char workTreeStatus = line[1];
            string filePath = line[3..].Trim().Trim('"');

            if (indexStatus != ' ' && indexStatus != '?')
            {
                StagedChanges.Add(new GitChange
                {
                    Status = indexStatus.ToString(),
                    FilePath = filePath,
                    IsStaged = true
                });
            }

            if (workTreeStatus != ' ')
            {
                string st = workTreeStatus == '?' ? "?" : workTreeStatus.ToString();
                UnstagedChanges.Add(new GitChange
                {
                    Status = st,
                    FilePath = filePath,
                    IsStaged = false
                });
            }
        }

        // Get recent commits
        var (log, _, _) = await RunGitAsync("log --oneline -20 --format=\"%H||%an||%ad||%s\" --date=short");
        RecentCommits.Clear();
        foreach (var line in log.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split("||", 4);
            if (parts.Length >= 4)
            {
                RecentCommits.Add(new GitLogEntry
                {
                    Hash = parts[0].Trim('"'),
                    Author = parts[1],
                    Date = parts[2],
                    Message = parts[3].Trim('"')
                });
            }
        }
    }

    public async Task<string> StageFileAsync(string filePath)
    {
        var (output, error, exitCode) = await RunGitAsync($"add \"{filePath}\"");
        return exitCode == 0 ? "Staged" : error;
    }

    public async Task<string> UnstageFileAsync(string filePath)
    {
        var (output, error, exitCode) = await RunGitAsync($"reset HEAD \"{filePath}\"");
        return exitCode == 0 ? "Unstaged" : error;
    }

    public async Task<string> StageAllAsync()
    {
        var (output, error, exitCode) = await RunGitAsync("add -A");
        return exitCode == 0 ? "All staged" : error;
    }

    public async Task<string> CommitAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Empty commit message";
        var escapedMessage = message.Replace("\"", "\\\"");
        var (output, error, exitCode) = await RunGitAsync($"commit -m \"{escapedMessage}\"");
        return exitCode == 0 ? output : error;
    }

    public async Task<string> PushAsync()
    {
        var (output, error, exitCode) = await RunGitAsync("push");
        return exitCode == 0 ? (string.IsNullOrEmpty(output) ? "Pushed successfully" : output) : error;
    }

    public async Task<string> PullAsync()
    {
        var (output, error, exitCode) = await RunGitAsync("pull");
        return exitCode == 0 ? output : error;
    }

    public async Task<string> GetDiffAsync(string filePath = "")
    {
        string args = string.IsNullOrEmpty(filePath) ? "diff" : $"diff \"{filePath}\"";
        var (output, error, exitCode) = await RunGitAsync(args);
        return exitCode == 0 ? output : error;
    }

    public async Task<string> GetStagedDiffAsync()
    {
        var (output, error, exitCode) = await RunGitAsync("diff --cached");
        return exitCode == 0 ? output : error;
    }

    public async Task<string> CheckoutBranchAsync(string branchName)
    {
        var (output, error, exitCode) = await RunGitAsync($"checkout \"{branchName}\"");
        return exitCode == 0 ? output : error;
    }

    public async Task<string> CreateBranchAsync(string branchName)
    {
        var (output, error, exitCode) = await RunGitAsync($"checkout -b \"{branchName}\"");
        return exitCode == 0 ? output : error;
    }

    public async Task<string> GenerateCommitMessageContext()
    {
        var diff = await GetStagedDiffAsync();
        if (string.IsNullOrWhiteSpace(diff))
        {
            diff = await GetDiffAsync();
        }
        
        if (diff.Length > 5000) diff = diff[..5000] + "\n[...truncated]";
        
        return $"Based on the following git diff, generate a concise conventional commit message:\n\n{diff}";
    }
}
