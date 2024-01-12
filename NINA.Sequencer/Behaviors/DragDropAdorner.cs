#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Sequencer.Behaviors {

    internal class DragDropAdorner : Adorner {
        private readonly RenderTargetBitmap thisImg;
        public DragDropBehavior DragDropBehavior;

        public DragDropAdorner(DragDropBehavior dragDropBehavior, UIElement adornedElement, RenderTargetBitmap img) : base(adornedElement) {
            thisImg = img;
            DragDropBehavior = dragDropBehavior;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            drawingContext.DrawImage(thisImg, new Rect(0, 0, thisImg.Width, thisImg.Height));
        }

        protected override Size MeasureOverride(Size constraint) {
            var result = base.MeasureOverride(constraint);
            InvalidateVisual();
            return result;
        }
    }
}