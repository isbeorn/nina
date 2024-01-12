#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Container.ExecutionStrategy {

    [TestFixture]
    public class ParallelStrategyTest {

        [Test]
        public async Task Execute_AllItemsAreCalled() {
            var containerMock = new Mock<ISequenceContainer>();
            var item1Mock = new Mock<ISequenceItem>();
            item1Mock.Setup(x => x.Status).Returns(NINA.Core.Enum.SequenceEntityStatus.CREATED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock.Setup(x => x.Status).Returns(NINA.Core.Enum.SequenceEntityStatus.CREATED);
            var item3Mock = new Mock<ISequenceItem>();
            item3Mock.Setup(x => x.Status).Returns(NINA.Core.Enum.SequenceEntityStatus.DISABLED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object, item3Mock.Object };
            containerMock.Setup(x => x.GetItemsSnapshot()).Returns(items);

            var sut = new ParallelStrategy();

            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item3Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Execute_ParallelProgressReport() {
            var containerMock = new Mock<ISequenceContainer>();
            var item1Mock = new Mock<ISequenceItem>();
            item1Mock.SetupGet(x => x.Name).Returns("Item1");
            item1Mock
                .Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Callback<IProgress<ApplicationStatus>, CancellationToken>((progress, ct) => progress.Report(new ApplicationStatus()));
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock.SetupGet(x => x.Name).Returns("Item2");
            item2Mock
                .Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Callback<IProgress<ApplicationStatus>, CancellationToken>((progress, ct) => progress.Report(new ApplicationStatus()));
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            containerMock.Setup(x => x.GetItemsSnapshot()).Returns(items);

            var progressMock = new Mock<IProgress<ApplicationStatus>>();

            var sut = new ParallelStrategy();

            await sut.Execute(containerMock.Object, progressMock.Object, default);
            await Task.Delay(10);

            progressMock.Verify(x => x.Report(It.IsAny<ApplicationStatus>()), Times.Exactly(3));
        }
    }
}