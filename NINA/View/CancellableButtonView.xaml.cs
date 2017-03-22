using System;
using System.Collections.Generic;
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
    /// Interaction logic for CancellableButtonView.xaml
    /// </summary>
    public partial class CancellableButtonView : UserControl {
        public CancellableButtonView() {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }


        public static readonly DependencyProperty MyButtonTooltipProperty =
            DependencyProperty.Register("MyButtonTooltip", typeof(string), typeof(CancellableButtonView), new UIPropertyMetadata(null));

        public string MyButtonTooltip {
            get {
                return (string)GetValue(MyButtonTooltipProperty);
            }
            set {
                SetValue(MyButtonTooltipProperty, value);
            }
        }

        public static readonly DependencyProperty MyButtonTextProperty =
            DependencyProperty.Register("MyButtonText", typeof(string), typeof(CancellableButtonView), new UIPropertyMetadata(null));

        public string MyButtonText {
            get {
                return (string)GetValue(MyButtonTextProperty);
            }
            set {
                SetValue(MyButtonTextProperty, value);
            }
        }

        public static readonly DependencyProperty MyCommandProperty =
            DependencyProperty.Register("MyCommand", typeof(ICommand), typeof(CancellableButtonView), new UIPropertyMetadata(null));

        public ICommand MyCommand {
            get {
                return (ICommand)GetValue(MyCommandProperty);
            }
            set {
                SetValue(MyCommandProperty, value);
            }
        }

        public static readonly DependencyProperty MyCancelCommandProperty =
            DependencyProperty.Register("MyCancelCommand", typeof(ICommand), typeof(CancellableButtonView), new UIPropertyMetadata(null));

        public ICommand MyCancelCommand {
            get {
                return (ICommand)GetValue(MyCancelCommandProperty);
            }
            set {
                SetValue(MyCancelCommandProperty, value);
            }
        }

        public static readonly DependencyProperty MyButtonImageProperty =
           DependencyProperty.Register("MyButtonImage", typeof(Geometry), typeof(CancellableButtonView), new UIPropertyMetadata(null));

        public Geometry MyButtonImage {
            get {
                return (Geometry)GetValue(MyButtonImageProperty);
            }
            set {
                SetValue(MyButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty MyCancelButtonImageProperty =
           DependencyProperty.Register("MyCancelButtonImage", typeof(Geometry), typeof(CancellableButtonView), new UIPropertyMetadata(null));

        public Geometry MyCancelButtonImage {
            get {
                return (Geometry)GetValue(MyCancelButtonImageProperty);
            }
            set {
                SetValue(MyCancelButtonImageProperty, value);
            }
        }
    }
}
