using NINA.ViewModel;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.AvalonDock.Layout;

namespace NINA.Utility.AvalonDock {

    public class PaneTemplateSelector : DataTemplateSelector {

        public PaneTemplateSelector() {
        }

        public DataTemplate CameraTemplate { get; set; }

        public DataTemplate TelescopeTemplate { get; set; }

        public DataTemplate ImageControlTemplate { get; set; }

        public DataTemplate PlatesolveTemplate { get; set; }

        public DataTemplate PolarAlignmentTemplate { get; set; }

        public DataTemplate GuiderTemplate { get; set; }

        public DataTemplate FilterWheelTemplate { get; set; }

        public DataTemplate ImagingTemplate { get; set; }

        public DataTemplate ImageHistoryTemplate { get; set; }

        public DataTemplate ImageStatisticsTemplate { get; set; }

        public DataTemplate SequenceTemplate { get; set; }

        public DataTemplate WeatherDataTemplate { get; set; }

        public DataTemplate FocuserTemplate { get; set; }

        public DataTemplate AutoFocusTemplate { get; set; }

        public DataTemplate ThumbnailTemplate { get; set; }

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

            if (item is GuiderVM)
                return GuiderTemplate;

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

            if (item is FocuserVM)
                return FocuserTemplate;

            if (item is AutoFocusVM)
                return AutoFocusTemplate;

            if (item is ThumbnailVM)
                return ThumbnailTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}