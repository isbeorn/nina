#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    public interface IGuider : INotifyPropertyChanged {
        bool Connected { get; }
        double PixelScale { get; set; }
        string State { get; }
        IGuideStep GuideStep { get; }

        string Name { get; }

        Task<bool> Connect(CancellationToken ct);

        Task<bool> AutoSelectGuideStar();

        bool Disconnect();

        Task<bool> Pause(bool pause, CancellationToken ct);

        Task<bool> StartGuiding(CancellationToken ct);

        Task<bool> StopGuiding(CancellationToken ct);

        Task<bool> Dither(CancellationToken ct);
    }

    public interface IGuideEvent {
        string Event { get; }
        string TimeStamp { get; }
        string Host { get; }
        int Inst { get; }
    }

    public interface IGuiderAppState {
        string State { get; }
    }

    public interface IGuideStep : IGuideEvent {
        double Frame { get; }
        double Time { get; }
        double TimeRA { get; }
        double TimeDec { get; }
        string Mount { get; }
        double Dx { get; }
        double Dy { get; }
        double RADistanceRaw { get; set; }
        double DecDistanceRaw { get; set; }
        double RADistanceGuide { get; set; }
        double DecDistanceGuide { get; set; }
        double RADistanceRawDisplay { get; set; }
        double DecDistanceRawDisplay { get; set; }
        double RADistanceGuideDisplay { get; set; }
        double DecDistanceGuideDisplay { get; set; }
        double RADuration { get; }
        string RADirection { get; }
        double DECDuration { get; }
        string DecDirection { get; }
        double StarMass { get; }
        double SNR { get; }
        double AvgDist { get; }
        bool RALimited { get; }
        bool DecLimited { get; }
        double ErrorCode { get; }

        IGuideStep Clone();
    }
}