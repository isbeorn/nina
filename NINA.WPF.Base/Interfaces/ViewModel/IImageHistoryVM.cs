#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System.Collections.Generic;
using System.Windows.Input;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.Utility.AutoFocus;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IImageHistoryVM : IDockableVM {
        AsyncObservableCollection<ImageHistoryPoint> AutoFocusPoints { get; set; }
        List<ImageHistoryPoint> ImageHistory { get; }
        AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistory { get; set; }
        ICommand PlotClearCommand { get; }

        int GetNextImageId();

        void Add(int id, IImageStatistics statistics, string imageType);
        void Add(int id, string imageType);
        void PopulateStatistics(int id, IImageStatistics statistics);

        void AppendImageProperties(ImageSavedEventArgs imageSavedEventArgs);

        void AppendAutoFocusPoint(AutoFocusReport report);

        void PlotClear();
    }
}