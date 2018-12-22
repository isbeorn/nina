using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Locale;
using NINA.ViewModel.Interfaces;
using NINA.Utility.Mediator.Interfaces;

namespace NINA.ViewModel.FlatWizard {

    public interface IFlatWizardVM : ICameraConsumer {
        BinningMode BinningMode { get; set; }
        double CalculatedExposureTime { get; set; }
        double CalculatedHistogramMean { get; set; }
        bool CameraConnected { get; set; }
        RelayCommand CancelFlatExposureSequenceCommand { get; }
        ObservableCollection<FilterInfo> FilterInfos { get; }
        ObservableCollection<FlatWizardFilterSettingsWrapper> Filters { get; set; }
        IAsyncCommand FindExposureTimeCommand { get; }
        int FlatCount { get; set; }
        IFlatWizardExposureTimeFinderService FlatWizardExposureTimeFinderService { get; set; }
        ILoc Locale { get; set; }
        IImagingVM ImagingVM { get; }
        short Gain { get; set; }
        BitmapSource Image { get; set; }
        bool IsPaused { get; set; }
        int Mode { get; set; }
        RelayCommand PauseFlatExposureSequenceCommand { get; }
        RelayCommand ResumeFlatExposureSequenceCommand { get; }
        FilterInfo SelectedFilter { get; set; }
        FlatWizardFilterSettingsWrapper SingleFlatWizardFilterSettings { get; set; }
        IAsyncCommand StartFlatSequenceCommand { get; }
        ApplicationStatus Status { get; set; }
    }
}