using System;
using System.IO;
using System.Windows.Controls;
using AIAssistant.Core;

namespace AIAssistant.Features.WorkspaceAgent
{
    public partial class WorkspaceAgentControl : UserControl
    {
        public WorkspaceAgentControl()
        {
            InitializeComponent();
            
            this.Loaded += WorkspaceAgentControl_Loaded;
            AppServices.Instance.WorkspaceChanged += OnWorkspaceChanged;
        }

        private void WorkspaceAgentControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateWorkspaceText(AppServices.Instance.WorkspacePath);
        }

        private void OnWorkspaceChanged(object? sender, EventArgs e)
        {
            UpdateWorkspaceText(AppServices.Instance.WorkspacePath);
        }

        private void UpdateWorkspaceText(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                WorkspaceText.Text = "No workspace...";
            }
            else
            {
                WorkspaceText.Text = Path.GetFileName(path);
            }
        }

        private void CanvasSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FormatSelector == null) return;
            FormatSelector.Items.Clear();
            int index = CanvasSelector.SelectedIndex;
            if (index == 0) // Code Project
            {
                FormatSelector.Items.Add(new ComboBoxItem { Content = "Any" });
            }
            else if (index == 1) // Document
            {
                FormatSelector.Items.Add(new ComboBoxItem { Content = "DOCX" });
                FormatSelector.Items.Add(new ComboBoxItem { Content = "PDF" });
                FormatSelector.Items.Add(new ComboBoxItem { Content = "TXT" });
            }
            else if (index == 2) // Presentation
            {
                FormatSelector.Items.Add(new ComboBoxItem { Content = "PPTX" });
            }
            else if (index == 3) // Spreadsheet
            {
                FormatSelector.Items.Add(new ComboBoxItem { Content = "XLSX" });
                FormatSelector.Items.Add(new ComboBoxItem { Content = "CSV" });
            }
            FormatSelector.SelectedIndex = 0;
        }

        /// <summary>Gets the currently selected canvas type (e.g. "Code Project", "Document").</summary>
        public string CanvasType
        {
            get
            {
                if (CanvasSelector?.SelectedItem is ComboBoxItem item)
                    return item.Content?.ToString() ?? "Code Project";
                return "Code Project";
            }
        }

        /// <summary>Gets the currently selected output format (e.g. "Any", "DOCX", "PPTX").</summary>
        public string FormatType
        {
            get
            {
                if (FormatSelector?.SelectedItem is ComboBoxItem item)
                    return item.Content?.ToString() ?? "Any";
                return "Any";
            }
        }
    }
}
