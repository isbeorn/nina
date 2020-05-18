#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;

namespace NINA.Utility {
    // data pipes by Dmitry Tashkinov
    // https://stackoverflow.com/a/3667609

    public class DataPiping {

        #region DataPipes (Attached DependencyProperty)

        public static readonly DependencyProperty DataPipesProperty =
            DependencyProperty.RegisterAttached("DataPipes",
                typeof(DataPipeCollection),
                typeof(DataPiping),
                new UIPropertyMetadata(null));

        public static void SetDataPipes(DependencyObject o, DataPipeCollection value) {
            o.SetValue(DataPipesProperty, value);
        }

        public static DataPipeCollection GetDataPipes(DependencyObject o) {
            return (DataPipeCollection)o.GetValue(DataPipesProperty);
        }

        #endregion DataPipes (Attached DependencyProperty)
    }

    public class DataPipeCollection : FreezableCollection<DataPipe> {
    }

    public class DataPipe : Freezable {

        #region Source (DependencyProperty)

        public object Source {
            get { return (object)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(object), typeof(DataPipe),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnSourceChanged)));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((DataPipe)d).OnSourceChanged(e);
        }

        protected virtual void OnSourceChanged(DependencyPropertyChangedEventArgs e) {
            Target = e.NewValue;
        }

        #endregion Source (DependencyProperty)

        #region Target (DependencyProperty)

        public object Target {
            get { return (object)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(object), typeof(DataPipe),
                new FrameworkPropertyMetadata(null));

        #endregion Target (DependencyProperty)

        protected override Freezable CreateInstanceCore() {
            return new DataPipe();
        }
    }
}
