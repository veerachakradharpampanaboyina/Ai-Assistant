using System;
using System.ComponentModel;
using AIAssistant;

namespace AIAssistant.Core
{
    public interface IAppContext : INotifyPropertyChanged
    {
        string WorkspacePath { get; }
        GitManager GitManager { get; }
        ArtifactManager ArtifactManager { get; }
        HookManager HookManager { get; }
        IBrowserAgent BrowserAgent { get; set; }

        void SetWorkspace(string path);
        
        event EventHandler WorkspaceChanged;
    }

    public class AppContext : IAppContext
    {
        private string _workspacePath = string.Empty;
        
        public string WorkspacePath => _workspacePath;
        public GitManager GitManager { get; } = new();
        public ArtifactManager ArtifactManager { get; } = new();
        public HookManager HookManager { get; } = new();
        public IBrowserAgent BrowserAgent { get; set; } = null!;

        public event EventHandler? WorkspaceChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetWorkspace(string path)
        {
            if (_workspacePath != path)
            {
                _workspacePath = path;
                GitManager.SetWorkspace(path);
                ArtifactManager.SetWorkspace(path);
                HookManager.SetWorkspace(path);
                
                WorkspaceChanged?.Invoke(this, EventArgs.Empty);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WorkspacePath)));
            }
        }
    }

    public static class AppServices
    {
        public static IAppContext Instance { get; } = new AppContext();

        public static T Get<T>() where T : class
        {
            if (typeof(T) == typeof(ArtifactManager)) return (T)(object)Instance.ArtifactManager;
            if (typeof(T) == typeof(GitManager)) return (T)(object)Instance.GitManager;
            if (typeof(T) == typeof(HookManager)) return (T)(object)Instance.HookManager;
            if (typeof(T) == typeof(IBrowserAgent)) return (T)(object)Instance.BrowserAgent;
            return null!;
        }
    }
}
