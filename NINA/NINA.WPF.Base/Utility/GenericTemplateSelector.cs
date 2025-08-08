using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.WPF.Base.Utility {
    /// <summary>
    /// A generic datatemplate selector that looks for datatemplates following the pattern [FullyQualifiedTypeName]_[Postfix]
    /// The type name is grabbed from the input item out of the bound datacontext
    /// The Postfix should ideally be bound via the DataTemplatePostfix static object via Postfix="{x:Static wpfutil:DataTemplatePostfix.[PropertyName]}"
    /// 
    /// Default Datatemplate is used when no entry is found for the key
    /// FailedToLoadTemplate is used when an entry is found for the key, but the initialization of the datatemplate fails
    /// </summary>
    public class GenericTemplateSelector : DataTemplateSelector {
        public DataTemplate Default { get; set; }
        public DataTemplate FailedToLoadTemplate { get; set; }
        public string Postfix { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {        
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
