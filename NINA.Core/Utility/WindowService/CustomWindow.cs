#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace NINA.Core.Utility.WindowService {

    public class CustomWindow : Window {
        public CustomWindow() {
            FixLayout();
        }

        public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(Window), null);

        public ICommand CloseCommand {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        private void FixLayout() {
            void Window_SourceInitialized(object sender, EventArgs e) {
                this.InvalidateMeasure();
                this.SourceInitialized -= Window_SourceInitialized;
            }

            this.SourceInitialized += Window_SourceInitialized;
        }

        private int? _dialogServiceId;
        private object _content;
        private object _dataContext;

        public string Title { get; set; }
        public object Content {
            get => _content;
            set {
                _content = value;
                if (System.Windows.DialogService.IsHeadless() && _dialogServiceId.HasValue) {
                    // Update dialog content when running headless
                    UpdateDialogServiceContent();
                }
            }
        }

        public object DataContext {
            get => _dataContext;
            set {
                _dataContext = value;
                if (System.Windows.DialogService.IsHeadless() && _dialogServiceId.HasValue) {
                    // Update dialog when DataContext changes
                    UpdateDialogServiceContent();
                }
            }
        }

        public bool? DialogResult { get; set; }

        public event EventHandler Closed;

        /// <summary>
        /// Show the window as a dialog
        /// </summary>
        public bool? ShowDialog() {
            if (System.Windows.DialogService.IsHeadless()) {
                return ShowViaDialogService();
            }

            // Try to use WPF window if available
            try {
                var windowType = Type.GetType("System.Windows.Window, PresentationFramework");
                if (windowType != null) {
                    var window = Activator.CreateInstance(windowType);

                    // Set properties
                    windowType.GetProperty("Title")?.SetValue(window, Title);
                    windowType.GetProperty("Content")?.SetValue(window, Content);
                    windowType.GetProperty("DataContext")?.SetValue(window, DataContext);

                    // Show dialog
                    var showDialogMethod = windowType.GetMethod("ShowDialog", Type.EmptyTypes);
                    var result = showDialogMethod?.Invoke(window, null);

                    return (bool?)result;
                }
            } catch (Exception ex) {
                Console.WriteLine($"CustomWindow: Failed to use WPF Window, falling back to DialogService: {ex.Message}");
            }

            // Fallback to DialogService
            return ShowViaDialogService();
        }

        private bool? ShowViaDialogService() {
            var dialog = new System.Windows.DialogService.DialogInfo {
                Title = Title ?? "Dialog",
                Message = ExtractMessageFromContent(),
                ContentType = DataContext?.GetType().FullName ?? Content?.GetType().FullName ?? "CustomWindow",
                DataContext = DataContext ?? Content,
                ResultCallback = (result) => {
                    DialogResult = result;
                    Closed?.Invoke(this, EventArgs.Empty);
                }
            };

            // Extract content information
            if (DataContext != null) {
                dialog.Content = ExtractPropertiesFromObject(DataContext);
            } else if (Content != null) {
                dialog.Content = ExtractPropertiesFromObject(Content);
            }

            _dialogServiceId = System.Windows.DialogService.RegisterDialog(dialog);

            // Extract and register buttons
            ExtractAndRegisterButtons();

            Console.WriteLine($"CustomWindow: Registered as DialogService #{_dialogServiceId}");

            // In headless mode, we don't block - return null to indicate dialog is open
            return null;
        }

        private void UpdateDialogServiceContent() {
            if (!_dialogServiceId.HasValue) return;

            var content = new Dictionary<string, object>();

            if (DataContext != null) {
                content = ExtractPropertiesFromObject(DataContext);
            } else if (Content != null) {
                content = ExtractPropertiesFromObject(Content);
            }

            System.Windows.DialogService.UpdateDialogContent(_dialogServiceId.Value, content);
        }

        private string ExtractMessageFromContent() {
            // Try to extract a meaningful message from Content or DataContext
            if (DataContext != null) {
                return ExtractMessage(DataContext);
            } else if (Content != null) {
                return ExtractMessage(Content);
            }
            return Title ?? "Dialog";
        }

        private string ExtractMessage(object obj) {
            if (obj == null) return "";

            var type = obj.GetType();

            // Look for common message properties
            var messageProp = type.GetProperty("Message") ??
                            type.GetProperty("Text") ??
                            type.GetProperty("Description") ??
                            type.GetProperty("Status");

            if (messageProp != null) {
                var value = messageProp.GetValue(obj);
                if (value != null) {
                    return value.ToString();
                }
            }

            return obj.ToString();
        }

        private Dictionary<string, object> ExtractPropertiesFromObject(object obj) {
            var properties = new Dictionary<string, object>();

            if (obj == null) return properties;

            try {
                var type = obj.GetType();
                foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
                    try {
                        // Skip command properties
                        if (prop.PropertyType.Name.Contains("Command")) {
                            continue;
                        }

                        var value = prop.GetValue(obj);
                        if (value != null && IsSimpleType(value.GetType())) {
                            properties[prop.Name] = value;
                        }
                    } catch {
                        // Skip properties that throw exceptions
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"CustomWindow: Error extracting properties: {ex.Message}");
            }

            return properties;
        }

        private bool IsSimpleType(Type type) {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(decimal) ||
                   type.IsEnum;
        }

        private void ExtractAndRegisterButtons() {
            if (!_dialogServiceId.HasValue) return;

            // Try to extract buttons from DataContext or Content
            var viewModel = DataContext ?? Content;
            if (viewModel == null) return;

            var type = viewModel.GetType();

            // Look for common button command properties
            var commandNames = new[] {
                "OkCommand", "CancelCommand", "YesCommand", "NoCommand",
                "AcceptCommand", "CloseCommand", "ContinueCommand", "SkipCommand",
                "SolveCommand", "AbortCommand"
            };

            foreach (var commandName in commandNames) {
                var commandProp = type.GetProperty(commandName);
                if (commandProp != null) {
                    var buttonName = commandName.Replace("Command", "");
                    var isCancel = buttonName.Equals("Cancel", StringComparison.OrdinalIgnoreCase) ||
                                 buttonName.Equals("No", StringComparison.OrdinalIgnoreCase);

                    System.Windows.DialogService.AddButton(_dialogServiceId.Value, buttonName, buttonName,
                        isDefault: buttonName.Equals("Ok", StringComparison.OrdinalIgnoreCase) ||
                                  buttonName.Equals("Yes", StringComparison.OrdinalIgnoreCase),
                        isCancel: isCancel);
                }
            }
        }

        /// <summary>
        /// Close the window
        /// </summary>
        public void Close() {
            if (_dialogServiceId.HasValue) {
                System.Windows.DialogService.CloseDialog(_dialogServiceId.Value, DialogResult ?? false);
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
