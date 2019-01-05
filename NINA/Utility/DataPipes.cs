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