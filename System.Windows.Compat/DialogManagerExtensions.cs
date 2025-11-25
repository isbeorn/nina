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

namespace TouchNStars.Utility {

    /// <summary>
    /// Extension methods for DialogManager to support DialogService (headless mode)
    /// This file can be conditionally compiled into plugins to add headless support
    /// </summary>
    public static class DialogManagerExtensions {

        /// <summary>
        /// Get dialogs from DialogService and convert to DialogManager.DialogInfo format
        /// </summary>
        public static List<object> GetDialogServiceDialogs() {
            var dialogs = new List<object>();

            try {
                var serviceDialogs = System.Windows.DialogService.GetAllDialogs();

                foreach (var serviceDialog in serviceDialogs) {
                    // Create a dynamic object that matches DialogManager.DialogInfo structure
                    var info = new Dictionary<string, object> {
                        ["WindowType"] = "DialogService",
                        ["Title"] = serviceDialog.Title,
                        ["ContentType"] = serviceDialog.ContentType,
                        ["IsCustomWindow"] = false,
                        ["HasDialogResult"] = true,
                        ["WindowHashCode"] = serviceDialog.DialogId,
                        ["DetectedAt"] = serviceDialog.CreatedAt,
                        ["Content"] = serviceDialog.Content,
                        ["AvailableCommands"] = serviceDialog.Buttons.Select(b => b.Text ?? b.Name).ToList(),
                        ["DataContext"] = null // Don't expose DataContext in headless mode
                    };

                    dialogs.Add(info);
                }
            } catch (Exception ex) {
                Console.WriteLine($"DialogManagerExtensions: Error getting DialogService dialogs: {ex.Message}");
            }

            return dialogs;
        }

        /// <summary>
        /// Close all DialogService dialogs
        /// </summary>
        public static int CloseAllDialogServiceDialogs(bool confirmResult = true) {
            try {
                return System.Windows.DialogService.CloseAllDialogs(confirmResult);
            } catch (Exception ex) {
                Console.WriteLine($"DialogManagerExtensions: Error closing DialogService dialogs: {ex}");
                return 0;
            }
        }

        /// <summary>
        /// Get count of DialogService dialogs
        /// </summary>
        public static int GetDialogServiceCount() {
            try {
                return System.Windows.DialogService.GetDialogCount();
            } catch (Exception ex) {
                Console.WriteLine($"DialogManagerExtensions: Error getting DialogService count: {ex}");
                return 0;
            }
        }

        /// <summary>
        /// Click a button in a DialogService dialog
        /// </summary>
        public static bool ClickDialogServiceButton(string windowTitle, string buttonIdentifier) {
            try {
                var serviceDialogs = System.Windows.DialogService.GetAllDialogs();
                foreach (var dialog in serviceDialogs) {
                    if (dialog.Title?.Contains(windowTitle, StringComparison.OrdinalIgnoreCase) == true) {
                        return System.Windows.DialogService.ClickButton(dialog.DialogId, buttonIdentifier);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"DialogManagerExtensions: Error clicking DialogService button: {ex}");
            }
            return false;
        }
    }
}
