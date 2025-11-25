#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace NINA.Properties
{

    /// <summary>
    /// Global application settings that are not tied to a specific profile
    /// </summary>
    [Serializable]
    [DataContract]
    public class Settings
    {

        private static Settings _default;

        /// <summary>
        /// Singleton instance for WPF-style Settings.Default access
        /// </summary>
        public static Settings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = Load();
                }
                return _default;
            }
        }

        /// <summary>
        /// Whether the sidebar is collapsed
        /// </summary>
        [DataMember]
        public bool CollapsedSidebar { get; set; } = false;

        /// <summary>
        /// Database location path
        /// </summary>
        [DataMember]
        public string DatabaseLocation { get; set; } = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "NINA.sqlite");

        /// <summary>
        /// Whether to update settings from older versions
        /// </summary>
        [DataMember]
        public bool UpdateSettings { get; set; } = true;

        /// <summary>
        /// Whether to use saved profile selection
        /// </summary>
        [DataMember]
        public bool UseSavedProfileSelection { get; set; } = false;

        /// <summary>
        /// Auto-update source (Release, Beta, or Nightly)
        /// </summary>
        [DataMember]
        public int AutoUpdateSource { get; set; } = 0; // Default to RELEASE

        /// <summary>
        /// Application font family name
        /// </summary>
        [DataMember]
        public string ApplicationFontFamily { get; set; } = "Segoe UI";

        /// <summary>
        /// Whether SGP server is enabled
        /// </summary>
        [DataMember]
        public bool SGPServerEnabled { get; set; } = false;

        /// <summary>
        /// Font stretch setting
        /// </summary>
        [DataMember]
        public string FontStretch { get; set; } = "Normal";

        /// <summary>
        /// Font style setting
        /// </summary>
        [DataMember]
        public string FontStyle { get; set; } = "Normal";

        /// <summary>
        /// Font weight setting
        /// </summary>
        [DataMember]
        public string FontWeight { get; set; } = "Normal";

        /// <summary>
        /// Maximum size of the image save queue
        /// </summary>
        [DataMember]
        public int SaveQueueSize { get; set; } = 2;

        /// <summary>
        /// Whether to use single dock layout
        /// </summary>
        [DataMember]
        public bool SingleDockLayout { get; set; } = false;

        /// <summary>
        /// Whether hardware acceleration is enabled
        /// </summary>
        [DataMember]
        public bool HardwareAcceleration { get; set; } = true;

        /// <summary>
        /// Plugin repositories (JSON array)
        /// </summary>
        [DataMember]
        public string PluginRepositories { get; set; } = "[\"https://nighttime-imaging.eu/wp-json/nina/v1\"]";

        /// <summary>
        /// Window placement XML data
        /// </summary>
        [DataMember]
        public string WindowPlacement { get; set; } = "";

        private static readonly string SettingsFilePath = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Settings.xml");

        /// <summary>
        /// Load the global app settings from disk
        /// </summary>
        public static Settings Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                // Return default settings if file doesn't exist
                return new Settings();
            }

            try
            {
                using (var fs = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var serializer = new DataContractSerializer(typeof(Settings));
                    return (Settings)serializer.ReadObject(fs);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load app settings from {SettingsFilePath}", ex);
                return new Settings();
            }
        }

        /// <summary>
        /// Save the global app settings to disk
        /// </summary>
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fs = new FileStream(SettingsFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var serializer = new DataContractSerializer(typeof(Settings));
                    using (var writer = XmlWriter.Create(fs, new XmlWriterSettings { Indent = true }))
                    {
                        serializer.WriteObject(writer, this);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save app settings to {SettingsFilePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// Reload the global app settings from disk
        /// </summary>
        public void Reload()
        {
            var reloaded = Load();
            // Copy properties from reloaded instance to this instance
            this.CollapsedSidebar = reloaded.CollapsedSidebar;
            this.DatabaseLocation = reloaded.DatabaseLocation;
            this.UpdateSettings = reloaded.UpdateSettings;
            this.UseSavedProfileSelection = reloaded.UseSavedProfileSelection;
            this.AutoUpdateSource = reloaded.AutoUpdateSource;
            this.ApplicationFontFamily = reloaded.ApplicationFontFamily;
            this.SGPServerEnabled = reloaded.SGPServerEnabled;
            this.FontStretch = reloaded.FontStretch;
            this.FontStyle = reloaded.FontStyle;
            this.FontWeight = reloaded.FontWeight;
            this.SaveQueueSize = reloaded.SaveQueueSize;
            this.SingleDockLayout = reloaded.SingleDockLayout;
            this.HardwareAcceleration = reloaded.HardwareAcceleration;
            this.PluginRepositories = reloaded.PluginRepositories;
            this.WindowPlacement = reloaded.WindowPlacement;
        }
    }
}
