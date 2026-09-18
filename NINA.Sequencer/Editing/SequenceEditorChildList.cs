#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Container;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NINA.Sequencer.Editing {
    // Existing core insertion operations, without requiring plugins to inherit SequenceContainer.
    internal interface ISequenceEditorInsertion {
        void InsertIntoSequenceBlocks(int index, ISequenceItem item);
        void InsertIntoSequenceBlocks(int index, ISequenceCondition condition);
        void InsertIntoSequenceBlocks(int index, ISequenceTrigger trigger);
    }

    internal sealed class SequenceEditorChildList : ISequenceEditorChildList {
        private readonly Func<IReadOnlyList<ISequenceEntity>> read;
        private readonly Action<int, ISequenceEntity> insert;
        private readonly Action<ISequenceEntity> remove;
        private readonly Action<ISequenceEntity, int> move;
        private SequenceEditorChildList(string name, Func<IReadOnlyList<ISequenceEntity>> read,
            Action<int, ISequenceEntity> insert = null, Action<ISequenceEntity> remove = null, Action<ISequenceEntity, int> move = null) {
            Name = name;
            this.read = read;
            this.insert = insert;
            this.remove = remove;
            this.move = move;
        }
        public string Name { get; }
        public bool IsReadOnly => insert == null;
        public IReadOnlyList<ISequenceEntity> Read() => read();
        public void Insert(int index, ISequenceEntity entity) => (insert ?? throw new InvalidOperationException("Fixed editor child"))(index, entity);
        public void Remove(ISequenceEntity entity) => (remove ?? throw new InvalidOperationException("Fixed editor child"))(entity);
        public void Move(ISequenceEntity entity, int index) => (move ?? throw new InvalidOperationException("Fixed editor child"))(entity, index);

        public static ISequenceEditorChildList Owned(string name, Func<IEnumerable<ISequenceEntity>> read) =>
            new SequenceEditorChildList(name, () => read().Where(child => child != null).ToArray());

        public static ISequenceEditorChildList Slot<T>(string name, Func<T> read, Action<T> write) where T : class, ISequenceEntity =>
            new SequenceEditorChildList(name, () => read() is T child ? new ISequenceEntity[] { child } : Array.Empty<ISequenceEntity>(),
                (index, entity) => write((T)entity), entity => { if (ReferenceEquals(read(), entity)) write(null); }, (entity, index) => { });

        public static ISequenceEditorChildList Collection<T>(string name, Func<IList<T>> list, Func<ICollection<T>> snapshot,
            Action<T> add, Action<T> remove, Action<int, T> insert = null) where T : ISequenceEntity {
            void Move(T entity, int index) {
                IList<T> items = list();
                int previous = items.IndexOf(entity);
                if (previous == index) return;
                if (items is ObservableCollection<T> observable) observable.Move(previous, index);
                else { items.RemoveAt(previous); items.Insert(index, entity); }
            }
            return new SequenceEditorChildList(name, () => snapshot().Cast<ISequenceEntity>().ToArray(),
                (index, entity) => { if (insert != null) insert(index, (T)entity); else { add((T)entity); Move((T)entity, index); } },
                entity => remove((T)entity), (entity, index) => Move((T)entity, index));
        }
    }

    internal sealed class SequenceList : IEquatable<SequenceList> {
        private readonly ISequenceEditorChildList children;
        public SequenceList(ISequenceEntity owner, ISequenceEditorChildList children) { Owner = owner; this.children = children; }
        public ISequenceEntity Owner { get; }
        public bool IsReadOnly => children.IsReadOnly;
        public ISequenceEntity[] Read() => children.Read().ToArray();
        public void Insert(int index, ISequenceEntity entity) => children.Insert(index, entity);
        public void Remove(ISequenceEntity entity) => children.Remove(entity);
        public void Reorder(ISequenceEntity entity, int index) => children.Move(entity, index);
        public bool Equals(SequenceList other) => other != null && ReferenceEquals(Owner, other.Owner) && children.Name == other.children.Name;
        public override bool Equals(object other) => other is SequenceList list && Equals(list);
        public override int GetHashCode() => HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Owner), children.Name);
    }
}