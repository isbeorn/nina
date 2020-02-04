#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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