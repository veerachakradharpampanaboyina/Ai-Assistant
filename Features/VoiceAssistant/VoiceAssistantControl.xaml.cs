using System;
using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AIAssistant.Features.VoiceAssistant
{
    public partial class VoiceAssistantControl : UserControl
    {
        public event EventHandler<string>? VoiceCommandReceived;

        private SpeechSynthesizer? _synthesizer;
        private bool _isListeningBackground = false;
        private Process? _bgVoiceProcess;

        public VoiceAssistantControl()
        {
            InitializeComponent();
            this.Loaded += VoiceAssistantControl_Loaded;
        }

        private async void VoiceAssistantControl_Loaded(object sender, RoutedEventArgs e)
        {
            await PopulateSettingsAsync();
        }

        private async Task PopulateSettingsAsync()
        {
            if (_synthesizer == null)
            {
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.SetOutputToDefaultAudioDevice();
            }

            if (VoiceSelector.Items.Count == 0)
            {
                var voices = _synthesizer.GetInstalledVoices();
                foreach (var v in voices)
                {
                    VoiceSelector.Items.Add(new ComboBoxItem { Content = v.VoiceInfo.Name, Tag = v.VoiceInfo.Name });
                }
                if (VoiceSelector.Items.Count > 0)
                    VoiceSelector.SelectedIndex = 0;
            }

            if (MicSelector.Items.Count == 0)
            {
                try
                {
                    string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice_capture.py");
                    if (!File.Exists(scriptPath)) 
                        scriptPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "voice_capture.py");
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"\"{scriptPath}\" --list-mics",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        string output = await process.StandardOutput.ReadToEndAsync();
                        var mics = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(output);
                        if (mics != null)
                        {
                            int defaultIndex = 0;
                            for (int i = 0; i < mics.Count; i++)
                            {
                                MicSelector.Items.Add(new ComboBoxItem { Content = mics[i], Tag = i });
                                if (mics[i].IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    defaultIndex = i;
                                }
                            }
                            if (MicSelector.Items.Count > 0)
                                MicSelector.SelectedIndex = defaultIndex;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load mics: {ex.Message}");
                }
            }
        }

        private async void VoiceCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VoiceCommandBtn.Content = "🎤 Listening...";
                VoiceCommandBtn.IsEnabled = false;

                bool wasBackgroundRunning = _isListeningBackground;
                if (wasBackgroundRunning)
                {
                    StopBackgroundListener();
                    await Task.Delay(500); 
                }

                string selectedLang = ((ComboBoxItem)LanguageSelector.SelectedItem).Content?.ToString() ?? "en-US";
                
                string micArg = "";
                if (MicSelector.SelectedItem is ComboBoxItem selectedMic && selectedMic.Tag != null)
                {
                    micArg = $" --mic-index={selectedMic.Tag}";
                }

                string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice_capture.py");
                if (!File.Exists(scriptPath)) 
                {
                    scriptPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "voice_capture.py");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" --lang={selectedLang}{micArg}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    if (!string.IsNullOrWhiteSpace(output) && !output.Contains("Error:"))
                    {
                        VoiceCommandReceived?.Invoke(this, output.Trim());
                    }
                }

                if (wasBackgroundRunning)
                {
                    StartBackgroundListener();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Voice Error: {ex.Message}");
            }
            finally
            {
                VoiceCommandBtn.Content = "🎤 Manual Voice";
                VoiceCommandBtn.IsEnabled = true;
            }
        }

        private void VoiceModeMainToggle_Checked(object sender, RoutedEventArgs e)
        {
            StartBackgroundListener();
        }

        private void VoiceModeMainToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            StopBackgroundListener();
        }

        private void StartBackgroundListener()
        {
            if (_isListeningBackground) return;
            _isListeningBackground = true;

            string selectedLang = "en-US";
            Application.Current.Dispatcher.Invoke(() => {
                selectedLang = ((ComboBoxItem)LanguageSelector.SelectedItem).Content?.ToString() ?? "en-US";
            });

            string micArg = "";
            Application.Current.Dispatcher.Invoke(() => {
                if (MicSelector.SelectedItem is ComboBoxItem selectedMic && selectedMic.Tag != null)
                {
                    micArg = $" --mic-index={selectedMic.Tag}";
                }
            });

            string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice_capture.py");
            if (!File.Exists(scriptPath)) 
            {
                scriptPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "voice_capture.py");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" --bg-listen --lang={selectedLang}{micArg}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                CreateNoWindow = true
            };

            try
            {
                _bgVoiceProcess = new Process { StartInfo = startInfo };
                _bgVoiceProcess.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        if (args.Data.StartsWith("WAKE_WORD_DETECTED:", StringComparison.OrdinalIgnoreCase))
                        {
                            string query = args.Data.Substring("WAKE_WORD_DETECTED:".Length).Trim();
                            if (!string.IsNullOrEmpty(query))
                            {
                                Application.Current.Dispatcher.InvokeAsync(() => {
                                    VoiceCommandReceived?.Invoke(this, query);
                                });
                            }
                        }
                    }
                };

                _bgVoiceProcess.Start();
                _bgVoiceProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start background voice listener: {ex.Message}");
            }
        }

        private void StopBackgroundListener()
        {
            _isListeningBackground = false;
            try
            {
                if (_bgVoiceProcess != null && !_bgVoiceProcess.HasExited)
                {
                    _bgVoiceProcess.Kill();
                    _bgVoiceProcess.Dispose();
                }
            }
            catch { }
            _bgVoiceProcess = null;
        }

        public void SpeakAloud(string text)
        {
            if (EnableTTSCheckbox.IsChecked != true) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                string engine = "Windows";
                string lang = "en-US";
                
                if (TTSEngineSelector.SelectedItem is ComboBoxItem engineItem && engineItem.Tag is string eTag)
                    engine = eTag;
                    
                if (LanguageSelector.SelectedItem is ComboBoxItem langItem && langItem.Content is string lTag)
                    lang = lTag;

                if (engine == "Google")
                {
                    Task.Run(() => 
                    {
                        try
                        {
                            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tts_temp.txt");
                            File.WriteAllText(tempFile, text);
                            string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "google_tts.py");
                            if (!File.Exists(scriptPath)) 
                                scriptPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "google_tts.py");

                            var startInfo = new ProcessStartInfo
                            {
                                FileName = "python",
                                Arguments = $"\"{scriptPath}\" \"{tempFile}\" \"{lang}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using var process = Process.Start(startInfo);
                            process?.WaitForExit();
                        }
                        catch { }
                    });
                }
                else
                {
                    if (_synthesizer == null) 
                    {
                        _synthesizer = new SpeechSynthesizer();
                        _synthesizer.SetOutputToDefaultAudioDevice();
                    }

                    if (VoiceSelector.SelectedItem is ComboBoxItem selectedVoice && selectedVoice.Tag is string voiceName)
                    {
                        try
                        {
                            _synthesizer.SelectVoice(voiceName);
                        }
                        catch { }
                    }
                    
                    _synthesizer.SpeakAsyncCancelAll();
                    _synthesizer.SpeakAsync(text);
                }
            });
        }

        public void SpeakAloudSync(string text)
        {
            bool isEnabled = false;
            string? voiceToUse = null;
            string engine = "Windows";
            string lang = "en-US";
            
            Application.Current.Dispatcher.Invoke(() => 
            {
                isEnabled = EnableTTSCheckbox.IsChecked == true;
                if (VoiceSelector.SelectedItem is ComboBoxItem selectedVoice && selectedVoice.Tag is string voiceName)
                    voiceToUse = voiceName;
                    
                if (TTSEngineSelector.SelectedItem is ComboBoxItem engineItem && engineItem.Tag is string eTag)
                    engine = eTag;
                    
                if (LanguageSelector.SelectedItem is ComboBoxItem langItem && langItem.Content is string lTag)
                    lang = lTag;
            });
            
            if (!isEnabled) return;

            if (engine == "Google")
            {
                try
                {
                    string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tts_temp_sync.txt");
                    File.WriteAllText(tempFile, text);
                    string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "google_tts.py");
                    if (!File.Exists(scriptPath)) 
                        scriptPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "google_tts.py");

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"\"{scriptPath}\" \"{tempFile}\" \"{lang}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var process = Process.Start(startInfo);
                    process?.WaitForExit();
                }
                catch { }
                return;
            }

            using var localSynth = new SpeechSynthesizer();
            localSynth.SetOutputToDefaultAudioDevice();
            
            if (!string.IsNullOrEmpty(voiceToUse))
            {
                try { localSynth.SelectVoice(voiceToUse); }
                catch { }
            }
            
            localSynth.Speak(text);
        }
    }
}
