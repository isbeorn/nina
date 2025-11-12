#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


namespace NINA.WPF.Base.Utility {
    /// <summary>
    /// This class holds all available postfixes for DataTemplates that are pluggable in different areas of the application
    /// The DataTemplates must define a Key with the pattern x:Key="[FullyQualifiedTypeName][_Postfix]"
    /// </summary>
    public static class DataTemplatePostfix {
        /// <summary>
        /// Dock panels inside the imaging tab
        /// </summary>
        public static readonly string Dockable = "_Dockable";
        /// <summary>
        /// SequenceEntities inside the mini sequencer on the imaging tab
        /// </summary>
        public static readonly string MiniSequence = "_Mini";
        /// <summary>
        /// Plugin Options page
        /// </summary>
        public static readonly string Options = "_Options";
        /// <summary>
        /// Camera details section
        /// </summary>
        public static readonly string CameraDetails = "_CameraDetails";
        /// <summary>
        /// Camera settings section
        /// </summary>
        public static readonly string CameraSettings = "_CameraSettings";
        /// <summary>
        /// Dome settings section
        /// </summary>
        public static readonly string DomeSettings = "_DomeSettings";
        /// <summary>
        /// Filter Wheel settings section
        /// </summary>
        public static readonly string FilterWheelSettings = "_FilterWheelSettings";
        /// <summary>
        /// Flat Device settings section
        /// </summary>
        public static readonly string FlatDeviceSettings = "_FlatDeviceSettings";
        /// <summary>
        /// Focuser settings section
        /// </summary>
        public static readonly string FocuserSettings = "_FocuserSettings";
        /// <summary>
        /// Guider settings section
        /// </summary>
        public static readonly string GuiderSettings = "_GuiderSettings";
        /// <summary>
        /// Rotator settings section
        /// </summary>
        public static readonly string RotatorSettings = "_RotatorSettings";
        /// <summary>
        /// Safety Monitor settings section
        /// </summary>
        public static readonly string SafetyMonitorSettings = "_SafetyMonitorSettings";
        /// <summary>
        /// Switch Hub page
        /// </summary>
        public static readonly string SwitchSettings = "_SwitchSettings";
        /// <summary>
        /// Telescope settings section
        /// </summary>
        public static readonly string TelescopeSettings = "_TelescopeSettings";
        /// <summary>
        /// Weather Data settings section
        /// </summary>
        public static readonly string WeatherDataSettings = "_WeatherDataSettings";
        /// <summary>
        /// GNSS source settings section
        /// </summary>
        public static readonly string GnssSourceSettings = "_GnssSourceSettings";
    }
}