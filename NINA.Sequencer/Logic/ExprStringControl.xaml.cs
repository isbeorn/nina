#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.SequenceItem;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Sequencer.Logic {
    public partial class ExprStringControl : UserControl {
        public ExprStringControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ExprStringControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(ExprStringControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ProcessedTextProperty =
            DependencyProperty.Register(nameof(ProcessedText), typeof(string), typeof(ExprStringControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty AcceptsReturnProperty =
            DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool), typeof(ExprStringControl),
                new PropertyMetadata(false));

        public string Text {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Label {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string ProcessedText {
            get => (string)GetValue(ProcessedTextProperty);
            private set => SetValue(ProcessedTextProperty, value);
        }

        public bool AcceptsReturn {
            get => (bool)GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var control = (ExprStringControl)d;
            control.UpdateProcessedText();
        }

        private void UpdateProcessedText() {
            var sequenceItem = DataContext as ISequenceItem;
            if (sequenceItem == null) {
                ProcessedText = Text;
                return;
            }

            ProcessedText = ExpressionExpander.Expand(Text, sequenceItem.SymbolBroker, sequenceItem.Parent);
        }
    }
}