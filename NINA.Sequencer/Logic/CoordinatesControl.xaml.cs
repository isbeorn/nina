using NINA.Astrometry;
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

namespace NINA.Sequencer.Logic {
    public partial class CoordinatesControl : UserControl {
        public CoordinatesControl() {
            InitializeComponent();
        }
        public bool Inherited { get; set; }

        public static readonly DependencyProperty InheritedProperty =
             DependencyProperty.Register("Inherited", typeof(bool), typeof(CoordinatesControl), null);
        public InputCoordinates Coords { get; set; }

        public static readonly DependencyProperty CoordsProperty =
             DependencyProperty.Register("Coords", typeof(InputCoordinates), typeof(CoordinatesControl), null);
    }
}
