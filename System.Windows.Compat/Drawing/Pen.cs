#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OpenCvSharp;

namespace System.Drawing {
    /// <summary>
    /// Pen for drawing with OpenCV - stores color and thickness
    /// </summary>
    public class Pen : IDisposable {
        public Scalar Color { get; set; }
        public float Width { get; set; }
        public LineTypes LineType { get; set; }

        public Pen(Windows.Media.Brush brush, float width) {
            // Convert brush color to OpenCV Scalar
            if (brush is Windows.Media.SolidColorBrush solidBrush) {
                Color = solidBrush.Color;
            } else {
                Color = new Scalar(255, 255, 255); // Default white
            }
            Width = width;
            LineType = LineTypes.AntiAlias;
        }

        public Pen(Windows.Media.Brush brush) : this(brush, 1.0f) {
            // Default width of 1
        }

        public Pen(SolidBrush brush, float width) {
            // Convert System.Drawing.SolidBrush to OpenCV Scalar
            Color = brush.Color;
            Width = width;
            LineType = LineTypes.AntiAlias;
        }

        public Pen(SolidBrush brush) : this(brush, 1.0f) {
            // Default width of 1
        }

        public Pen(Windows.Media.Color color, float width) {
            Color = color;
            Width = width;
            LineType = LineTypes.AntiAlias;
        }

        public Pen(Color color, float width) {
            // Convert System.Drawing.Color to OpenCV Scalar (BGR format)
            Color = new Scalar(color.B, color.G, color.R, color.A);
            Width = width;
            LineType = LineTypes.AntiAlias;
        }

        public Pen(Color color) : this(color, 1.0f) {
            // Default width of 1
        }

        // Implicit conversion to Scalar for easy use with OpenCV
        public static implicit operator Scalar(Pen pen) => pen.Color;

        public void Dispose() {
            // No resources to dispose for now
        }
    }

    /// <summary>
    /// SolidBrush for OpenCV - stores a single color
    /// </summary>
    public class SolidBrush : IDisposable {
        public Windows.Media.Color Color { get; set; }

        public SolidBrush(Windows.Media.Color color) {
            Color = color;
        }

        // Constructor that accepts System.Drawing.Color for compatibility
        public SolidBrush(global::System.Drawing.Color color) {
            Color = Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        // Implicit conversion to Scalar for OpenCV
        public static implicit operator Scalar(SolidBrush brush) => brush.Color;

        public void Dispose() {
            // No resources to dispose
        }
    }

    /// <summary>
    /// Font for text rendering with OpenCV
    /// </summary>
    public class Font : IDisposable {
        public FontFamily FontFamily { get; set; }
        public float Size { get; set; }
        public FontStyle Style { get; set; }
        public GraphicsUnit Unit { get; set; }
        public HersheyFonts HersheyFont { get; set; }
        public double Scale { get; set; }

        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit) {
            FontFamily = family;
            Size = emSize;
            Style = style;
            Unit = unit;

            // Map to OpenCV Hershey fonts
            HersheyFont = HersheyFonts.HersheySimplex;

            // Calculate scale based on size (OpenCV uses different sizing)
            Scale = emSize / 24.0; // Normalize to a reasonable scale
        }

        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit)
            : this(new FontFamily(familyName), emSize, style, unit) { }

        public Font(string familyName, float emSize, FontStyle style)
            : this(new FontFamily(familyName), emSize, style, GraphicsUnit.Point) { }

        public Font(string familyName, float emSize)
            : this(new FontFamily(familyName), emSize, FontStyle.Regular, GraphicsUnit.Point) { }

        public Font(FontFamily family, float emSize, FontStyle style)
            : this(family, emSize, style, GraphicsUnit.Point) { }

        public Font(FontFamily family, float emSize)
            : this(family, emSize, FontStyle.Regular, GraphicsUnit.Point) { }

        public void Dispose() {
            // No resources to dispose
        }
    }

    /// <summary>
    /// Font family stub
    /// </summary>
    public class FontFamily {
        public string Name { get; set; }

        public FontFamily(string name) {
            Name = name;
        }
    }

    /// <summary>
    /// Font style enumeration
    /// </summary>
    [Flags]
    public enum FontStyle {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        Underline = 4,
        Strikeout = 8
    }

    /// <summary>
    /// Graphics unit enumeration
    /// </summary>
    public enum GraphicsUnit {
        World = 0,
        Display = 1,
        Pixel = 2,
        Point = 3,
        Inch = 4,
        Document = 5,
        Millimeter = 6
    }

    /// <summary>
    /// Static Brushes class with predefined brush colors
    /// </summary>
    public static class Brushes {
        public static Windows.Media.SolidColorBrush Transparent => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Transparent);
        public static Windows.Media.SolidColorBrush Black => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Black);
        public static Windows.Media.SolidColorBrush White => new Windows.Media.SolidColorBrush(Windows.Media.Colors.White);
        public static Windows.Media.SolidColorBrush Red => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Red);
        public static Windows.Media.SolidColorBrush Green => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Green);
        public static Windows.Media.SolidColorBrush Blue => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Blue);
        public static Windows.Media.SolidColorBrush Yellow => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Yellow);
        public static Windows.Media.SolidColorBrush Cyan => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Cyan);
        public static Windows.Media.SolidColorBrush Magenta => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Magenta);
        public static Windows.Media.SolidColorBrush Gray => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Gray);
        public static Windows.Media.SolidColorBrush Orange => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Orange);
        public static Windows.Media.SolidColorBrush Purple => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Purple);
        public static Windows.Media.SolidColorBrush Pink => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Pink);
        public static Windows.Media.SolidColorBrush Brown => new Windows.Media.SolidColorBrush(Windows.Media.Colors.Brown);
        public static Windows.Media.SolidColorBrush DarkBlue => new Windows.Media.SolidColorBrush(Windows.Media.Color.FromRgb(0, 0, 139));
        public static Windows.Media.SolidColorBrush DarkRed => new Windows.Media.SolidColorBrush(Windows.Media.Color.FromRgb(139, 0, 0));
        public static Windows.Media.SolidColorBrush LightYellow => new Windows.Media.SolidColorBrush(Windows.Media.Color.FromRgb(255, 255, 224));
    }
}
