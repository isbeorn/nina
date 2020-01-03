#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {

    public interface IGuider : INotifyPropertyChanged {
        bool Connected { get; }
        double PixelScale { get; set; }
        string State { get; }
        string Name { get; }
        string Id { get; }

        event EventHandler<IGuideStep> GuideEvent;

        Task<bool> Connect();

        Task<bool> AutoSelectGuideStar();

        bool Disconnect();

        //Task<bool> Pause(bool pause, CancellationToken ct);

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
        double RADistanceRaw { get; set; }
        double DECDistanceRaw { get; set; }
        double RADistanceRawDisplay { get; set; }
        double DECDistanceRawDisplay { get; set; }
        double RADuration { get; }
        double DECDuration { get; }

        IGuideStep Clone();
    }
}