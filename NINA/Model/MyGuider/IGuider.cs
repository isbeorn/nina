#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
        double RADistanceRaw { get; set; }
        double DECDistanceRaw { get; set; }
        double RADuration { get; }
        double DECDuration { get; }

        IGuideStep Clone();
    }
}
