using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;

namespace NINA.ViewModel {

    internal class FlatWizardVM : DockableVM, ICameraConsumer, IFilterWheelConsumer {
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IImagingMediator imagingMediator;
        private bool cameraConnected;

        private int flatCount;

        private bool gottaGoFast;

        private double meanTarget;

        private double minExposureTime;

        private FilterInfo selectedFilter;

        private double tolerance;

        private double calculatedExposureTime = 0;

        private int calculatedHistogramMean = 0;

        private CancellationTokenSource findExposureCancelToken;

        private BitmapSource image;

        public IAsyncCommand FindExposureTimeCommand { get; private set; }
        public RelayCommand CancelFindExposureTimeCommand { get; }
        public IAsyncCommand StartFlatSequenceCommand { get; private set; }

        public FlatWizardVM(IProfileService profileService,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            IImagingMediator imagingMediator,
                            IFocuserMediator focuserMediator,
                            IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFlatsWizard";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ContrastSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;
            this.focuserMediator = focuserMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            findExposureCancelToken = new CancellationTokenSource();

            FindExposureTimeCommand = new AsyncCommand<bool>(() => FindExposureTime(new Progress<ApplicationStatus>(p => Status = p), findExposureCancelToken.Token));
            CancelFindExposureTimeCommand = new RelayCommand(CancelFindExposureTime);

            FlatCount = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            HistogramMeanTarget = profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget;
            Tolerance = profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance;
            GottaGoFast = profileService.ActiveProfile.FlatWizardSettings.NoFlatProcessing;
            MinExposureTime = profileService.ActiveProfile.CameraSettings.MinFlatExposureTime;
        }

        public FlatWizardVM(IProfileService profileService,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            IImagingMediator imagingMediator,
                            IFocuserMediator focuserMediator,
                            IApplicationStatusMediator applicationStatusMediator,
                            FilterInfo selectedFilter,
                            int flatCount) : this(profileService, cameraMediator, filterWheelMediator, imagingMediator, focuserMediator, applicationStatusMediator) {
            SelectedFilter = selectedFilter;
            FlatCount = flatCount;
        }

        private void CancelFindExposureTime(object obj) {
            findExposureCancelToken?.Cancel();
        }

        private async Task<bool> FindExposureTime(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return await StartFindingExposureTimeSequence(progress, ct);
        }

        private async Task<bool> StartFindingExposureTimeSequence(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            bool foundHistogramCenter = false;
            do {
                progress.Report(new ApplicationStatus() { Status = "Starting Exposure Time calculation at " + MinExposureTime });
                var oldAutoStretch = imagingMediator.SetAutoStretch(true);
                var oldDetectStars = imagingMediator.SetDetectStars(false);
                Image =
                    await imagingMediator.CaptureAndPrepareImage(new CaptureSequence(MinExposureTime, "FLAT", SelectedFilter, new BinningMode(1, 1), 1), ct, progress);
                imagingMediator.SetAutoStretch(oldAutoStretch);
                imagingMediator.SetDetectStars(oldDetectStars);
                ct.ThrowIfCancellationRequested();
            } while (foundHistogramCenter == false);
            return foundHistogramCenter;
        }

        public bool CameraConnected {
            get {
                return cameraConnected;
            }
            set {
                cameraConnected = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<FilterInfo> Filters => profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;

        public int FlatCount {
            get {
                return flatCount;
            }
            set {
                flatCount = value;
                profileService.ActiveProfile.FlatWizardSettings.FlatCount = flatCount;
                RaisePropertyChanged();
            }
        }

        public bool GottaGoFast {
            get {
                return gottaGoFast;
            }
            set {
                gottaGoFast = value;
                profileService.ActiveProfile.FlatWizardSettings.NoFlatProcessing = gottaGoFast;
                RaisePropertyChanged();
            }
        }

        public double CalculatedExposureTime {
            get {
                return calculatedExposureTime;
            }
            set {
                calculatedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public int CalculatedHistogramMean {
            get {
                return calculatedHistogramMean;
            }
            set {
                calculatedHistogramMean = value;
                RaisePropertyChanged();
            }
        }

        public double HistogramMeanTarget {
            get {
                return meanTarget;
            }
            set {
                meanTarget = value;
                profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget = meanTarget;
                RaisePropertyChanged();
            }
        }

        public double MinExposureTime {
            get {
                return minExposureTime;
            }
            set {
                minExposureTime = value;
                profileService.ActiveProfile.CameraSettings.MinFlatExposureTime = minExposureTime;
                RaisePropertyChanged();
            }
        }

        public double Tolerance {
            get {
                return tolerance;
            }
            set {
                tolerance = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo SelectedFilter {
            get {
                return selectedFilter;
            }
            set {
                selectedFilter = value;
                RaisePropertyChanged();
            }
        }

        public BitmapSource Image {
            get {
                return image;
            }
            set {
                image = value;
                RaisePropertyChanged();
            }
        }

        public ApplicationStatus Status { get; private set; }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            CameraConnected = deviceInfo.Connected;
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            SelectedFilter = deviceInfo.SelectedFilter;
        }
    }
}