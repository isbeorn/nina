#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System.Windows.Media;

namespace NINA.Utility.Profile {

    public interface IColorSchemaSettings : ISettings {
        Color AltBackgroundColor { get; set; }
        Color AltBorderColor { get; set; }
        Color AltButtonBackgroundColor { get; set; }
        Color AltButtonBackgroundSelectedColor { get; set; }
        Color AltButtonForegroundColor { get; set; }
        Color AltButtonForegroundDisabledColor { get; set; }
        ColorSchema AltColorSchema { get; set; }
        string AltColorSchemaName { get; set; }
        Color AltNotificationErrorColor { get; set; }
        Color AltNotificationErrorTextColor { get; set; }
        Color AltNotificationWarningColor { get; set; }
        Color AltNotificationWarningTextColor { get; set; }
        Color AltPrimaryColor { get; set; }
        Color AltSecondaryColor { get; set; }
        Color BackgroundColor { get; set; }
        Color BorderColor { get; set; }
        Color ButtonBackgroundColor { get; set; }
        Color ButtonBackgroundSelectedColor { get; set; }
        Color ButtonForegroundColor { get; set; }
        Color ButtonForegroundDisabledColor { get; set; }
        ColorSchema ColorSchema { get; set; }
        string ColorSchemaName { get; set; }
        ColorSchemas ColorSchemas { get; set; }
        Color NotificationErrorColor { get; set; }
        Color NotificationErrorTextColor { get; set; }
        Color NotificationWarningColor { get; set; }
        Color NotificationWarningTextColor { get; set; }
        Color PrimaryColor { get; set; }
        Color SecondaryColor { get; set; }
    }
}