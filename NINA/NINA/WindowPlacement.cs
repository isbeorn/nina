#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"

using System;
using System.Runtime.InteropServices;

namespace SaveWindowState {
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Point {
        public int X;
        public int Y;

        public Point(int x, int y) {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    // RECT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rect(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement {
        public int length;
        public int flags;
        public int showCmd;
        public Point minPosition;
        public Point maxPosition;
        public Rect normalPosition;
    }
}