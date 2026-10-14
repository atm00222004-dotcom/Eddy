using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using _8F.Models;

namespace _8F.Services
{

    public class OperatorActionMonitor
    {
        private readonly IOperatorActionRepository _repository;
        private readonly int _flushIntervalMs;
        private readonly Channel<OperatorActionEvent> _channel;
        private CancellationTokenSource? _cts;
        private Task? _backgroundTask;
        private volatile bool _isRunning;

        private const int MaxBatchSize = 200;

        #region Session State & Power Cut Detection

        private class SessionState
        {
            public Guid SessionId { get; set; }
            public DateTime StartTimeUtc { get; set; }
            public DateTime LastHeartbeatUtc { get; set; }
            public string? OperatorName { get; set; }
            public bool CleanExit { get; set; }
            public string? ExitReason { get; set; }
        }

        private readonly string _sessionFilePath;
        private readonly object _sessionLock = new();
        private SessionState? _currentSessionState;
        private System.Threading.Timer? _heartbeatTimer;

        #endregion

        #region Win32 Fallback for Cursor Screen Coordinates

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out Win32Point lpPoint);

        #endregion

        #region Word Typing Buffer

        private readonly StringBuilder _wordBuffer = new();
        private readonly object _wordLock = new();
        private System.Threading.Timer? _wordDebounceTimer;
        private string? _pendingWordControlName;
        private string? _pendingWordWindowTitle;
        private string? _pendingWordOperator;

        #endregion

        public Guid SessionId { get; } = Guid.NewGuid();

        public OperatorActionMonitor(IOperatorActionRepository repository, int flushIntervalMs = 1000)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _flushIntervalMs = flushIntervalMs > 0 ? flushIntervalMs : 1000;

            var eddyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Eddy");
            _sessionFilePath = Path.Combine(eddyFolder, "session_state.json");

            _channel = Channel.CreateBounded<OperatorActionEvent>(new BoundedChannelOptions(5000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        }
        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;
                _cts = new CancellationTokenSource();

                CheckPreviousSessionForPowerCut();

                _currentSessionState = new SessionState
                {
                    SessionId = SessionId,
                    StartTimeUtc = DateTime.UtcNow,
                    LastHeartbeatUtc = DateTime.UtcNow,
                    OperatorName = GetCurrentOperator(),
                    CleanExit = false
                };
                SaveSessionState(_currentSessionState);

                LogLifecycleEvent("ApplicationStarted", "Application", "Application Opened");

                _heartbeatTimer = new System.Threading.Timer(_ =>
                {
                    lock (_sessionLock)
                    {
                        if (_currentSessionState != null && !_currentSessionState.CleanExit)
                        {
                            _currentSessionState.LastHeartbeatUtc = DateTime.UtcNow;
                            _currentSessionState.OperatorName = GetCurrentOperator();
                            SaveSessionState(_currentSessionState);
                        }
                    }
                }, null, 5000, 5000);

                InputManager.Current.PreProcessInput += OnPreProcessInput;

                SystemEvents.PowerModeChanged += OnPowerModeChanged;
                SystemEvents.SessionEnding += OnSystemSessionEnding;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                if (Application.Current != null)
                {
                    Application.Current.SessionEnding += OnAppSessionEnding;
                    Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
                }

                _backgroundTask = Task.Run(ProcessQueueAsync);

                Debug.WriteLine($"[OperatorActionMonitor] Started auditing in-app operator actions. Session: {SessionId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OperatorActionMonitor] Start failed: {ex.Message}");
            }
        }
        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _isRunning = false;

                InputManager.Current.PreProcessInput -= OnPreProcessInput;

                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                SystemEvents.SessionEnding -= OnSystemSessionEnding;
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;

                if (Application.Current != null)
                {
                    Application.Current.SessionEnding -= OnAppSessionEnding;
                    Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
                }

                FlushWordBuffer();

                LogLifecycleEvent("ApplicationClosed", "Application", "Application Closed normally");
                MarkCleanExit("NormalExit");

                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;

                _channel.Writer.TryComplete();
                _cts?.Cancel();

                if (_backgroundTask != null)
                {
                    Task.WaitAny(new[] { _backgroundTask }, 3000);
                }

                lock (_wordLock)
                {
                    _wordDebounceTimer?.Dispose();
                    _wordDebounceTimer = null;
                }

                Debug.WriteLine("[OperatorActionMonitor] Stopped and flushed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OperatorActionMonitor] Stop failed: {ex.Message}");
            }
        }

        #region Power & Lifecycle Handlers

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            try
            {
                string detail = e.Mode switch
                {
                    PowerModes.StatusChange => "Power status changed (possible AC power loss or battery switch)",
                    PowerModes.Suspend => "System entering sleep / suspend mode",
                    PowerModes.Resume => "System resumed from sleep / suspend",
                    _ => $"Power mode: {e.Mode}"
                };

                LogLifecycleEvent("PowerModeChanged", "SystemPower", detail);
            }
            catch { }
        }

        private void OnSystemSessionEnding(object sender, SessionEndingEventArgs e)
        {
            try
            {
                LogLifecycleEvent("SystemShutdown", "SystemPower", $"System session ending: {e.Reason} (Shutdown / Logoff / Power Loss)");
                MarkCleanExit($"SystemShutdown: {e.Reason}");
            }
            catch { }
        }

        private void OnAppSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            try
            {
                LogLifecycleEvent("SystemShutdown", "SystemPower", $"Application session ending: {e.ReasonSessionEnding}");
                MarkCleanExit($"AppSessionEnding: {e.ReasonSessionEnding}");
            }
            catch { }
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            try
            {
                lock (_sessionLock)
                {
                    if (_currentSessionState != null && !_currentSessionState.CleanExit)
                    {
                        LogLifecycleEvent("ApplicationClosed", "Application", "Application Process Exit");
                        MarkCleanExit("ProcessExit");
                    }
                }
            }
            catch { }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                string msg = ex != null ? $"{ex.GetType().Name}: {ex.Message}" : "Fatal unhandled exception";
                LogLifecycleEvent("ApplicationCrash", "Application", $"Fatal Crash: {msg}");
                MarkCleanExit("Crash");
            }
            catch { }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                string msg = $"{e.Exception.GetType().Name}: {e.Exception.Message}";
                LogLifecycleEvent("ApplicationError", "Application", $"UI Dispatcher Error: {msg}");
            }
            catch { }
        }

        public void LogLifecycleEvent(string actionType, string controlName, string details)
        {
            try
            {
                var evt = new OperatorActionEvent
                {
                    EventTimestamp = DateTime.UtcNow,
                    ActionType = actionType,
                    MouseButton = null,
                    ControlName = controlName,
                    KeyName = details,
                    WindowTitle = GetSafeWindowTitle(GetActiveWindow()),
                    OperatorName = GetCurrentOperator(),
                    SessionId = SessionId,
                    ScreenX = null,
                    ScreenY = null
                };

                _repository.InsertEventSync(evt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OperatorActionMonitor] LogLifecycleEvent error: {ex.Message}");
            }
        }

        private void CheckPreviousSessionForPowerCut()
        {
            try
            {
                if (File.Exists(_sessionFilePath))
                {
                    var json = File.ReadAllText(_sessionFilePath);
                    var prevSession = JsonSerializer.Deserialize<SessionState>(json);
                    if (prevSession != null && !prevSession.CleanExit)
                    {
                        DateTime outageTime = prevSession.LastHeartbeatUtc != default ? prevSession.LastHeartbeatUtc : prevSession.StartTimeUtc;
                        var powerCutEvt = new OperatorActionEvent
                        {
                            EventTimestamp = outageTime,
                            ActionType = "PowerCutOrAbnormalShutdown",
                            MouseButton = null,
                            ControlName = "SystemPower",
                            KeyName = $"Abrupt termination detected: Session {prevSession.SessionId} lost power / ended abruptly at ~{outageTime:yyyy-MM-dd HH:mm:ss} UTC (Power Cut / Sudden Loss of Power)",
                            WindowTitle = "8F",
                            OperatorName = prevSession.OperatorName,
                            SessionId = prevSession.SessionId != Guid.Empty ? prevSession.SessionId : SessionId,
                            ScreenX = null,
                            ScreenY = null
                        };

                        _repository.InsertEventSync(powerCutEvt);
                        Debug.WriteLine($"[OperatorActionMonitor] Logged power cut for previous session {prevSession.SessionId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OperatorActionMonitor] CheckPreviousSessionForPowerCut error: {ex.Message}");
            }
        }

        private void MarkCleanExit(string reason)
        {
            lock (_sessionLock)
            {
                if (_currentSessionState != null)
                {
                    _currentSessionState.CleanExit = true;
                    _currentSessionState.LastHeartbeatUtc = DateTime.UtcNow;
                    _currentSessionState.ExitReason = reason;
                    SaveSessionState(_currentSessionState);
                }
            }
        }

        private void SaveSessionState(SessionState state)
        {
            try
            {
                var dir = Path.GetDirectoryName(_sessionFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_sessionFilePath, json);
            }
            catch { }
        }

        #endregion

        #region Centralized Input Event Handling

        private void OnPreProcessInput(object? sender, PreProcessInputEventArgs e)
        {
            try
            {
                if (!_isRunning) return;

                if (e.StagingItem.Input is MouseButtonEventArgs mouseArgs && mouseArgs.RoutedEvent == Mouse.PreviewMouseDownEvent)
                {
                    HandleMouseDown(mouseArgs);
                }
                else if (e.StagingItem.Input is TextCompositionEventArgs textArgs && textArgs.RoutedEvent == TextCompositionManager.PreviewTextInputEvent)
                {
                    HandleTextInput(textArgs);
                }
                else if (e.StagingItem.Input is KeyEventArgs keyArgs && keyArgs.RoutedEvent == Keyboard.PreviewKeyDownEvent)
                {
                    HandleKeyDown(keyArgs);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OperatorActionMonitor] PreProcessInput error: {ex.Message}");
            }
        }

        private void HandleMouseDown(MouseButtonEventArgs e)
        {
            FlushWordBuffer();

            var timestamp = DateTime.UtcNow;
            string button = e.ChangedButton.ToString(); // Left, Right, Middle

            DependencyObject? source = (e.OriginalSource as DependencyObject) 
                ?? (Mouse.DirectlyOver as DependencyObject);

            Window? window = GetWindowFromElement(source) ?? GetActiveWindow();
            string windowTitle = GetSafeWindowTitle(window);
            string controlName = ResolveControlName(source);
            string? operatorName = GetCurrentOperator();

            int? screenX = null;
            int? screenY = null;

            try
            {
                if (GetCursorPos(out var pt))
                {
                    screenX = pt.X;
                    screenY = pt.Y;
                }
            }
            catch { }

            if (!screenX.HasValue || !screenY.HasValue)
            {
                try
                {
                    if (window != null && window.IsLoaded)
                    {
                        Point posInWin = e.GetPosition(window);
                        Point screenPt = window.PointToScreen(posInWin);
                        screenX = (int)Math.Round(screenPt.X);
                        screenY = (int)Math.Round(screenPt.Y);
                    }
                }
                catch { }
            }

            var evt = new OperatorActionEvent
            {
                EventTimestamp = timestamp,
                ActionType = "Click",
                MouseButton = button,
                ControlName = controlName,
                KeyName = null,
                WindowTitle = windowTitle,
                OperatorName = operatorName,
                SessionId = SessionId,
                ScreenX = screenX,
                ScreenY = screenY
            };

            _channel.Writer.TryWrite(evt);
        }

        private void HandleTextInput(TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;

            if (IsPasswordContext())
            {
                RecordPasswordEntry();
                return;
            }

            if (e.Text == "\r" || e.Text == "\n" || e.Text == "\t")
            {
                FlushWordBuffer();
                return;
            }

            if (e.Text == " ")
            {
                FlushWordBuffer();
                return;
            }

            lock (_wordLock)
            {
                _wordBuffer.Append(e.Text);
                _pendingWordControlName = ResolveControlName(Keyboard.FocusedElement as DependencyObject ?? e.OriginalSource as DependencyObject);
                _pendingWordWindowTitle = GetSafeWindowTitle(GetActiveWindow());
                _pendingWordOperator = GetCurrentOperator();
            }

            ResetWordDebounceTimer();
        }

        private void HandleKeyDown(KeyEventArgs e)
        {
            var timestamp = DateTime.UtcNow;

            if (e.IsRepeat) return;

            if (IsPasswordContext())
            {
                if (e.Key == Key.Enter)
                {
                    RecordPasswordEntry();
                }
                return;
            }

            if (e.Key == Key.Back)
            {
                lock (_wordLock)
                {
                    if (_wordBuffer.Length > 0)
                    {
                        _wordBuffer.Remove(_wordBuffer.Length - 1, 1);
                        ResetWordDebounceTimer();
                        return;
                    }
                }
            }

            if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Tab || e.Key == Key.Space)
            {
                FlushWordBuffer();
            }

            IInputElement? focused = Keyboard.FocusedElement;
            bool isInsideTextControl = focused is TextBoxBase || (focused is ComboBox cb && cb.IsEditable);

            if (isInsideTextControl)
            {
                if (e.Key != Key.Enter && e.Key != Key.Escape && e.Key != Key.Tab && 
                    !(e.Key >= Key.F1 && e.Key <= Key.F24) &&
                    (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == 0)
                {
                    return;
                }
            }

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string pasted = Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(pasted))
                        {
                            FlushWordBuffer();
                            var pasteEvt = new OperatorActionEvent
                            {
                                EventTimestamp = timestamp,
                                ActionType = "WordTyped",
                                MouseButton = null,
                                ControlName = ResolveControlName(focused as DependencyObject),
                                KeyName = pasted.Trim(),
                                WindowTitle = GetSafeWindowTitle(GetActiveWindow()),
                                OperatorName = GetCurrentOperator(),
                                SessionId = SessionId,
                                ScreenX = null,
                                ScreenY = null
                            };
                            _channel.Writer.TryWrite(pasteEvt);
                            return;
                        }
                    }
                }
                catch { }
            }

            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin ||
                key == Key.None || key == Key.DeadCharProcessed)
            {
                return;
            }

            string? keyName = null;

            if (!isInsideTextControl && key == Key.B)
            {
                keyName = "B (Balance)";
            }
            else if (!isInsideTextControl && key == Key.Space)
            {
                keyName = "Space (Test)";
            }
            else if (!isInsideTextControl && key == Key.R)
            {
                keyName = "R (Reset Counters)";
            }
            else if (key >= Key.F1 && key <= Key.F24)
            {
                keyName = key.ToString();
            }
            else if (key == Key.Enter || key == Key.Return)
            {
                keyName = "Enter";
            }
            else if (key == Key.Escape)
            {
                keyName = "Escape";
            }
            else if (key == Key.Tab)
            {
                keyName = "Tab";
            }
            else if (key == Key.Back)
            {
                keyName = "Backspace";
            }
            else if (key == Key.Delete)
            {
                keyName = "Delete";
            }
            else if (key == Key.Insert)
            {
                keyName = "Insert";
            }
            else if (key == Key.Home)
            {
                keyName = "Home";
            }
            else if (key == Key.End)
            {
                keyName = "End";
            }
            else if (key == Key.PageUp)
            {
                keyName = "PageUp";
            }
            else if (key == Key.PageDown)
            {
                keyName = "PageDown";
            }
            else if (key == Key.Up)
            {
                keyName = "UpArrow";
            }
            else if (key == Key.Down)
            {
                keyName = "DownArrow";
            }
            else if (key == Key.Left)
            {
                keyName = "LeftArrow";
            }
            else if (key == Key.Right)
            {
                keyName = "RightArrow";
            }
            else if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != 0)
            {
                var mods = new List<string>();
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) mods.Add("Ctrl");
                if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) mods.Add("Alt");
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) mods.Add("Shift");
                keyName = $"{string.Join("+", mods)}+{key}";
            }
            else if (!isInsideTextControl)
            {
                keyName = key.ToString();
            }

            if (keyName == null) return;

            Window? activeWindow = GetActiveWindow();
            string windowTitle = GetSafeWindowTitle(activeWindow);
            string? operatorName = GetCurrentOperator();

            var evt = new OperatorActionEvent
            {
                EventTimestamp = timestamp,
                ActionType = "ShortcutKey",
                MouseButton = null,
                ControlName = ResolveControlName(focused as DependencyObject),
                KeyName = keyName,
                WindowTitle = windowTitle,
                OperatorName = operatorName,
                SessionId = SessionId,
                ScreenX = null,
                ScreenY = null
            };

            _channel.Writer.TryWrite(evt);
        }

        #endregion

        #region Word Buffer Management

        private void ResetWordDebounceTimer()
        {
            lock (_wordLock)
            {
                _wordDebounceTimer?.Dispose();
                _wordDebounceTimer = new System.Threading.Timer(_ => FlushWordBuffer(), null, 800, Timeout.Infinite);
            }
        }
        public void FlushWordBuffer()
        {
            string wordToLog;
            string? ctrl;
            string? winTitle;
            string? opName;

            lock (_wordLock)
            {
                _wordDebounceTimer?.Dispose();
                _wordDebounceTimer = null;

                if (_wordBuffer.Length == 0) return;

                wordToLog = _wordBuffer.ToString().Trim();
                _wordBuffer.Clear();

                ctrl = _pendingWordControlName;
                winTitle = _pendingWordWindowTitle;
                opName = _pendingWordOperator;

                _pendingWordControlName = null;
                _pendingWordWindowTitle = null;
                _pendingWordOperator = null;
            }

            if (string.IsNullOrEmpty(wordToLog)) return;

            var evt = new OperatorActionEvent
            {
                EventTimestamp = DateTime.UtcNow,
                ActionType = "WordTyped",
                MouseButton = null,
                ControlName = ctrl,
                KeyName = wordToLog,
                WindowTitle = winTitle ?? GetSafeWindowTitle(GetActiveWindow()),
                OperatorName = opName ?? GetCurrentOperator(),
                SessionId = SessionId,
                ScreenX = null,
                ScreenY = null
            };

            _channel.Writer.TryWrite(evt);
        }

        #endregion

        #region Helper Resolution Methods
        private static bool IsPasswordContext()
        {
            try
            {
                Window? activeWindow = GetActiveWindow();
                if (activeWindow != null)
                {
                    if (activeWindow is PasswordDialog || 
                        activeWindow.GetType().Name == "PasswordDialog" ||
                        string.Equals(activeWindow.Title, "Password Required", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                IInputElement? focused = Keyboard.FocusedElement;
                if (focused is PasswordBox) return true;

                if (focused is DependencyObject dep)
                {
                    DependencyObject? parent = dep;
                    int depth = 0;
                    while (parent != null && depth < 8)
                    {
                        if (parent is PasswordBox) return true;
                        parent = VisualTreeHelper.GetParent(parent) ?? LogicalTreeHelper.GetParent(parent);
                        depth++;
                    }
                }
            }
            catch { }

            return false;
        }

        private void RecordPasswordEntry()
        {
            var evt = new OperatorActionEvent
            {
                EventTimestamp = DateTime.UtcNow,
                ActionType = "PasswordEntry",
                MouseButton = null,
                ControlName = "PasswordBox",
                KeyName = "******",
                WindowTitle = GetSafeWindowTitle(GetActiveWindow()),
                OperatorName = GetCurrentOperator(),
                SessionId = SessionId,
                ScreenX = null,
                ScreenY = null
            };
            _channel.Writer.TryWrite(evt);
        }
        public static string ResolveControlName(DependencyObject? element)
        {
            if (element == null) return "Window Background";

            DependencyObject? curr = element;
            int depth = 0;
            string? bestLabel = null;
            string? bestName = null;
            string? controlType = null;

            while (curr != null && depth < 30)
            {
                if (curr is PasswordBox pb)
                {
                    return !string.IsNullOrEmpty(pb.Name) ? $"PasswordBox: {pb.Name}" : "PasswordBox";
                }

                if (curr is TextBox tb)
                {
                    return !string.IsNullOrEmpty(tb.Name) ? $"TextBox: {tb.Name}" : "TextBox";
                }

                if (curr is MenuItem mi)
                {
                    string header = mi.Header is string h ? h.Trim() : (mi.Header?.ToString() ?? "");
                    string name = !string.IsNullOrEmpty(mi.Name) ? mi.Name : "";
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(header))
                        return $"MenuItem: {name} (\"{header}\")";
                    if (!string.IsNullOrEmpty(header))
                        return $"MenuItem: \"{header}\"";
                    return "MenuItem";
                }

                if (curr is ButtonBase btn)
                {
                    controlType = btn.GetType().Name;
                    if (!string.IsNullOrEmpty(btn.Name)) bestName = btn.Name;

                    if (btn.Content is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        bestLabel = s.Trim().Replace("\r", " ").Replace("\n", " ");
                    }
                    else if (btn.ToolTip is string tt && !string.IsNullOrWhiteSpace(tt))
                    {
                        bestLabel = tt.Trim();
                    }
                    break;
                }

                if (curr is FrameworkElement fe)
                {
                    if (!string.IsNullOrEmpty(fe.Name) && bestName == null)
                    {
                        bestName = fe.Name;
                        controlType = fe.GetType().Name;
                    }

                    if (fe is Label lbl && lbl.Content != null && bestLabel == null)
                    {
                        string lblText = lbl.Content.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(lblText))
                        {
                            bestLabel = lblText.Replace("\r", " ").Replace("\n", " ");
                        }
                    }
                    else if (fe is TextBlock tbBlock && !string.IsNullOrWhiteSpace(tbBlock.Text) && bestLabel == null)
                    {
                        bestLabel = tbBlock.Text.Trim().Replace("\r", " ").Replace("\n", " ");
                    }
                }

                curr = VisualTreeHelper.GetParent(curr) ?? LogicalTreeHelper.GetParent(curr);
                depth++;
            }

            if (!string.IsNullOrEmpty(bestName) && !string.IsNullOrEmpty(bestLabel) && !string.Equals(bestName, bestLabel, StringComparison.OrdinalIgnoreCase))
            {
                return $"{bestName} (\"{bestLabel}\")";
            }
            if (!string.IsNullOrEmpty(bestLabel))
            {
                return $"\"{bestLabel}\"";
            }
            if (!string.IsNullOrEmpty(bestName))
            {
                return bestName;
            }
            if (!string.IsNullOrEmpty(controlType))
            {
                return controlType;
            }

            return element.GetType().Name;
        }

        private static Window? GetWindowFromElement(DependencyObject? element)
        {
            if (element == null) return null;
            return Window.GetWindow(element);
        }

        private static Window? GetActiveWindow()
        {
            if (Application.Current == null) return null;

            foreach (Window w in Application.Current.Windows)
            {
                if (w.IsActive) return w;
            }
            return Application.Current.MainWindow;
        }

        private static string GetSafeWindowTitle(Window? window)
        {
            if (window == null) return "8F";
            string title = window.Title;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = window.GetType().Name;
            }
            return title;
        }

        private static string? GetCurrentOperator()
        {
            try
            {
                string? op = DeviceCOM.part?.CheckedBy;
                if (!string.IsNullOrWhiteSpace(op)) return op.Trim();
            }
            catch { }
            return null;
        }

        #endregion

        #region Background Queue Processor

        private async Task ProcessQueueAsync()
        {
            var batch = new List<OperatorActionEvent>(MaxBatchSize);

            while (_isRunning || _channel.Reader.Count > 0)
            {
                try
                {
                    var timeoutTask = Task.Delay(_flushIntervalMs);

                    while (batch.Count < MaxBatchSize)
                    {
                        if (_channel.Reader.TryRead(out var evt))
                        {
                            batch.Add(evt);
                        }
                        else
                        {
                            var readTask = _channel.Reader.WaitToReadAsync().AsTask();
                            var completed = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

                            if (completed == timeoutTask)
                            {
                                break;
                            }

                            if (!await readTask.ConfigureAwait(false))
                            {
                                break;
                            }
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await _repository.InsertBatchAsync(batch).ConfigureAwait(false);
                        batch.Clear();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OperatorActionMonitor] Background flush loop error: {ex.Message}");
                    await Task.Delay(500).ConfigureAwait(false);
                }
            }

            while (_channel.Reader.TryRead(out var evt))
            {
                batch.Add(evt);
                if (batch.Count >= MaxBatchSize)
                {
                    try { await _repository.InsertBatchAsync(batch).ConfigureAwait(false); } catch { }
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                try { await _repository.InsertBatchAsync(batch).ConfigureAwait(false); } catch { }
            }
        }

        #endregion
    }
}
