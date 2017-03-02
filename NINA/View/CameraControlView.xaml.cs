using NINA.Model.MyCamera;
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
using static NINA.Model.FilterWheelModel;

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
            DependencyProperty.Register("MyCommand", typeof(ICommand), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICommand MyCommand {
            get {
                return (ICommand)GetValue(MyCommandProperty);
            }
            set {
                SetValue(MyCommandProperty, value);
            }
        }

        public static readonly DependencyProperty MyCancelCommandProperty =
           DependencyProperty.Register("MyCancelCommand", typeof(ICommand), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICommand MyCancelCommand {
            get {
                return (ICommand)GetValue(MyCancelCommandProperty);
            }
            set {
                SetValue(MyCancelCommandProperty, value);
            }
        }

        public static readonly DependencyProperty MyButtonImageProperty =
           DependencyProperty.Register("MyButtonImage", typeof(Geometry), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Geometry MyButtonImage {
            get {
                return (Geometry)GetValue(MyButtonImageProperty);
            }
            set {
                SetValue(MyButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty MyCancelButtonImageProperty =
           DependencyProperty.Register("MyCancelButtonImage", typeof(Geometry), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Geometry MyCancelButtonImage {
            get {
                return (Geometry)GetValue(MyCancelButtonImageProperty);
            }
            set {
                SetValue(MyCancelButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty MyButtonTextProperty =
            DependencyProperty.Register("MyButtonText", typeof(string), typeof(CameraControlView), new UIPropertyMetadata(null));

        public string MyButtonText {
            get {
                return (string)GetValue(MyButtonTextProperty);
            }
            set {
                SetValue(MyButtonTextProperty, value);
            }
        }

        public static readonly DependencyProperty MyExposureDurationProperty =
            DependencyProperty.Register("MyExposureDuration", typeof(double), typeof(CameraControlView), new UIPropertyMetadata(null));

        public double MyExposureDuration {
            get {
                return (double)GetValue(MyExposureDurationProperty);
            }
            set {
                SetValue(MyExposureDurationProperty, value);
            }
        }

        public static readonly DependencyProperty MyFiltersProperty =
            DependencyProperty.Register("MyFilters", typeof(ObservableCollection<FilterInfo>), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ObservableCollection<FilterInfo> MyFilters {
            get {
                return (ObservableCollection<FilterInfo>)GetValue(MyFiltersProperty);
            }
            set {
                SetValue(MyFiltersProperty, value);
            }
        }

        public static readonly DependencyProperty MySelectedFilterProperty =
            DependencyProperty.Register("MySelectedFilter", typeof(FilterInfo), typeof(CameraControlView), new UIPropertyMetadata(null));

        public FilterInfo MySelectedFilter {
            get {
                return (FilterInfo)GetValue(MySelectedFilterProperty);
            }
            set {
                SetValue(MySelectedFilterProperty, value);
            }
        }

        public static readonly DependencyProperty MyBinningModesProperty =
            DependencyProperty.Register("MyBinningModes", typeof(ObservableCollection<BinningMode>), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ObservableCollection<BinningMode> MyBinningModes {
            get {
                return (ObservableCollection<BinningMode>)GetValue(MyBinningModesProperty);
            }
            set {
                SetValue(MyBinningModesProperty, value);
            }
        }

        public static readonly DependencyProperty MySelectedBinningModeProperty =
            DependencyProperty.Register("MySelectedBinningMode", typeof(BinningMode), typeof(CameraControlView), new UIPropertyMetadata(null));

        public BinningMode MySelectedBinningMode {
            get {
                return (BinningMode)GetValue(MySelectedBinningModeProperty);
            }
            set {
                SetValue(MySelectedBinningModeProperty, value);
            }
        }

        public static readonly DependencyProperty MyLoopProperty =
           DependencyProperty.Register("MyLoop", typeof(bool), typeof(CameraControlView), new UIPropertyMetadata(null));

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
