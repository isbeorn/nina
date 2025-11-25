#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Globalization;

namespace System.Windows.Media
{
    public class ColorConverter
    {
        public object ConvertFrom(object value)
        {
            if (value is string str)
            {
                return ConvertFromString(str);
            }
            throw new NotSupportedException($"Cannot convert from {value?.GetType()}");
        }

        public static object ConvertFromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Color.FromArgb(0, 0, 0, 0);

            value = value.Trim();

            // Remove # if present
            if (value.StartsWith("#"))
                value = value.Substring(1);

            // Parse hex color: AARRGGBB or RRGGBB
            byte a = 255, r = 0, g = 0, b = 0;

            if (value.Length == 8) // AARRGGBB
            {
                a = byte.Parse(value.Substring(0, 2), NumberStyles.HexNumber);
                r = byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber);
                g = byte.Parse(value.Substring(4, 2), NumberStyles.HexNumber);
                b = byte.Parse(value.Substring(6, 2), NumberStyles.HexNumber);
            }
            else if (value.Length == 6) // RRGGBB
            {
                r = byte.Parse(value.Substring(0, 2), NumberStyles.HexNumber);
                g = byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber);
                b = byte.Parse(value.Substring(4, 2), NumberStyles.HexNumber);
            }
            else
            {
                throw new FormatException($"Invalid color format: {value}");
            }

            return Color.FromArgb(a, r, g, b);
        }

        public string ConvertToString(object value)
        {
            if (value is Color color)
            {
                return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            throw new NotSupportedException($"Cannot convert from {value?.GetType()}");
        }
    }
}
