using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment {
    internal class FocuserTemplateSelector : DataTemplateSelector {
        public DataTemplate Default { get; set; }
        public DataTemplate Zwo { get; set; }
        public DataTemplate FailedToLoadTemplate { get; set; }

        public string Postfix { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is ASIFocuser) {
                return Zwo;
            } else {
                var templateKey = item?.GetType().FullName + Postfix;
                if (item != null && Application.Current.Resources.Contains(templateKey)) {
                    try {
                        return (DataTemplate)Application.Current.Resources[templateKey];
                    } catch (Exception ex) {
                        Logger.Error($"Datatemplate {templateKey} failed to load", ex);
                        return FailedToLoadTemplate;
                    }
                } else {
                    return Default;
                }
            }
        }
    }
}
