#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CsvHelper.Configuration;
using NINA.Image.ImageData;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.IO;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Utility.AutoFocus;

namespace NINA.WPF.Base.Model {

    public class ImageHistoryPoint : BaseINPC {
        public ImageHistoryPoint(int id, string imageType) {
            Id = id;
            Type = imageType;
        }

        public ImageHistoryPoint(int id, IImageStatistics statistics, string imageType) {
            Id = id;
            Statistics = statistics;
            if (statistics != null) {
                Median = statistics.Median;
                Mean = statistics.Mean;
                StDev = statistics.StDev;
                MAD = statistics.MedianAbsoluteDeviation;
            }
            Type = imageType;
        }

        public void PopulateStatistics(IImageStatistics statistics) {
            Statistics = statistics;
            if (statistics != null) {
                Median = statistics.Median;
                Mean = statistics.Mean;
                StDev = statistics.StDev;
                MAD = statistics.MedianAbsoluteDeviation;
            }
            RaisePropertyChanged(nameof(Median));
            RaisePropertyChanged(nameof(Mean));
            RaisePropertyChanged(nameof(StDev));
            RaisePropertyChanged(nameof(MAD));
            RaisePropertyChanged(nameof(Statistics));
        }

        public void PopulateAFPoint(AutoFocusReport report) {
            AutoFocusPoint = new NINA.Core.Model.AutoFocusPoint();
            AutoFocusPoint.OldPosition = report.InitialFocusPoint.Position;
            AutoFocusPoint.NewPosition = report.CalculatedFocusPoint.Position;
            AutoFocusPoint.Temperature = report.Temperature;
            AutoFocusPoint.Time = report.Timestamp;
            AutoFocusPoint.Filter = report.Filter;
        }

        public void PopulateProperties(ImageSavedEventArgs imageSavedEventArgs) {
            if (imageSavedEventArgs == null)
                return;
            if (imageSavedEventArgs.StarDetectionAnalysis != null) {
                HFR = imageSavedEventArgs.StarDetectionAnalysis.HFR;
                Stars = imageSavedEventArgs.StarDetectionAnalysis.DetectedStars;
            }
            IsBayered = imageSavedEventArgs.IsBayered;
            Filter = imageSavedEventArgs.Filter;
            Duration = imageSavedEventArgs.Duration;
            if (imageSavedEventArgs.PathToImage != null) {
                Filename = Path.GetFileName(imageSavedEventArgs.PathToImage.LocalPath);
                LocalPath = imageSavedEventArgs.PathToImage.LocalPath;
            }

            if (imageSavedEventArgs.MetaData?.Target != null) {
                Target = imageSavedEventArgs.MetaData.Target;
            }
            if (imageSavedEventArgs.MetaData?.Focuser != null) {
                if (!double.IsNaN((double)imageSavedEventArgs.MetaData?.Focuser?.Temperature))
                    Temperature = imageSavedEventArgs.MetaData.Focuser.Temperature;
                if ((bool)(imageSavedEventArgs.MetaData?.Focuser?.Position.HasValue))
                    FocuserPosition = (int)imageSavedEventArgs.MetaData.Focuser.Position;
            } else if (imageSavedEventArgs.MetaData?.WeatherData != null && !double.IsNaN((double)imageSavedEventArgs.MetaData?.WeatherData?.Temperature)) {
                Temperature = imageSavedEventArgs.MetaData.WeatherData.Temperature;
            }

            if (imageSavedEventArgs.MetaData?.Image?.RecordedRMS != null) {
                RecordedRMS = imageSavedEventArgs.MetaData.Image.RecordedRMS;
                Rms = RecordedRMS.Total * RecordedRMS.Scale;
                RmsText = RecordedRMS.TotalText;
            }

            if (imageSavedEventArgs.MetaData?.Rotator != null) {
                if (!double.IsNaN((double)imageSavedEventArgs.MetaData?.Rotator?.Position)) {
                    RotatorPosition = (double)imageSavedEventArgs.MetaData.Rotator.Position;
                }
                if (!double.IsNaN((double)imageSavedEventArgs.MetaData?.Rotator?.MechanicalPosition)) {
                    RotatorMechanicalPosition = (double)imageSavedEventArgs.MetaData.Rotator.MechanicalPosition;
                }
            }
        }

        public int Id { get; private set; }
        public int Index { get; set; }
        public double Zero { get; } = 0.05;

        public IImageStatistics Statistics { get; private set; }

        public NINA.Core.Model.AutoFocusPoint AutoFocusPoint { get; private set; }

        public double HFR { get; private set; }

        public int Stars { get; private set; }

        public double Median { get; private set; }
        public double Mean { get; private set; }
        public double StDev { get; private set; }
        public double MAD { get; private set; }

        public bool IsBayered { get; private set; }
        public string Filter { get; private set; }
        public double Duration { get; private set; }
        public string Filename { get; private set; }
        public string LocalPath { get; private set; }

        public DateTime dateTime { get; private set; } = DateTime.Now;
        public double Temperature { get; private set; }
        public double Rms { get; private set; }
        public string RmsText { get; private set; }
        public RMS RecordedRMS { get; private set; }

        public string Type { get; private set; }

        public TargetParameter Target { get; private set; }

        public int FocuserPosition { get; private set; } = 0;
        public double RotatorPosition { get; private set; } = 0.0;
        public double RotatorMechanicalPosition { get; private set; } = 0.0;
    }

    public sealed class ImageHistoryPointMap : ClassMap<ImageHistoryPoint> {

        public ImageHistoryPointMap() {
            Map(m => m.Id).Index(1);
            Map(m => m.Target.Name).Index(2).Name("Target Name");
            Map(m => m.Target.Coordinates.RA).Index(3).Name("Target Ra");
            Map(m => m.Target.Coordinates.Dec).Index(4).Name("Target Dec");
            Map(m => m.Target.PositionAngle).Optional().Index(5).Name("Target PositionAngle");
            Map(m => m.Filename).Index(6);
            Map(m => m.dateTime).Index(7);
            Map(m => m.Temperature).Optional().Index(8);
            Map(m => m.Duration).Index(9);
            Map(m => m.Filter).Index(10);
            Map(m => m.HFR).Index(11);
            Map(m => m.Stars).Index(12);
            Map(m => m.Median).Index(13);
            Map(m => m.Mean).Index(14);
            Map(m => m.StDev).Index(15);
            Map(m => m.MAD).Index(16);
            Map(m => m.RecordedRMS.TotalText).Optional().Index(17).Name("Rms");
            Map(m => m.RecordedRMS.RA).Optional().Index(18).Name("Rms Ra");
            Map(m => m.RecordedRMS.Dec).Optional().Index(19).Name("Rms Dec");
            Map(m => m.RecordedRMS.Total).Optional().Index(20).Name("Rms Total");
            Map(m => m.RecordedRMS.Scale).Optional().Index(21).Name("Rms Scale");
            Map(m => m.RotatorPosition).Optional().Index(22).Name("Rotator Position");
            Map(m => m.RotatorMechanicalPosition).Optional().Index(23).Name("Rotator MechanicalPosition");
            Map(m => m.FocuserPosition).Optional().Index(24).Name("Focuser Position");
            Map(m => m.AutoFocusPoint.OldPosition).Optional().Index(25).Name("AutoFocus Old Position");
            Map(m => m.AutoFocusPoint.NewPosition).Optional().Index(26).Name("AutoFocus New Position");
            Map(m => m.AutoFocusPoint.Time).Index(27).Optional().Name("AutoFocus Time");
            Map(m => m.AutoFocusPoint.Filter).Optional().Index(28).Name("AutoFocus Filter");
        }
    }
}