using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Utility.AvalonDock {
    public class PaneStyleSelector : StyleSelector {
        public Style AnchorableStyle {
            get;
            set;
        }

        public Style DocumentStyle {
            get;
            set;
        }

        public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container) {

            if (item is ImageControlVM) {
                return DocumentStyle;      
            } else {
                return AnchorableStyle;
            }


            //return base.SelectStyle(item, container);
        }
    }
}
