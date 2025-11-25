#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace Microsoft.Win32 {
    public class FileDialog {
        public string FileName { get; set; }
        public string InitialDirectory { get; set; }
        public bool? ShowDialog() => null;
        public bool? ShowDialog(System.Windows.Window owner) => ShowDialog();
    }

    public class OpenFileDialog : FileDialog {
        public string Title { get; set; }
        public string Filter { get; set; }
        public string[] FileNames { get; set; } = new string[0];
        public bool Multiselect { get; set; }
        public bool CheckFileExists { get; set; } = true;
        public bool CheckPathExists { get; set; } = true;
        public string DefaultExt { get; set; }
    }

    public class SaveFileDialog : FileDialog {
        public string Title { get; set; }
        public string Filter { get; set; }
        public bool CheckPathExists { get; set; } = true;
        public string DefaultExt { get; set; }
        public bool OverwritePrompt { get; set; } = true;
    }

    public class OpenFolderDialog {
        public string Title { get; set; }
        public string FolderName { get; set; }
        public string InitialDirectory { get; set; }
        public bool Multiselect { get; set; }

        public bool? ShowDialog() {
            // In headless mode, return null (dialog cancelled)
            return null;
        }

        public bool? ShowDialog(System.Windows.Window owner) {
            return ShowDialog();
        }
    }
}
