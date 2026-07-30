using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIAssistant;

public enum ArtifactType
{
    Plan,
    Report,
    Screenshot,
    Recording,
    Diff,
    Markdown,
    Log,
    Conversation
}

public class ArtifactItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public ArtifactType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "active";
    public string TypeIcon => Type switch
    {
        ArtifactType.Plan => "📋",
        ArtifactType.Report => "📊",
        ArtifactType.Screenshot => "📸",
        ArtifactType.Recording => "🎥",
        ArtifactType.Diff => "📝",
        ArtifactType.Markdown => "📄",
        ArtifactType.Log => "📜",
        ArtifactType.Conversation => "💬",
        _ => "📦"
    };
    public string DisplayTitle => $"{TypeIcon} {Title}";
    public string TimeAgo
    {
        get
        {
            var diff = DateTime.Now - CreatedAt;
            if (diff.TotalMinutes < 1) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return $"{(int)diff.TotalDays}d ago";
        }
    }
}

public class PlanStep
{
    public int Index { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, running, success, failed
    public string StatusIcon => Status switch
    {
        "pending" => "⬜",
        "running" => "🔄",
        "success" => "✅",
        "failed" => "❌",
        _ => "⬜"
    };
    public string DisplayText => $"{StatusIcon} Step {Index + 1}: {Description}";
}

public class ConversationEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Workspace { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<string> Messages { get; set; } = new();
    public string DisplayText => $"[{Timestamp:HH:mm}] {Summary}";
}

public class ScheduledTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Repeat { get; set; } = "once"; // once, hourly, daily
    public bool IsActive { get; set; } = true;
    public string DisplayText => $"⏰ {Description} ({ScheduledTime:g})";
}

public class BackgroundTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Status { get; set; } = "running"; // running, completed, failed, cancelled
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public System.Diagnostics.Process? Process { get; set; }
    public string StatusIcon => Status switch
    {
        "running" => "🟢",
        "completed" => "✅",
        "failed" => "❌",
        "cancelled" => "⛔",
        _ => "⬜"
    };
    public string DisplayText => $"{StatusIcon} {Name}";
}

public class InboxItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Type { get; set; } = "info"; // info, warning, error, question
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = false;
    public string TypeIcon => Type switch
    {
        "info" => "ℹ️",
        "warning" => "⚠️",
        "error" => "❌",
        "question" => "❓",
        _ => "📩"
    };
    public string DisplayText => $"{TypeIcon} {Message}";
}

public class ArtifactManager
{
    private string _artifactDir = string.Empty;
    public ObservableCollection<ArtifactItem> Artifacts { get; } = new();
    public ObservableCollection<PlanStep> CurrentPlan { get; } = new();
    public ObservableCollection<ConversationEntry> Conversations { get; } = new();
    public ObservableCollection<ScheduledTask> ScheduledTasks { get; } = new();
    public ObservableCollection<BackgroundTask> BackgroundTasks { get; } = new();
    public ObservableCollection<InboxItem> Inbox { get; } = new();

    public void SetWorkspace(string workspacePath)
    {
        _artifactDir = Path.Combine(workspacePath, ".agent_history", "artifacts");
        Directory.CreateDirectory(_artifactDir);
        LoadArtifacts();
        LoadConversations(workspacePath);
    }

    public ArtifactItem CreateArtifact(ArtifactType type, string title, string content)
    {
        var artifact = new ArtifactItem
        {
            Type = type,
            Title = title,
            Content = content
        };

        string ext = type switch
        {
            ArtifactType.Screenshot => ".png",
            ArtifactType.Recording => ".webm",
            ArtifactType.Diff => ".diff",
            _ => ".md"
        };

        artifact.FilePath = Path.Combine(_artifactDir, $"{artifact.Id}_{SanitizeFileName(title)}{ext}");
        File.WriteAllText(artifact.FilePath, content);

        Artifacts.Insert(0, artifact);
        SaveArtifactIndex();
        return artifact;
    }

    public void SetPlan(List<string> steps)
    {
        CurrentPlan.Clear();
        for (int i = 0; i < steps.Count; i++)
        {
            CurrentPlan.Add(new PlanStep { Index = i, Description = steps[i] });
        }
    }

    public void UpdatePlanStep(int index, string status)
    {
        if (index >= 0 && index < CurrentPlan.Count)
        {
            CurrentPlan[index].Status = status;
            // Trigger UI refresh by replacing item
            var step = CurrentPlan[index];
            CurrentPlan.RemoveAt(index);
            CurrentPlan.Insert(index, step);
        }
    }

    public void AddConversation(string workspace, string summary, List<string> messages)
    {
        var entry = new ConversationEntry
        {
            Workspace = workspace,
            Summary = summary,
            Messages = messages
        };
        Conversations.Insert(0, entry);
        SaveConversations(workspace);
    }

    public void AddToInbox(string type, string message)
    {
        Inbox.Insert(0, new InboxItem { Type = type, Message = message });
    }

    public BackgroundTask AddBackgroundTask(string name, string command, System.Diagnostics.Process process)
    {
        var task = new BackgroundTask { Name = name, Command = command, Process = process };
        BackgroundTasks.Add(task);
        return task;
    }

    public void CompleteBackgroundTask(string id, string status)
    {
        var task = BackgroundTasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Status = status;
            int idx = BackgroundTasks.IndexOf(task);
            BackgroundTasks.RemoveAt(idx);
            BackgroundTasks.Insert(idx, task);
        }
    }

    public ScheduledTask AddScheduledTask(string description, string command, DateTime scheduledTime, string repeat = "once")
    {
        var task = new ScheduledTask
        {
            Description = description,
            Command = command,
            ScheduledTime = scheduledTime,
            Repeat = repeat
        };
        ScheduledTasks.Add(task);
        return task;
    }

    private void LoadArtifacts()
    {
        Artifacts.Clear();
        string indexPath = Path.Combine(_artifactDir, "index.json");
        if (File.Exists(indexPath))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<ArtifactItem>>(File.ReadAllText(indexPath));
                if (items != null)
                {
                    foreach (var item in items.OrderByDescending(a => a.CreatedAt))
                    {
                        Artifacts.Add(item);
                    }
                }
            }
            catch { }
        }
    }

    private void SaveArtifactIndex()
    {
        try
        {
            string indexPath = Path.Combine(_artifactDir, "index.json");
            var json = JsonSerializer.Serialize(Artifacts.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(indexPath, json);
        }
        catch { }
    }

    private void LoadConversations(string workspace)
    {
        Conversations.Clear();
        string convDir = Path.Combine(workspace, ".agent_history", "conversations");
        if (Directory.Exists(convDir))
        {
            try
            {
                string indexPath = Path.Combine(convDir, "index.json");
                if (File.Exists(indexPath))
                {
                    var entries = JsonSerializer.Deserialize<List<ConversationEntry>>(File.ReadAllText(indexPath));
                    if (entries != null)
                    {
                        foreach (var e in entries.OrderByDescending(c => c.Timestamp).Take(50))
                        {
                            Conversations.Add(e);
                        }
                    }
                }
            }
            catch { }
        }
    }

    private void SaveConversations(string workspace)
    {
        try
        {
            string convDir = Path.Combine(workspace, ".agent_history", "conversations");
            Directory.CreateDirectory(convDir);
            string indexPath = Path.Combine(convDir, "index.json");
            var json = JsonSerializer.Serialize(Conversations.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(indexPath, json);
        }
        catch { }
    }

    private string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalid.Contains(c)).Take(30).ToArray()).Replace(' ', '_').ToLower();
    }

    // --- Task Persistence (Resume interrupted tasks) ---
    public void SaveAgentState(string workspace, string taskDescription, int stepIndex, List<string> pendingActions)
    {
        try
        {
            string statePath = Path.Combine(workspace, ".agent_history", "agent_state.json");
            Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
            var state = new
            {
                Task = taskDescription,
                StepIndex = stepIndex,
                PendingActions = pendingActions,
                Timestamp = DateTime.Now
            };
            File.WriteAllText(statePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public (string task, int stepIndex, List<string> pendingActions)? LoadAgentState(string workspace)
    {
        try
        {
            string statePath = Path.Combine(workspace, ".agent_history", "agent_state.json");
            if (File.Exists(statePath))
            {
                var doc = JsonDocument.Parse(File.ReadAllText(statePath));
                var root = doc.RootElement;
                return (
                    root.GetProperty("Task").GetString() ?? "",
                    root.GetProperty("StepIndex").GetInt32(),
                    root.GetProperty("PendingActions").EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                );
            }
        }
        catch { }
        return null;
    }

    public void ClearAgentState(string workspace)
    {
        try
        {
            string statePath = Path.Combine(workspace, ".agent_history", "agent_state.json");
            if (File.Exists(statePath)) File.Delete(statePath);
        }
        catch { }
    }

    // --- Repository Indexing ---
    public async Task<Dictionary<string, List<string>>> BuildRepoIndexAsync(string workspace)
    {
        var index = new Dictionary<string, List<string>>();
        string[] ignoredDirs = { ".git", ".vs", "node_modules", "bin", "obj", ".agent_history", "packages", "__pycache__" };
        string[] codeExtensions = { ".cs", ".py", ".js", ".ts", ".jsx", ".tsx", ".html", ".css", ".json", ".xml", ".yaml", ".yml", ".md", ".sql", ".java", ".go", ".rs", ".cpp", ".c", ".h" };

        await Task.Run(() =>
        {
            try
            {
                foreach (var file in Directory.GetFiles(workspace, "*.*", SearchOption.AllDirectories))
                {
                    bool skip = false;
                    foreach (var dir in ignoredDirs)
                    {
                        if (file.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;

                    string ext = Path.GetExtension(file).ToLower();
                    if (!codeExtensions.Contains(ext)) continue;

                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length > 500_000) continue;

                        var symbols = new List<string>();
                        string content = File.ReadAllText(file);
                        string relativePath = Path.GetRelativePath(workspace, file);

                        // Extract class/function/method names
                        var classMatches = System.Text.RegularExpressions.Regex.Matches(content, @"(?:class|interface|struct|enum)\s+(\w+)");
                        foreach (System.Text.RegularExpressions.Match m in classMatches)
                            symbols.Add($"class:{m.Groups[1].Value}");

                        var funcMatches = System.Text.RegularExpressions.Regex.Matches(content, @"(?:def|function|func|fn)\s+(\w+)");
                        foreach (System.Text.RegularExpressions.Match m in funcMatches)
                            symbols.Add($"func:{m.Groups[1].Value}");

                        var methodMatches = System.Text.RegularExpressions.Regex.Matches(content, @"(?:public|private|protected|internal|static|async)\s+\w+\s+(\w+)\s*\(");
                        foreach (System.Text.RegularExpressions.Match m in methodMatches)
                            symbols.Add($"method:{m.Groups[1].Value}");

                        var exportMatches = System.Text.RegularExpressions.Regex.Matches(content, @"export\s+(?:default\s+)?(?:class|function|const|let|var)\s+(\w+)");
                        foreach (System.Text.RegularExpressions.Match m in exportMatches)
                            symbols.Add($"export:{m.Groups[1].Value}");

                        if (symbols.Count > 0)
                            index[relativePath] = symbols;
                    }
                    catch { }
                }
            }
            catch { }
        });

        // Save index
        try
        {
            string indexPath = Path.Combine(workspace, ".agent_history", "repo_index.json");
            Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
            File.WriteAllText(indexPath, JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }

        return index;
    }
}
