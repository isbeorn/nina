using NINA.Model.MyCamera;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace NINA.View {
    /// <summary>
    /// Interaction logic for CameraControlView.xaml
    /// </summary>
    public partial class CameraControlView : UserControl {
        public CameraControlView() {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }

        public static readonly DependencyProperty MyCommandProperty =
            DependencyProperty.Register(nameof(MyCommand), typeof(ICommand), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICommand MyCommand {
            get {
                return (ICommand)GetValue(MyCommandProperty);
            }
            set {
                SetValue(MyCommandProperty, value);
            }
        }

        public static readonly DependencyProperty MyCancelCommandProperty =
           DependencyProperty.Register(nameof(MyCancelCommand), typeof(ICommand), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICommand MyCancelCommand {
            get {
                return (ICommand)GetValue(MyCancelCommandProperty);
            }
            set {
                SetValue(MyCancelCommandProperty, value);
            }
        }

        public static readonly DependencyProperty MyOrientationProperty =
          DependencyProperty.Register(nameof(MyOrientation), typeof(Orientation), typeof(CameraControlView), new UIPropertyMetadata(Orientation.Horizontal));

        public Orientation MyOrientation {
            get {
                return (Orientation)GetValue(MyOrientationProperty);
            }
            set {
                SetValue(MyOrientationProperty, value);
            }
        }

        public static readonly DependencyProperty MyButtonImageProperty =
           DependencyProperty.Register(nameof(MyButtonImage), typeof(Geometry), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Geometry MyButtonImage {
            get {
                return (Geometry)GetValue(MyButtonImageProperty);
            }
            set {
                SetValue(MyButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty MyCancelButtonImageProperty =
           DependencyProperty.Register(nameof(MyCancelButtonImage), typeof(Geometry), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Geometry MyCancelButtonImage {
            get {
                return (Geometry)GetValue(MyCancelButtonImageProperty);
            }
            set {
                SetValue(MyCancelButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty MyButtonTextProperty =
            DependencyProperty.Register(nameof(MyButtonText), typeof(string), typeof(CameraControlView), new UIPropertyMetadata(null));

        public string MyButtonText {
            get {
                return (string)GetValue(MyButtonTextProperty);
            }
            set {
                SetValue(MyButtonTextProperty, value);
            }
        }

        public static readonly DependencyProperty MyExposureDurationProperty =
            DependencyProperty.Register(nameof(MyExposureDuration), typeof(double), typeof(CameraControlView), new UIPropertyMetadata(null));

        public double MyExposureDuration {
            get {
                return (double)GetValue(MyExposureDurationProperty);
            }
            set {
                SetValue(MyExposureDurationProperty, value);
            }
        }

        public static readonly DependencyProperty MyFiltersProperty =
            DependencyProperty.Register(nameof(MyFilters), typeof(ObservableCollection<Model.MyFilterWheel.FilterInfo>), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ObservableCollection<Model.MyFilterWheel.FilterInfo> MyFilters {
            get {
                return (ObservableCollection<Model.MyFilterWheel.FilterInfo>)GetValue(MyFiltersProperty);
            }
            set {
                SetValue(MyFiltersProperty, value);
            }
        }

        public static readonly DependencyProperty MySelectedFilterProperty =
            DependencyProperty.Register(nameof(MySelectedFilter), typeof(Model.MyFilterWheel.FilterInfo), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Model.MyFilterWheel.FilterInfo MySelectedFilter {
            get {
                return (Model.MyFilterWheel.FilterInfo)GetValue(MySelectedFilterProperty);
            }
            set {
                SetValue(MySelectedFilterProperty, value);
            }
        }

        public static readonly DependencyProperty MyBinningModesProperty =
            DependencyProperty.Register(nameof(MyBinningModes), typeof(AsyncObservableCollection<BinningMode>), typeof(CameraControlView), new UIPropertyMetadata(null));

        public AsyncObservableCollection<BinningMode> MyBinningModes {
            get {
                return (AsyncObservableCollection<BinningMode>)GetValue(MyBinningModesProperty);
            }
            set {
                SetValue(MyBinningModesProperty, value);
            }
        }

        public static readonly DependencyProperty MySelectedBinningModeProperty =
            DependencyProperty.Register(nameof(MySelectedBinningMode), typeof(BinningMode), typeof(CameraControlView), new UIPropertyMetadata(null));

        public BinningMode MySelectedBinningMode {
            get {
                return (BinningMode)GetValue(MySelectedBinningModeProperty);
            }
            set {
                SetValue(MySelectedBinningModeProperty, value);
            }
        }

        public static readonly DependencyProperty MyLoopProperty =
           DependencyProperty.Register(nameof(MyLoop), typeof(bool), typeof(CameraControlView), new UIPropertyMetadata(null));

        public bool MyLoop {
            get {
                return (bool)GetValue(MyLoopProperty);
            }
            set {
                SetValue(MyLoopProperty, value);
            }
        }


    }
}
