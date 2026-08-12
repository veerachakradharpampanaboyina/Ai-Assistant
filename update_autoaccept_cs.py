import re

file_path = r"c:\Users\lokes\.gemini\antigravity-ide\scratch\AIAssistant\MainWindow.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Replace create_file
create_file_old = """                        case "create_file":
                            Application.Current.Dispatcher.Invoke(() => {
                                _pendingChanges.Add(new PendingChange {
                                    Action = "create",
                                    FilePath = targetPath,
                                    OriginalContent = "",
                                    NewContent = cmd.Content ?? ""
                                });
                            });
                            results.AppendLine($"- create_file: {cmd.Path} (Staged for user approval)");
                            LogAgentActivity($"Staged file creation: {cmd.Path}");
                            break;"""
                            
create_file_new = """                        case "create_file":
                            bool autoAcceptCreate = false;
                            Application.Current.Dispatcher.Invoke(() => autoAcceptCreate = AutoAcceptToggle.IsChecked == true);
                            
                            if (autoAcceptCreate)
                            {
                                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targetPath)!);
                                File.WriteAllText(targetPath, cmd.Content ?? "");
                                Application.Current.Dispatcher.Invoke(() => PopulateFileExplorer(_workspacePath));
                                results.AppendLine($"- create_file: {cmd.Path} (Auto-Accepted and saved)");
                                LogAgentActivity($"Auto-Accepted file creation: {cmd.Path}");
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() => {
                                    _pendingChanges.Add(new PendingChange {
                                        Action = "create",
                                        FilePath = targetPath,
                                        OriginalContent = "",
                                        NewContent = cmd.Content ?? ""
                                    });
                                });
                                results.AppendLine($"- create_file: {cmd.Path} (Staged for user approval)");
                                LogAgentActivity($"Staged file creation: {cmd.Path}");
                            }
                            break;"""
                            
content = content.replace(create_file_old, create_file_new)


# Replace delete_file
delete_file_old = """                        case "delete_file":
                            if (File.Exists(targetPath))
                            {
                                string originalContent = await File.ReadAllTextAsync(targetPath);
                                Application.Current.Dispatcher.Invoke(() => {
                                    _pendingChanges.Add(new PendingChange {
                                        Action = "delete",
                                        FilePath = targetPath,
                                        OriginalContent = originalContent,
                                        NewContent = ""
                                    });
                                });
                                results.AppendLine($"- delete_file: {cmd.Path} (Staged for user approval)");
                                LogAgentActivity($"Staged file deletion: {cmd.Path}");
                            }
                            else
                            {
                                results.AppendLine($"- ERROR: delete_file: {cmd.Path} (File not found)");
                            }
                            break;"""
                            
delete_file_new = """                        case "delete_file":
                            if (File.Exists(targetPath))
                            {
                                bool autoAcceptDelete = false;
                                Application.Current.Dispatcher.Invoke(() => autoAcceptDelete = AutoAcceptToggle.IsChecked == true);
                                
                                if (autoAcceptDelete)
                                {
                                    File.Delete(targetPath);
                                    Application.Current.Dispatcher.Invoke(() => PopulateFileExplorer(_workspacePath));
                                    results.AppendLine($"- delete_file: {cmd.Path} (Auto-Accepted and deleted)");
                                    LogAgentActivity($"Auto-Accepted file deletion: {cmd.Path}");
                                }
                                else
                                {
                                    string originalContent = await File.ReadAllTextAsync(targetPath);
                                    Application.Current.Dispatcher.Invoke(() => {
                                        _pendingChanges.Add(new PendingChange {
                                            Action = "delete",
                                            FilePath = targetPath,
                                            OriginalContent = originalContent,
                                            NewContent = ""
                                        });
                                    });
                                    results.AppendLine($"- delete_file: {cmd.Path} (Staged for user approval)");
                                    LogAgentActivity($"Staged file deletion: {cmd.Path}");
                                }
                            }
                            else
                            {
                                results.AppendLine($"- ERROR: delete_file: {cmd.Path} (File not found)");
                            }
                            break;"""
                            
content = content.replace(delete_file_old, delete_file_new)


# Replace edit_file
edit_file_old = """                        case "edit_file":
                            if (File.Exists(targetPath))
                            {
                                string originalContent = await File.ReadAllTextAsync(targetPath);
                                string fakeJson = $@"{{ ""replacements"": [ {{ ""startLine"": {cmd.StartLine}, ""endLine"": {cmd.EndLine}, ""newCode"": {JsonSerializer.Serialize(cmd.NewCode)} }} ] }}";
                                string newContent = await Task.Run(() => ApplyFixesToSnippet(originalContent, fakeJson));
                                
                                Application.Current.Dispatcher.Invoke(() => {
                                    _pendingChanges.Add(new PendingChange {
                                        Action = "edit",
                                        FilePath = targetPath,
                                        OriginalContent = originalContent,
                                        NewContent = newContent
                                    });
                                });

                                results.AppendLine($"- edit_file: {cmd.Path} (Staged for user approval)");
                                LogAgentActivity($"Staged file edit: {cmd.Path}");
                            }
                            else
                            {
                                results.AppendLine($"- ERROR: edit_file: {cmd.Path} (File not found)");
                                LogAgentActivity($"ERROR: edit_file '{cmd.Path}' not found.");
                            }
                            break;"""
                            
edit_file_new = """                        case "edit_file":
                            if (File.Exists(targetPath))
                            {
                                string originalContent = await File.ReadAllTextAsync(targetPath);
                                string fakeJson = $@"{{ ""replacements"": [ {{ ""startLine"": {cmd.StartLine}, ""endLine"": {cmd.EndLine}, ""newCode"": {JsonSerializer.Serialize(cmd.NewCode)} }} ] }}";
                                string newContent = await Task.Run(() => ApplyFixesToSnippet(originalContent, fakeJson));
                                
                                bool autoAcceptEdit = false;
                                Application.Current.Dispatcher.Invoke(() => autoAcceptEdit = AutoAcceptToggle.IsChecked == true);
                                
                                if (autoAcceptEdit)
                                {
                                    File.WriteAllText(targetPath, newContent);
                                    results.AppendLine($"- edit_file: {cmd.Path} (Auto-Accepted and saved)");
                                    LogAgentActivity($"Auto-Accepted file edit: {cmd.Path}");
                                }
                                else
                                {
                                    Application.Current.Dispatcher.Invoke(() => {
                                        _pendingChanges.Add(new PendingChange {
                                            Action = "edit",
                                            FilePath = targetPath,
                                            OriginalContent = originalContent,
                                            NewContent = newContent
                                        });
                                    });

                                    results.AppendLine($"- edit_file: {cmd.Path} (Staged for user approval)");
                                    LogAgentActivity($"Staged file edit: {cmd.Path}");
                                }
                            }
                            else
                            {
                                results.AppendLine($"- ERROR: edit_file: {cmd.Path} (File not found)");
                                LogAgentActivity($"ERROR: edit_file '{cmd.Path}' not found.");
                            }
                            break;"""
                            
content = content.replace(edit_file_old, edit_file_new)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("MainWindow.xaml.cs updated for auto-accept.")
