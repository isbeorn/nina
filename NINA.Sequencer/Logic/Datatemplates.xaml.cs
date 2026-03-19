using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.Sequencer.Logic {

    [Export(typeof(ResourceDictionary))]
    public partial class Datatemplates : ResourceDictionary {

        public Datatemplates() {
            InitializeComponent();
        }
    }
}
