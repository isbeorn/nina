#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Container {

    [TestFixture]
    public class SequenceContainerTest {
        private Mock<IExecutionStrategy> dummyStrategyMock = new Mock<IExecutionStrategy>();
        private SequenceContainer Sut;

        [SetUp]
        public void Setup() {
            var mock = new Mock<SequenceContainer>(dummyStrategyMock.Object);
            mock.CallBase = true;
            Sut = mock.Object;
        }

        //[Test]
        //public void SequenceContainerTest_Clone_GoodClone() {
        //    var itemMock = new Mock<ISequenceItem>();
        //    itemMock.Setup(x => x.Clone()).Returns(new Mock<ISequenceItem>().Object);
        //    var item2Mock = new Mock<ISequenceItem>();
        //    item2Mock.Setup(x => x.Clone()).Returns(new Mock<ISequenceItem>().Object);
        //    var conditionMock = new Mock<ISequenceCondition>();
        //    conditionMock.Setup(x => x.Clone()).Returns(new Mock<ISequenceCondition>().Object);
        //    var condition2Mock = new Mock<ISequenceCondition>();
        //    condition2Mock.Setup(x => x.Clone()).Returns(new Mock<ISequenceCondition>().Object);
        //    var triggerMock = new Mock<ISequenceTrigger>();
        //    triggerMock.Setup(x => x.Clone()).Returns(new Mock<ISequenceTrigger>().Object);
        //    var trigger2Mock = new Mock<ISequenceTrigger>();
        //    trigger2Mock.Setup(x => x.Clone()).Returns(new Mock<ISequenceTrigger>().Object);

        //    var items = new List<ISequenceItem>() { itemMock.Object, item2Mock.Object };
        //    var conditions = new List<ISequenceCondition>() { conditionMock.Object, condition2Mock.Object };
        //    var trigger = new List<ISequenceTrigger>() { triggerMock.Object, trigger2Mock.Object };

        //    foreach (var item in items) {
        //        Sut.Add(item);
        //    }
        //    foreach (var item in conditions) {
        //        Sut.Add(item);
        //    }
        //    foreach (var item in trigger) {
        //        Sut.Add(item);
        //    }

        //    var item2 = (SequenceContainer)Sut.Clone();

        //    item2.Should().NotBeSameAs(Sut);
        //    item2.Icon.Should().BeSameAs(Sut.Icon);
        //    item2.Iterations.Should().Be(Sut.Iterations);
        //    item2.Items.Should().NotBeSameAs(Sut.Items);
        //    item2.Items.Should().BeEquivalentTo(Sut.Items);
        //    item2.Conditions.Should().NotBeSameAs(Sut.Conditions);
        //    item2.Conditions.Should().BeEquivalentTo(Sut.Conditions);
        //    item2.Triggers.Should().NotBeSameAs(Sut.Triggers);
        //    item2.Triggers.Should().BeEquivalentTo(Sut.Triggers);
        //}

        [Test]
        public void Add_Item() {
            var item = new Mock<ISequenceItem>().Object;

            Sut.Add(item);

            Sut.Items.Should().HaveCount(1);
        }

        [Test]
        public void Remove_ExistingItem() {
            var item = new Mock<ISequenceItem>().Object;

            Sut.Add(item);
            var removed = Sut.Remove(item);

            Sut.Items.Should().HaveCount(0);
            removed.Should().BeTrue();
        }

        [Test]
        public void Add_Condition() {
            var item = new Mock<ISequenceCondition>().Object;

            Sut.Add(item);

            Sut.Conditions.Should().HaveCount(1);
        }

        [Test]
        public void Remove_ExistingCondition_AtLeastOneMustRemain() {
            var item = new Mock<ISequenceCondition>().Object;

            Sut.Add(item);
            var removed = Sut.Remove(item);

            Sut.Conditions.Should().HaveCount(0);
            removed.Should().BeTrue();
        }

        [Test]
        public void Remove_ExistingCondition() {
            var item = new Mock<ISequenceCondition>().Object;
            var item2 = new Mock<ISequenceCondition>().Object;

            Sut.Add(item);
            Sut.Add(item2);
            var removed = Sut.Remove(item2);

            Sut.Conditions.Should().HaveCount(1);
            removed.Should().BeTrue();
        }

        [Test]
        public void Remove_NonExistingItem() {
            var item = new Mock<ISequenceItem>().Object;

            var removed = Sut.Remove(item);

            removed.Should().BeFalse();
        }

        [Test]
        public void Remove_ExistingTrigger() {
            var item = new Mock<ISequenceTrigger>().Object;
            var item2 = new Mock<ISequenceTrigger>().Object;

            Sut.Add(item);
            Sut.Add(item2);
            var removed = Sut.Remove(item2);

            Sut.Triggers.Should().HaveCount(1);
            removed.Should().BeTrue();
        }

        [Test]
        public void Remove_NonExistingTrigger() {
            var item = new Mock<ISequenceTrigger>().Object;

            var removed = Sut.Remove(item);

            removed.Should().BeFalse();
        }

        [Test]
        public void AfterParentChanged() {
            var itemMock = new Mock<ISequenceItem>();
            var validatableItemMock = itemMock.As<IValidatable>();
            var item2Mock = new Mock<ISequenceItem>();
            var validatableItem2Mock = itemMock.As<IValidatable>();

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            Sut.AfterParentChanged();

            itemMock.Verify(x => x.AfterParentChanged(), Times.Once);
            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            item2Mock.Verify(x => x.AfterParentChanged(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
        }

        [Test]
        public void Validate_AllItemsValid() {
            var itemMock = new Mock<ISequenceItem>();
            var validatableItemMock = itemMock.As<IValidatable>();
            validatableItemMock.Setup(x => x.Validate()).Returns(true);
            var item2Mock = new Mock<ISequenceItem>();
            var validatableItem2Mock = itemMock.As<IValidatable>();
            validatableItem2Mock.Setup(x => x.Validate()).Returns(true);

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            var valid = Sut.Validate();

            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
            valid.Should().BeTrue();
        }

        [Test]
        public void Validate_OneItemInvalid() {
            var itemMock = new Mock<ISequenceItem>();
            var validatableItemMock = itemMock.As<IValidatable>();
            validatableItemMock.Setup(x => x.Validate()).Returns(true);
            var item2Mock = new Mock<ISequenceItem>();
            var validatableItem2Mock = itemMock.As<IValidatable>();
            validatableItem2Mock.Setup(x => x.Validate()).Returns(false);

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            var valid = Sut.Validate();

            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
            valid.Should().BeFalse();
        }

        [Test]
        public void AfterParentChanged_ConditionValidation() {
            var itemMock = new Mock<ISequenceCondition>();
            var validatableItemMock = itemMock.As<IValidatable>();
            var item2Mock = new Mock<ISequenceCondition>();
            var validatableItem2Mock = itemMock.As<IValidatable>();

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            Sut.AfterParentChanged();

            itemMock.Verify(x => x.AfterParentChanged(), Times.Once);
            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            item2Mock.Verify(x => x.AfterParentChanged(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
        }

        [Test]
        public void Validate_AllConditionsValid() {
            var itemMock = new Mock<ISequenceCondition>();
            var validatableItemMock = itemMock.As<IValidatable>();
            validatableItemMock.Setup(x => x.Validate()).Returns(true);
            var item2Mock = new Mock<ISequenceCondition>();
            var validatableItem2Mock = itemMock.As<IValidatable>();
            validatableItem2Mock.Setup(x => x.Validate()).Returns(true);

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            var valid = Sut.Validate();

            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
            valid.Should().BeTrue();
        }

        [Test]
        public void Validate_OneConditionInvalid() {
            var itemMock = new Mock<ISequenceCondition>();
            var validatableItemMock = itemMock.As<IValidatable>();
            validatableItemMock.Setup(x => x.Validate()).Returns(true);
            var item2Mock = new Mock<ISequenceCondition>();
            var validatableItem2Mock = itemMock.As<IValidatable>();
            validatableItem2Mock.Setup(x => x.Validate()).Returns(false);

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            var valid = Sut.Validate();

            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
            valid.Should().BeFalse();
        }

        [Test]
        public void AfterParentChanged_TriggerValidation() {
            var itemMock = new Mock<ISequenceTrigger>();
            var validatableItemMock = itemMock.As<IValidatable>();
            var item2Mock = new Mock<ISequenceTrigger>();
            var validatableItem2Mock = itemMock.As<IValidatable>();

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            Sut.AfterParentChanged();

            itemMock.Verify(x => x.AfterParentChanged(), Times.Once);
            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            item2Mock.Verify(x => x.AfterParentChanged(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
        }

        [Test]
        public void Validate_AllTriggersValid() {
            var itemMock = new Mock<ISequenceTrigger>();
            var validatableItemMock = itemMock.As<IValidatable>();
            validatableItemMock.Setup(x => x.Validate()).Returns(true);
            var item2Mock = new Mock<ISequenceTrigger>();
            var validatableItem2Mock = itemMock.As<IValidatable>();
            validatableItem2Mock.Setup(x => x.Validate()).Returns(true);

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            var valid = Sut.Validate();

            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
            valid.Should().BeTrue();
        }

        [Test]
        public void Validate_OneTriggerInvalid() {
            var itemMock = new Mock<ISequenceTrigger>();
            var validatableItemMock = itemMock.As<IValidatable>();
            validatableItemMock.Setup(x => x.Validate()).Returns(true);
            var item2Mock = new Mock<ISequenceTrigger>();
            var validatableItem2Mock = itemMock.As<IValidatable>();
            validatableItem2Mock.Setup(x => x.Validate()).Returns(false);

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            var valid = Sut.Validate();

            validatableItemMock.Verify(x => x.Validate(), Times.Once);
            validatableItem2Mock.Verify(x => x.Validate(), Times.Once);
            valid.Should().BeFalse();
        }

        [Test]
        public async Task ResetProgress() {
            var itemMock = new Mock<ISequenceItem>();
            var item2Mock = new Mock<ISequenceItem>();

            Sut.Add(itemMock.Object);
            Sut.Add(item2Mock.Object);

            await Sut.Run(default, default);

            Sut.ResetProgress();

            itemMock.Verify(x => x.ResetProgress(), Times.Once);
            item2Mock.Verify(x => x.ResetProgress(), Times.Once);
            Sut.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public void GetRootContainer_DoesNotExist() {
            var root = Sut.GetRootContainer(Sut);

            root.Should().BeNull();
        }

        [Test]
        public void GetRootContainer_Exists() {
            var rootContainer = new Mock<ISequenceRootContainer>().Object;

            Sut.AttachNewParent(rootContainer);

            var root = Sut.GetRootContainer(Sut);

            root.Should().Be(rootContainer);
        }

        [Test]
        public void GetRootContainer_DeepHierarchy_Exists() {
            var rootContainer = new Mock<ISequenceRootContainer>().Object;
            var intermediateContainerMock = new Mock<ISequenceContainer>();
            intermediateContainerMock.SetupGet(x => x.Parent).Returns(rootContainer);

            Sut.AttachNewParent(intermediateContainerMock.Object);

            var root = Sut.GetRootContainer(Sut);

            root.Should().Be(rootContainer);
        }

        [Test]
        public void CheckConditions_NoConditionsAvailable_False() {
            var check = Sut.CheckConditions(null, null);

            check.Should().BeFalse();
        }

        [Test]
        public void CheckConditions_OneTruthyConditionAvailable_True() {
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock.Setup(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);
            Sut.Add(conditionMock.Object);

            var check = Sut.CheckConditions(null, null);

            check.Should().BeTrue();
        }

        [Test]
        public void CheckConditions_OneFalsyConditionAvailable_True() {
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock.Setup(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(false);
            Sut.Add(conditionMock.Object);

            var check = Sut.CheckConditions(null, null);

            check.Should().BeFalse();
        }

        [Test]
        public void CheckConditions_AtLeastOneFalsyConditionAvailable_True() {
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock.Setup(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(false);
            var condition2Mock = new Mock<ISequenceCondition>();
            condition2Mock.Setup(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);
            Sut.Add(conditionMock.Object);
            Sut.Add(condition2Mock.Object);

            var check = Sut.CheckConditions(null, null);

            check.Should().BeFalse();
        }

        [Test]
        public void CheckConditions_AllTruthyConditionAvailable_True() {
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock.Setup(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);
            var condition2Mock = new Mock<ISequenceCondition>();
            condition2Mock.Setup(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);
            Sut.Add(conditionMock.Object);
            Sut.Add(condition2Mock.Object);

            var check = Sut.CheckConditions(null, null);

            check.Should().BeTrue();
        }

        [Test]
        public void MoveUp_FirstItem_NoParent_NothingChanged() {
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);

            Sut.MoveUp(item1);

            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item2, item3, item4 });
        }

        [Test]
        public void MoveUp_FirstItem_HasParent_FirstItemRemoved() {
            var parentMock = new Mock<ISequenceContainer>();
            parentMock.Setup(x => x.Items.IndexOf(It.IsAny<ISequenceItem>())).Returns(-1);
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.AttachNewParent(parentMock.Object);
            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);

            Sut.MoveUp(item1);

            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item2, item3, item4 });
        }

        [Test]
        public void MoveUp_InBetween_OrderChanged() {
            var parentMock = new Mock<ISequenceContainer>();
            parentMock.Setup(x => x.Items.IndexOf(It.IsAny<ISequenceItem>())).Returns(-1);
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.AttachNewParent(parentMock.Object);
            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);

            Sut.MoveUp(item3);

            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item3, item2, item4 });
        }

        [Test]
        public void MoveUp_InBetweenToFirst_OrderChanged() {
            var parentMock = new Mock<ISequenceContainer>();
            parentMock.Setup(x => x.Items.IndexOf(It.IsAny<ISequenceItem>())).Returns(-1);
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.AttachNewParent(parentMock.Object);
            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);

            Sut.MoveUp(item2);

            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item2, item1, item3, item4 });
        }

        [Test]
        public void MoveDown_LastItem_NoParent_NothingChanged() {
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);

            Sut.MoveDown(item4);

            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item2, item3, item4 });
        }

        [Test]
        public void MoveDown_LastItem_HasParent_LastItemRemoved() {
            var parentMock = new Mock<ISequenceContainer>();
            parentMock.Setup(x => x.Items.IndexOf(It.IsAny<ISequenceItem>())).Returns(-1);
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.AttachNewParent(parentMock.Object);
            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);

            Sut.MoveDown(item4);

            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item2, item3 });
        }

        [Test]
        public void MoveDown_InBetween_OrderChanged() {
            var parentMock = new Mock<ISequenceContainer>();
            parentMock.Setup(x => x.Items.IndexOf(It.IsAny<ISequenceItem>())).Returns(-1);
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.AttachNewParent(parentMock.Object);
            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);

            Sut.MoveDown(item2);

            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item3, item2, item4 });
        }

        [Test]
        public void MoveDown_InBetweenToLast_OrderChanged() {
            var parentMock = new Mock<ISequenceContainer>();
            parentMock.Setup(x => x.Items.IndexOf(It.IsAny<ISequenceItem>())).Returns(-1);
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.AttachNewParent(parentMock.Object);
            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);

            Sut.MoveDown(item3);

            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item2, item4, item3 });
        }

        /* Todo */

        [Test]
        public void DropInCommand() {
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(-1, 0)]
        public void InsertIntoSequenceBlocks_SequenceItem_EmptyList(int index, int count) {
            var item1 = new Mock<ISequenceItem>().Object;

            Sut.InsertIntoSequenceBlocks(index, item1);

            Sut.Items.Should().HaveCount(count);
        }

        [Test]
        public void InsertIntoSequenceBlocks_SequenceItem_ContainsItems_AddInBetween() {
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item4);
            Sut.InsertIntoSequenceBlocks(1, item3);

            Sut.Items.Should().HaveCount(4);
            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item3, item2, item4 });
        }

        [Test]
        [TestCase(0, 1, 0)]
        [TestCase(1, 0, 0)]
        [TestCase(-1, 0, 0)]
        [TestCase(0, -1, 0)]
        public void MoveWithinIntoSequenceBlocks_SequenceItem_EmptyList(int oldIdx, int newIdx, int count) {
            Sut.MoveWithinIntoSequenceBlocks(oldIdx, newIdx);

            Sut.Items.Should().HaveCount(count);
        }

        [Test]
        [TestCase(0, 1, 1)]
        [TestCase(1, 0, 1)]
        [TestCase(-1, 0, 1)]
        [TestCase(0, -1, 1)]
        public void MoveWithinIntoSequenceBlocks_SequenceItem_SingleItem(int oldIdx, int newIdx, int count) {
            var item1 = new Mock<ISequenceItem>().Object;
            Sut.Add(item1);

            Sut.MoveWithinIntoSequenceBlocks(oldIdx, newIdx);

            Sut.Items.Should().HaveCount(count);
        }

        [Test]
        public void MoveWithinIntoSequenceBlocks_SequenceItem_ContainsTwoItems_MoveToStart() {
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;

            Sut.Add(item1);
            Sut.Add(item2);
            Sut.MoveWithinIntoSequenceBlocks(1, 0);

            Sut.Items.Should().HaveCount(2);
            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item2, item1 });
        }

        [Test]
        public void MoveWithinIntoSequenceBlocks_SequenceItem_ContainsTwoItems_MoveToEnd() {
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;

            Sut.Add(item1);
            Sut.Add(item2);
            Sut.MoveWithinIntoSequenceBlocks(0, 1);

            Sut.Items.Should().HaveCount(2);
            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item2, item1 });
        }

        [Test]
        public void MoveWithinIntoSequenceBlocks_SequenceItem_ContainsItems_MoveUpInBetween() {
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);
            Sut.MoveWithinIntoSequenceBlocks(2, 1);

            Sut.Items.Should().HaveCount(4);
            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item3, item2, item4 });
        }

        [Test]
        public void MoveWithinIntoSequenceBlocks_SequenceItem_ContainsItems_MoveDownInBetween() {
            var item1 = new Mock<ISequenceItem>().Object;
            var item2 = new Mock<ISequenceItem>().Object;
            var item3 = new Mock<ISequenceItem>().Object;
            var item4 = new Mock<ISequenceItem>().Object;

            Sut.Add(item1);
            Sut.Add(item2);
            Sut.Add(item3);
            Sut.Add(item4);
            Sut.MoveWithinIntoSequenceBlocks(1, 2);

            Sut.Items.Should().HaveCount(4);
            Sut.Items.Should().ContainInOrder(new List<ISequenceItem>() { item1, item3, item2, item4 });
        }

        [Test]
        public void DropIntoTriggersCommand_TriggerWithoutParent_Clone_EmptyList_AddedToContainer() {
            var sourceMock = new Mock<ISequenceTrigger>();
            var cloneMock = new Mock<ISequenceTrigger>();
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);

            var param = new DropIntoParameters(sourceMock.Object);
            Sut.DropIntoTriggersCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Once);
            sourceMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            Sut.Triggers.Should().Contain(cloneMock.Object);
        }

        [Test]
        public void DropIntoTriggersCommand_TriggerWithParent_NoClone_EmptyList_AddedToContainer() {
            var parentMock = new Mock<ISequenceContainer>();
            var sourceMock = new Mock<ISequenceTrigger>();
            var cloneMock = new Mock<ISequenceTrigger>();
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            sourceMock.SetupGet(p => p.Parent).Returns(parentMock.Object);

            var param = new DropIntoParameters(sourceMock.Object);
            Sut.DropIntoTriggersCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            sourceMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            parentMock.Verify(x => x.Remove(It.Is<ISequenceTrigger>(c => c == sourceMock.Object)), Times.Once);
            Sut.Triggers.Should().Contain(sourceMock.Object);
        }

        [Test]
        public void DropIntoTriggersCommand_TriggerWithoutParent_Clone_FilledList_AddedToContainer() {
            var sourceMock = new Mock<ISequenceTrigger>();
            var cloneMock = new Mock<ISequenceTrigger>();
            cloneMock.SetupGet(n => n.Name).Returns("RandoNamo");
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            Sut.Triggers.Add(new Mock<ISequenceTrigger>().Object);
            Sut.Triggers.Add(new Mock<ISequenceTrigger>().Object);

            var param = new DropIntoParameters(sourceMock.Object);
            Sut.DropIntoTriggersCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Once);
            sourceMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            Sut.Triggers.Should().Contain(cloneMock.Object);
        }

        [Test]
        public void DropIntoTriggersCommand_TriggerWithoutParent_Clone_FilledList_SameNameAlreadyExists_NotAdded() {
            var sourceMock = new Mock<ISequenceTrigger>();
            var cloneMock = new Mock<ISequenceTrigger>();
            cloneMock.SetupGet(n => n.Name).Returns("RandoNamo");
            var existingMock = new Mock<ISequenceTrigger>();
            existingMock.SetupGet(n => n.Name).Returns("RandoNamo");
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            Sut.Triggers.Add(existingMock.Object);

            var param = new DropIntoParameters(sourceMock.Object);
            Sut.DropIntoTriggersCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Once);
            sourceMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Never);
            Sut.Triggers.Should().NotContain(cloneMock.Object);
        }

        [Test]
        public void DropIntoConditionsCommand_ConditionWithoutParent_Clone_EmptyList_AddedToContainer() {
            var sourceMock = new Mock<ISequenceCondition>();
            var cloneMock = new Mock<ISequenceCondition>();
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);

            var param = new DropIntoParameters(sourceMock.Object);
            Sut.DropIntoConditionsCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Once);
            sourceMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            Sut.Conditions.Should().Contain(cloneMock.Object);
        }

        [Test]
        public void DropIntoConditionsCommand_ConditionWithParent_NoClone_EmptyList_AddedToContainer() {
            var parentMock = new Mock<ISequenceContainer>();
            var sourceMock = new Mock<ISequenceCondition>();
            var cloneMock = new Mock<ISequenceCondition>();
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            sourceMock.SetupGet(p => p.Parent).Returns(parentMock.Object);

            var param = new DropIntoParameters(sourceMock.Object);
            Sut.DropIntoConditionsCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            sourceMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            parentMock.Verify(x => x.Remove(It.Is<ISequenceCondition>(c => c == sourceMock.Object)), Times.Once);
            Sut.Conditions.Should().Contain(sourceMock.Object);
        }

        [Test]
        public void DropIntoConditionsCommand_ConditionWithoutParent_Clone_FilledList_AddedToContainer() {
            var sourceMock = new Mock<ISequenceCondition>();
            var cloneMock = new Mock<ISequenceCondition>();
            cloneMock.SetupGet(n => n.Name).Returns("RandoNamo");
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            Sut.Conditions.Add(new Mock<ISequenceCondition>().Object);
            Sut.Conditions.Add(new Mock<ISequenceCondition>().Object);

            var param = new DropIntoParameters(sourceMock.Object);
            Sut.DropIntoConditionsCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Once);
            sourceMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            Sut.Conditions.Should().Contain(cloneMock.Object);
        }

        [Test]
        public void DropIntoConditionsCommand_ConditionWithoutParent_Clone_FilledList_SameNameAlreadyExists_NotAdded() {
            var sourceMock = new Mock<ISequenceCondition>();
            var cloneMock = new Mock<ISequenceCondition>();
            cloneMock.SetupGet(n => n.Name).Returns("RandoNamo");
            var existingMock = new Mock<ISequenceCondition>();
            existingMock.SetupGet(n => n.Name).Returns("RandoNamo");
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            Sut.Conditions.Add(existingMock.Object);

            var param = new DropIntoParameters(sourceMock.Object);
            Sut.DropIntoConditionsCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Once);
            sourceMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Never);
            Sut.Conditions.Should().NotContain(cloneMock.Object);
        }

        [Test]
        public void DropIntoItemsCommand_ItemWithoutParent_Clone_EmptyList_AddedToContainer() {
            var sourceMock = new Mock<ISequenceItem>();
            var cloneMock = new Mock<ISequenceItem>();
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);

            var param = new DropIntoParameters(sourceMock.Object, null, NINA.Core.Enum.DropTargetEnum.Center);
            Sut.DropIntoCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Once);
            sourceMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            Sut.Items.Should().Contain(cloneMock.Object);
        }

        [Test]
        public void DropIntoItemsCommand_ItemWithParent_NoClone_EmptyList_AddedToContainer() {
            var parentMock = new Mock<ISequenceContainer>();
            var sourceMock = new Mock<ISequenceItem>();
            var cloneMock = new Mock<ISequenceItem>();
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            sourceMock.SetupGet(p => p.Parent).Returns(parentMock.Object);

            var param = new DropIntoParameters(sourceMock.Object, null, NINA.Core.Enum.DropTargetEnum.Center);
            Sut.DropIntoCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            sourceMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            parentMock.Verify(x => x.Remove(It.Is<ISequenceItem>(c => c == sourceMock.Object)), Times.Once);
            Sut.Items.Should().Contain(sourceMock.Object);
        }

        [Test]
        public void DropIntoItemsCommand_ItemWithoutParent_Clone_FilledList_AddedToContainer() {
            var sourceMock = new Mock<ISequenceItem>();
            var cloneMock = new Mock<ISequenceItem>();
            cloneMock.SetupGet(n => n.Name).Returns("RandoNamo");
            sourceMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            Sut.Items.Add(new Mock<ISequenceItem>().Object);
            Sut.Items.Add(new Mock<ISequenceItem>().Object);

            var param = new DropIntoParameters(sourceMock.Object, null, NINA.Core.Enum.DropTargetEnum.Center);
            Sut.DropIntoCommand.Execute(param);

            sourceMock.Verify(x => x.Clone(), Times.Once);
            sourceMock.Verify(x => x.AttachNewParent(It.IsAny<ISequenceContainer>()), Times.Never);
            cloneMock.Verify(x => x.AttachNewParent(It.Is<ISequenceContainer>(c => c == Sut)), Times.Once);
            Sut.Items.Should().Contain(cloneMock.Object);
        }

        [Test]
        public void ResetAll_AllItemsGetReset() {
            var itemMock = new Mock<ISequenceItem>();
            var containerMock = new Mock<ISequenceContainer>();
            var conditionMock = new Mock<ISequenceCondition>();

            Sut.Items.Add(itemMock.Object);
            Sut.Items.Add(containerMock.Object);
            Sut.Conditions.Add(conditionMock.Object);

            Sut.ResetProgressCommand.Execute(null);

            itemMock.Verify(x => x.ResetProgress(), Times.Exactly(2));
            containerMock.Verify(x => x.ResetAll(), Times.Once);
            conditionMock.Verify(x => x.ResetProgress(), Times.Once);
        }

        [Test]
        public async Task Triggerable_RunTriggers_Called() {
            var beforeTrigger = new Mock<ISequenceTrigger>();
            var afterTrigger = new Mock<ISequenceTrigger>();
            var mixedTrigger = new Mock<ISequenceTrigger>();

            beforeTrigger.Setup(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);
            beforeTrigger.Setup(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(false);

            afterTrigger.Setup(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(false);
            afterTrigger.Setup(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            mixedTrigger.Setup(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);
            mixedTrigger.Setup(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            Sut.Add(beforeTrigger.Object);
            Sut.Add(afterTrigger.Object);
            Sut.Add(mixedTrigger.Object);

            await Sut.RunTriggers(default, default, default, default);

            beforeTrigger.Verify(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Once);
            beforeTrigger.Verify(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Never);
            beforeTrigger.Verify(x => x.Run(It.IsAny<ISequenceContainer>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            afterTrigger.Verify(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Once);
            afterTrigger.Verify(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Never);
            afterTrigger.Verify(x => x.Run(It.IsAny<ISequenceContainer>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);

            mixedTrigger.Verify(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Once);
            mixedTrigger.Verify(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Never);
            mixedTrigger.Verify(x => x.Run(It.IsAny<ISequenceContainer>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Triggerable_RunTriggersAfter_Called() {
            var beforeTrigger = new Mock<ISequenceTrigger>();
            var afterTrigger = new Mock<ISequenceTrigger>();
            var mixedTrigger = new Mock<ISequenceTrigger>();

            beforeTrigger.Setup(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);
            beforeTrigger.Setup(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(false);

            afterTrigger.Setup(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(false);
            afterTrigger.Setup(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            mixedTrigger.Setup(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);
            mixedTrigger.Setup(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            Sut.Add(beforeTrigger.Object);
            Sut.Add(afterTrigger.Object);
            Sut.Add(mixedTrigger.Object);

            await Sut.RunTriggersAfter(default, default, default, default);

            beforeTrigger.Verify(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Never);
            beforeTrigger.Verify(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Once);
            beforeTrigger.Verify(x => x.Run(It.IsAny<ISequenceContainer>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);

            afterTrigger.Verify(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Never);
            afterTrigger.Verify(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Once);
            afterTrigger.Verify(x => x.Run(It.IsAny<ISequenceContainer>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            mixedTrigger.Verify(x => x.ShouldTrigger(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Never);
            mixedTrigger.Verify(x => x.ShouldTriggerAfter(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()), Times.Once);
            mixedTrigger.Verify(x => x.Run(It.IsAny<ISequenceContainer>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(false, true, true, true, true)]
        [TestCase(false, true, true, false, true)]
        [TestCase(false, true, false, true, true)]
        [TestCase(false, false, true, true, true)]
        [TestCase(false, true, false, false, true)]
        [TestCase(false, false, true, false, true)]
        [TestCase(false, false, false, true, true)]
        [TestCase(false, false, false, false, false)]
        [TestCase(true, true, true, true, true)]
        [TestCase(true, true, true, false, true)]
        [TestCase(true, true, false, true, true)]
        [TestCase(true, false, true, true, true)]
        [TestCase(true, true, false, false, true)]
        [TestCase(true, false, true, false, true)]
        [TestCase(true, false, false, true, true)]
        [TestCase(true, false, false, false, true)]
        public void HasChanged(bool baseChanged, bool itemchanged, bool triggerchanged, bool conditionchanged, bool expected) {
            var item = new Mock<ISequenceItem>();
            item.SetupGet(x => x.HasChanged).Returns(itemchanged);
            var trigger = new Mock<ISequenceTrigger>();
            trigger.SetupGet(x => x.HasChanged).Returns(triggerchanged);
            var condition = new Mock<ISequenceCondition>();
            condition.SetupGet(x => x.HasChanged).Returns(conditionchanged);

            Sut.HasChanged = baseChanged;
            Sut.Add(item.Object);
            Sut.Add(trigger.Object);
            Sut.Add(condition.Object);

            Sut.HasChanged.Should().Be(expected);
        }

        [Test]
        public void ClearHasChanged() {
            var item = new Mock<ISequenceItem>();
            var trigger = new Mock<ISequenceTrigger>();
            var condition = new Mock<ISequenceCondition>();

            Sut.Add(item.Object);
            Sut.Add(trigger.Object);
            Sut.Add(condition.Object);
            Sut.HasChanged = true;

            //Act
            Sut.ClearHasChanged();

            Sut.HasChanged.Should().BeFalse();
            item.Verify(x => x.ClearHasChanged(), Times.Once);
            trigger.Verify(x => x.ClearHasChanged(), Times.Once);
            condition.Verify(x => x.ClearHasChanged(), Times.Once);
        }
    }
}