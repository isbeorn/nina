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
using System.Collections.Generic;

namespace System.Windows.Media {
    /// <summary>
    /// DrawingVisual is a visual object that can be used to render graphics on screen
    /// </summary>
    public class DrawingVisual {
        internal List<DrawingOperation> Operations { get; } = new List<DrawingOperation>();

        public DrawingContext RenderOpen() {
            Operations.Clear();
            return new DrawingContext(this);
        }
    }

    internal class DrawingOperation {
        public enum OperationType {
            DrawImage,
            DrawLine
        }

        public OperationType Type { get; set; }
        public Imaging.BitmapSource Image { get; set; }
        public Rect Rect { get; set; }
        public Pen Pen { get; set; }
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }
    }

    /// <summary>
    /// DrawingContext is used to describe visual content
    /// </summary>
    public class DrawingContext : IDisposable {
        private readonly DrawingVisual _visual;

        internal DrawingContext(DrawingVisual visual) {
            _visual = visual;
        }

        public void DrawImage(Imaging.BitmapSource imageSource, Rect rectangle) {
            _visual.Operations.Add(new DrawingOperation {
                Type = DrawingOperation.OperationType.DrawImage,
                Image = imageSource,
                Rect = rectangle
            });
        }

        public void DrawLine(Pen pen, Point point1, Point point2) {
            _visual.Operations.Add(new DrawingOperation {
                Type = DrawingOperation.OperationType.DrawLine,
                Pen = pen,
                Point1 = point1,
                Point2 = point2
            });
        }

        public void DrawDrawing(Drawing drawing) {
            // Stub - drawing operations are not actually rendered in headless mode
            // This is called during image thumbnail creation but we handle it differently
        }

        public void Dispose() {
            // Context is closed
        }
    }
}
