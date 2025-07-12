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
                var template = FindTemplateForData(item, container as FrameworkElement);
                if (template is HierarchicalDataTemplate || template is null) {
                    return ContainerStyle;
                } else {
                    return ItemStyle;
                }                
            } else {
                return ItemStyle;
            }   
        }
        static DataTemplate FindTemplateForData(object data, FrameworkElement scope) {
            if (data == null) return null;
            if (scope == null) return null;

            var key = new DataTemplateKey(data.GetType());
            return scope.TryFindResource(key) as DataTemplate;
        }
    }
}
