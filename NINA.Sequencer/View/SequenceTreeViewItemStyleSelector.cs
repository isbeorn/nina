using NINA.Sequencer.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Sequencer {
    internal class SequenceTreeViewItemStyleSelector : StyleSelector {
        public Style DefaultStyle { get; set; }
        public Style ContainerStyle { get; set; }
        public Style ItemStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container) {
            if (item is ISequenceRootContainer || item is StartAreaContainer || item is TargetAreaContainer || item is EndAreaContainer) {
                return DefaultStyle;
            }
            if (item is ISequenceContainer && item is not IImmutableContainer) {
                return ContainerStyle;
            } else {
                return ItemStyle;
            }   
        }
    }
}
