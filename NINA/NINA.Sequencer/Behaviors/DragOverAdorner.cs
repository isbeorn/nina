#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace NINA.Sequencer.Behaviors {

    internal class DragOverAdorner : Adorner {
        private readonly DropTargetEnum dropTargetEnum;
        public double AdornerWidth;
        public double AdornerHeight;
        public double ElementWidth;
        public double ElementHeight;
        public int TextSpacing = 15;
        public int HighlightWidth = 20;
        public bool LeftOfElement;
        private FormattedText textToWrite;
        private FormattedText iconToWrite;

        public DragOverAdorner(double width, double actualHeight, string text, bool leftOfElement, DropTargetEnum dropTargetEnum, UIElement adornedElement) : base(adornedElement) {
            var fbrush = FindResource("NotificationErrorTextBrush") as SolidColorBrush;
            LeftOfElement = leftOfElement;
            textToWrite = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal, new FontFamily("Microsoft YaHei UI"))
                , 14, fbrush, 1);
            string iconToWriteText = null;
            switch (dropTargetEnum) {
                case DropTargetEnum.Top: iconToWriteText = "↑"; break;
                case DropTargetEnum.Bottom: iconToWriteText = "↓"; break;
                case DropTargetEnum.Center: iconToWriteText = LeftOfElement ? "→" : "←"; break;
                default: iconToWriteText = ""; break;
            }
            iconToWrite = new FormattedText(iconToWriteText, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Expanded, new FontFamily("Microsoft YaHei UI"))
                , 24, fbrush, 1);
            AdornerWidth = width + textToWrite.Width + TextSpacing * 4 + iconToWrite.Width + HighlightWidth;
            AdornerHeight = textToWrite.Height + 10;
            ElementWidth = width;
            ElementHeight = actualHeight;
            this.dropTargetEnum = dropTargetEnum;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            var textBgBrush = FindResource("NotificationErrorBrush") as SolidColorBrush;
            var iconBgBrush = FindResource("SecondaryBackgroundBrush") as SolidColorBrush;

            int leftOffset = LeftOfElement ? 0 : HighlightWidth;
            var textBlockWidth = iconToWrite.Width + TextSpacing * 2 + textToWrite.Width + TextSpacing * 2;

            if (dropTargetEnum == DropTargetEnum.Top || dropTargetEnum == DropTargetEnum.Bottom) {
                if (LeftOfElement) {
                    drawingContext.DrawRectangle(textBgBrush, null, new Rect(new Point(textBlockWidth + HighlightWidth, AdornerHeight / 2 - 2), new Point(textBlockWidth + HighlightWidth + ElementWidth, AdornerHeight / 2 + 2)));
                    drawingContext.DrawGeometry(textBgBrush, null, new PathGeometry() {
                        Figures = new PathFigureCollection {
                        new PathFigure(new Point(textBlockWidth, 0), new List<PathSegment> {
                            new LineSegment(new Point(textBlockWidth+HighlightWidth, AdornerHeight/2), false),
                            new LineSegment(new Point(textBlockWidth, AdornerHeight), false),
                            }, true),
                        },
                        FillRule = FillRule.EvenOdd
                    });
                } else {
                    drawingContext.DrawRectangle(textBgBrush, null, new Rect(new Point(0, AdornerHeight / 2 - 2), new Point(-ElementWidth, AdornerHeight / 2 + 2)));
                    drawingContext.DrawGeometry(textBgBrush, null, new PathGeometry() {
                        Figures = new PathFigureCollection {
                            new PathFigure(new Point(HighlightWidth, 0), new List<PathSegment> {
                                new LineSegment(new Point(0, AdornerHeight/2), false),
                                new LineSegment(new Point(HighlightWidth, AdornerHeight), false),
                            }, true),
                        },
                        FillRule = FillRule.EvenOdd
                    });
                }
            } else {
                if (LeftOfElement) {
                    drawingContext.DrawGeometry(textBgBrush, null, new PathGeometry() {
                        Figures = new PathFigureCollection {
                            new PathFigure(new Point(textBlockWidth, 0), new List<PathSegment> {
                                new LineSegment(new Point(textBlockWidth + HighlightWidth, (-ElementHeight/2) + AdornerHeight / 2), false),
                                new LineSegment(new Point(textBlockWidth + HighlightWidth,  ElementHeight/2 + AdornerHeight / 2), false),
                                new LineSegment(new Point(textBlockWidth, AdornerHeight), false),
                            }, true),
                        },
                        FillRule = FillRule.EvenOdd
                    });
                } else {
                    drawingContext.DrawGeometry(textBgBrush, null, new PathGeometry() {
                        Figures = new PathFigureCollection {
                            new PathFigure(new Point(HighlightWidth, 0), new List<PathSegment> {
                                new LineSegment(new Point(0, (-ElementHeight/2) + AdornerHeight / 2), false),
                                new LineSegment(new Point(0,  ElementHeight/2 + AdornerHeight / 2), false),
                                new LineSegment(new Point(HighlightWidth, AdornerHeight), false),
                            }, true),
                        },
                        FillRule = FillRule.EvenOdd
                    });
                }
            }

            if (LeftOfElement) {
                // draw text rectangle full size
                drawingContext.DrawRectangle(textBgBrush, null, new Rect(new Point(leftOffset, 0), new Point(leftOffset + textBlockWidth, AdornerHeight)));
                drawingContext.DrawText(textToWrite, new Point(leftOffset + TextSpacing, AdornerHeight / 2 - textToWrite.Height / 2));

                // draw arrow rectangle only for icon size right of text
                drawingContext.DrawRectangle(iconBgBrush, null, new Rect(new Point(leftOffset + textToWrite.Width + TextSpacing * 2, AdornerHeight * 0.1), new Point(leftOffset + textBlockWidth, AdornerHeight - AdornerHeight * 0.1)));
                drawingContext.DrawText(iconToWrite, new Point(leftOffset + textToWrite.Width + TextSpacing * 3, AdornerHeight / 2 - iconToWrite.Height / 2));
            } else {
                // draw text rectangle full size
                drawingContext.DrawRectangle(textBgBrush, null, new Rect(new Point(leftOffset, 0), new Point(leftOffset + textBlockWidth, AdornerHeight)));
                drawingContext.DrawText(textToWrite, new Point(leftOffset + iconToWrite.Width + TextSpacing * 3, AdornerHeight / 2 - textToWrite.Height / 2));

                // draw arrow rectangle only for icon size right of text
                drawingContext.DrawRectangle(iconBgBrush, null, new Rect(new Point(leftOffset, AdornerHeight * 0.1), new Point(leftOffset + iconToWrite.Width + TextSpacing * 2, AdornerHeight - AdornerHeight * 0.1)));
                drawingContext.DrawText(iconToWrite, new Point(leftOffset + TextSpacing, AdornerHeight / 2 - iconToWrite.Height / 2));
            }
        }
    }
}