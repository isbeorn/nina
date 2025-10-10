#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Logic;
using System;
using System.Windows.Media;

namespace NINA.Sequencer {

    public interface ISequenceEntity : ICloneable, IDroppable {
        string Name { get; set; }

        /// <summary>
        /// A brief description of what the sequence item does
        /// </summary>
        string Description { get; set; }

        GeometryGroup Icon { get; set; }
        string Category { get; set; }

        ISymbolBroker SymbolBroker { get; set; }

        /// <summary>
        /// Indicator that the item is currently active and running
        /// </summary>
        SequenceEntityStatus Status { get; set; }

        /// <summary>
        /// When the sequencer is started this method is called
        /// </summary>
        void Initialize();

        /// <summary>
        /// When the sequencer is stopped or canceled this method is called
        /// </summary>
        void Teardown();

        /// <summary>
        /// When a sequence container is entered this method is called
        /// </summary>
        void SequenceBlockInitialize();

        /// <summary>
        /// When a sequence container is finished this method is called
        /// </summary>
        void SequenceBlockTeardown();

        /// <summary>
        /// Each time the sequence container starts a loop this is called
        /// </summary>
        void SequenceBlockStarted();

        /// <summary>
        /// Each time the sequence container finishes a loop this is called
        /// </summary>
        void SequenceBlockFinished();
    }
}