#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;

namespace System.Windows {

    /// <summary>
    /// Platform-agnostic dialog service for tracking and managing dialogs
    /// Works on both Windows (WPF) and Linux (headless)
    /// </summary>
    public static class DialogService {

        private static readonly object _lock = new object();
        private static readonly Dictionary<int, DialogInfo> _activeDialogs = new Dictionary<int, DialogInfo>();
        private static int _nextDialogId = 1;

        /// <summary>
        /// Information about a dialog
        /// </summary>
        public class DialogInfo {
            public int DialogId { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public string ContentType { get; set; }
            public DateTime CreatedAt { get; set; }
            public Dictionary<string, object> Content { get; set; }
            public List<ButtonInfo> Buttons { get; set; }
            public object DataContext { get; set; }
            public Action<bool> ResultCallback { get; set; }

            public DialogInfo() {
                Content = new Dictionary<string, object>();
                Buttons = new List<ButtonInfo>();
                CreatedAt = DateTime.Now;
            }
        }

        /// <summary>
        /// Button information
        /// </summary>
        public class ButtonInfo {
            public string Name { get; set; }
            public string Text { get; set; }
            public bool IsDefault { get; set; }
            public bool IsCancel { get; set; }
            public Action OnClick { get; set; }
        }

        /// <summary>
        /// Register a new dialog
        /// </summary>
        public static int RegisterDialog(string title, string message, string contentType = null, object dataContext = null, Action<bool> resultCallback = null) {
            lock (_lock) {
                int dialogId = _nextDialogId++;

                var dialog = new DialogInfo {
                    DialogId = dialogId,
                    Title = title ?? "",
                    Message = message ?? "",
                    ContentType = contentType ?? "GenericDialog",
                    DataContext = dataContext,
                    ResultCallback = resultCallback
                };

                _activeDialogs[dialogId] = dialog;

                Console.WriteLine($"DialogService: Registered dialog #{dialogId} - {title}");
                return dialogId;
            }
        }

        /// <summary>
        /// Register a dialog with detailed information
        /// </summary>
        public static int RegisterDialog(DialogInfo dialog) {
            lock (_lock) {
                if (dialog.DialogId == 0) {
                    dialog.DialogId = _nextDialogId++;
                }

                _activeDialogs[dialog.DialogId] = dialog;

                Console.WriteLine($"DialogService: Registered dialog #{dialog.DialogId} - {dialog.Title}");
                return dialog.DialogId;
            }
        }

        /// <summary>
        /// Add a button to a dialog
        /// </summary>
        public static void AddButton(int dialogId, string name, string text, bool isDefault = false, bool isCancel = false, Action onClick = null) {
            lock (_lock) {
                if (_activeDialogs.TryGetValue(dialogId, out var dialog)) {
                    dialog.Buttons.Add(new ButtonInfo {
                        Name = name,
                        Text = text,
                        IsDefault = isDefault,
                        IsCancel = isCancel,
                        OnClick = onClick
                    });
                }
            }
        }

        /// <summary>
        /// Update dialog content
        /// </summary>
        public static void UpdateDialogContent(int dialogId, Dictionary<string, object> content) {
            lock (_lock) {
                if (_activeDialogs.TryGetValue(dialogId, out var dialog)) {
                    foreach (var kvp in content) {
                        dialog.Content[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Update dialog message/status
        /// </summary>
        public static void UpdateDialogMessage(int dialogId, string message) {
            lock (_lock) {
                if (_activeDialogs.TryGetValue(dialogId, out var dialog)) {
                    dialog.Message = message;
                    dialog.Content["Message"] = message;
                }
            }
        }

        /// <summary>
        /// Close a dialog with result
        /// </summary>
        public static bool CloseDialog(int dialogId, bool result = true) {
            lock (_lock) {
                if (_activeDialogs.TryGetValue(dialogId, out var dialog)) {
                    // Call the result callback if provided
                    dialog.ResultCallback?.Invoke(result);

                    _activeDialogs.Remove(dialogId);
                    Console.WriteLine($"DialogService: Closed dialog #{dialogId} with result: {result}");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Click a button on a dialog
        /// </summary>
        public static bool ClickButton(int dialogId, string buttonName) {
            lock (_lock) {
                if (_activeDialogs.TryGetValue(dialogId, out var dialog)) {
                    var button = dialog.Buttons.FirstOrDefault(b =>
                        b.Name.Equals(buttonName, StringComparison.OrdinalIgnoreCase) ||
                        b.Text.Equals(buttonName, StringComparison.OrdinalIgnoreCase));

                    if (button != null) {
                        Console.WriteLine($"DialogService: Clicking button '{buttonName}' on dialog #{dialogId}");
                        button.OnClick?.Invoke();

                        // If it's a cancel button, close with false result
                        if (button.IsCancel) {
                            CloseDialog(dialogId, false);
                        } else {
                            CloseDialog(dialogId, true);
                        }
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Get all active dialogs
        /// </summary>
        public static List<DialogInfo> GetAllDialogs() {
            lock (_lock) {
                // Return copies without the callback references (not serializable)
                return _activeDialogs.Values.Select(d => new DialogInfo {
                    DialogId = d.DialogId,
                    Title = d.Title,
                    Message = d.Message,
                    ContentType = d.ContentType,
                    CreatedAt = d.CreatedAt,
                    Content = new Dictionary<string, object>(d.Content),
                    Buttons = d.Buttons.Select(b => new ButtonInfo {
                        Name = b.Name,
                        Text = b.Text,
                        IsDefault = b.IsDefault,
                        IsCancel = b.IsCancel
                    }).ToList(),
                    DataContext = d.DataContext // Include DataContext for plugin compatibility
                }).ToList();
            }
        }

        /// <summary>
        /// Get a specific dialog by ID
        /// </summary>
        public static DialogInfo GetDialog(int dialogId) {
            lock (_lock) {
                if (_activeDialogs.TryGetValue(dialogId, out var dialog)) {
                    // Return a copy
                    return new DialogInfo {
                        DialogId = dialog.DialogId,
                        Title = dialog.Title,
                        Message = dialog.Message,
                        ContentType = dialog.ContentType,
                        CreatedAt = dialog.CreatedAt,
                        Content = new Dictionary<string, object>(dialog.Content),
                        Buttons = dialog.Buttons.Select(b => new ButtonInfo {
                            Name = b.Name,
                            Text = b.Text,
                            IsDefault = b.IsDefault,
                            IsCancel = b.IsCancel
                        }).ToList()
                    };
                }
                return null;
            }
        }

        /// <summary>
        /// Get count of active dialogs
        /// </summary>
        public static int GetDialogCount() {
            lock (_lock) {
                return _activeDialogs.Count;
            }
        }

        /// <summary>
        /// Close all dialogs
        /// </summary>
        public static int CloseAllDialogs(bool result = true) {
            lock (_lock) {
                var count = _activeDialogs.Count;
                var dialogIds = _activeDialogs.Keys.ToList();

                foreach (var id in dialogIds) {
                    CloseDialog(id, result);
                }

                Console.WriteLine($"DialogService: Closed all {count} dialogs with result: {result}");
                return count;
            }
        }

        /// <summary>
        /// Get dialogs by content type
        /// </summary>
        public static List<DialogInfo> GetDialogsByType(string contentType) {
            lock (_lock) {
                return _activeDialogs.Values
                    .Where(d => d.ContentType?.Contains(contentType, StringComparison.OrdinalIgnoreCase) == true)
                    .Select(d => new DialogInfo {
                        DialogId = d.DialogId,
                        Title = d.Title,
                        Message = d.Message,
                        ContentType = d.ContentType,
                        CreatedAt = d.CreatedAt,
                        Content = new Dictionary<string, object>(d.Content),
                        Buttons = d.Buttons.Select(b => new ButtonInfo {
                            Name = b.Name,
                            Text = b.Text,
                            IsDefault = b.IsDefault,
                            IsCancel = b.IsCancel
                        }).ToList()
                    }).ToList();
            }
        }

        /// <summary>
        /// Check if running in headless mode (no WPF UI)
        /// </summary>
        public static bool IsHeadless() {
#if NET48
            return false; // .NET Framework always has WPF available
#else
            try {
                // Check if System.Windows.Application type exists and has a Current instance
                var appType = Type.GetType("System.Windows.Application, PresentationFramework");
                if (appType == null) {
                    return true; // WPF not available
                }

                var currentProp = appType.GetProperty("Current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var current = currentProp?.GetValue(null);
                return current == null; // No WPF application running
            } catch {
                return true; // Assume headless if we can't determine
            }
#endif
        }
    }
}
