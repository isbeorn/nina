using NINA.Model.MyGuider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment.Guider {

    internal class GuiderTemplateSelector : DataTemplateSelector {
        public DataTemplate MGen { get; set; }
        public DataTemplate PHD2 { get; set; }
        public DataTemplate Default { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is MGENGuider) {
                return MGen;
            } else if (item is PHD2Guider) {
                return PHD2;
            } else {
                return Default;
            }
        }
    }
}