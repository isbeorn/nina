#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NUnit.Framework;
using System.Threading;
using System.Windows;

namespace NINA.Test.Utility {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DataPipesTest {

        /// <summary>
        /// Verifies DataPipe mirrors Source changes into Target, which is the core purpose of the binding pipe.
        /// </summary>
        [Test]
        public void Source_Set_UpdatesTarget() {
            DataPipe pipe = new DataPipe();
            object source = new object();

            pipe.Source = source;

            Assert.That(pipe.Target, Is.SameAs(source));
        }

        /// <summary>
        /// Verifies the attached DataPipes property stores and retrieves a collection on a dependency object.
        /// </summary>
        [Test]
        public void DataPipesAttachedProperty_SetAndGet_ReturnsSameCollection() {
            DependencyObject dependencyObject = new DependencyObject();
            DataPipeCollection collection = new DataPipeCollection {
                new DataPipe { Source = "Filter" }
            };

            DataPiping.SetDataPipes(dependencyObject, collection);

            Assert.That(DataPiping.GetDataPipes(dependencyObject), Is.SameAs(collection));
        }
    }
}
