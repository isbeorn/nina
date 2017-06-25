using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using Xceed.Wpf.AvalonDock.Layout;
using NINA.ViewModel;

namespace NINA.Utility.AvalonDock {
    public class PaneTemplateSelector : DataTemplateSelector {
        public PaneTemplateSelector() {

        }


        public DataTemplate CameraTemplate { get; set; }

        public DataTemplate TelescopeTemplate { get; set; }

        public DataTemplate ImageControlTemplate { get; set; }

        public DataTemplate PlatesolveTemplate { get; set; }

        public DataTemplate PolarAlignmentTemplate { get; set; }

        public DataTemplate PHD2Template { get; set; }

        public DataTemplate FilterWheelTemplate { get; set; }

        public DataTemplate ImagingTemplate { get; set; }

        public DataTemplate ImageHistoryTemplate { get; set; }

        public DataTemplate ImageStatisticsTemplate { get; set; }

        public DataTemplate SequenceTemplate { get; set; }

        public DataTemplate WeatherDataTemplate { get; set; }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container) {
            var itemAsLayoutContent = item as LayoutContent;

            if (item is CameraVM)
                return CameraTemplate;

            if (item is TelescopeVM)
                return TelescopeTemplate;

            if (item is PlatesolveVM)
                return PlatesolveTemplate;

            if (item is PolarAlignmentVM)
                return PolarAlignmentTemplate;

            if (item is PHD2VM)
                return PHD2Template;

            if (item is FilterWheelVM)
                return FilterWheelTemplate;

            if (item is ImagingVM)
                return ImagingTemplate;

            if (item is ImageHistoryVM)
                return ImageHistoryTemplate;

            if (item is ImageStatisticsVM)
                return ImageStatisticsTemplate;
            
            if (item is ImageControlVM)
                return ImageControlTemplate;

            if (item is SequenceVM)
                return SequenceTemplate;

            if (item is WeatherDataVM)
                return WeatherDataTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
