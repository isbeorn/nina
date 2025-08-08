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
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NINA.Sequencer {

    public class SequencerFactory : BaseINPC, ISequencerFactory {
        public IList<ISequenceItem> Items { get; private set; }
        public IList<ISequenceCondition> Conditions { get; private set; }
        public IList<ISequenceTrigger> Triggers { get; private set; }
        public IList<ISequenceContainer> Container { get; private set; }
        public IList<IDateTimeProvider> DateTimeProviders { get; private set; }

        public SequencerFactory(
                IProfileService profileService,
                IList<ISequenceItem> items,
                IList<ISequenceCondition> conditions,
                IList<ISequenceTrigger> triggers,
                IList<ISequenceContainer> container,
                IList<IDateTimeProvider> dateTimeProviders
        ) {
            DateTimeProviders = new List<IDateTimeProvider>(dateTimeProviders);
            Items = new ObservableCollection<ISequenceItem>(items);
            Conditions = new ObservableCollection<ISequenceCondition>(conditions);
            Triggers = new ObservableCollection<ISequenceTrigger>(triggers);
            Container = new ObservableCollection<ISequenceContainer>(container);

            var enitityOptions = new PluginOptionsAccessor(profileService, Guid.Parse("E7C2BE8E-479B-4DBA-A0B0-D513B77F9A54"));
            var allEntities = new List<SidebarEntity>();
            var sidebarItems = new List<SidebarEntity>();
            var sidebarConditions = new List<SidebarEntity>();
            var sidebarTriggers = new List<SidebarEntity>();
            foreach (var item in Items) {
                sidebarItems.Add(new SidebarEntity(item, enitityOptions));
                allEntities.Add(new SidebarEntity(item, enitityOptions));
            }
            foreach (var condition in Conditions) {
                sidebarConditions.Add(new SidebarEntity(condition, enitityOptions));
                allEntities.Add(new SidebarEntity(condition, enitityOptions));
            }
            foreach (var trigger in Triggers) {
                sidebarTriggers.Add(new SidebarEntity(trigger, enitityOptions));
                allEntities.Add(new SidebarEntity(trigger, enitityOptions));
            }

            ItemsView = CollectionViewSource.GetDefaultView(allEntities);
            ItemsView.GroupDescriptions.Add(new PropertyGroupDescription("Entity.Category"));
            ItemsView.SortDescriptions.Add(new SortDescription("Entity.Category", ListSortDirection.Ascending));
            ItemsView.SortDescriptions.Add(new SortDescription("Entity.Name", ListSortDirection.Ascending));
            ItemsView.Filter += new Predicate<object>(ApplyViewFilter);

            InstructionsView = CollectionViewSource.GetDefaultView(sidebarItems);
            InstructionsView.GroupDescriptions.Add(new PropertyGroupDescription("Entity.Category"));
            InstructionsView.SortDescriptions.Add(new SortDescription("Entity.Category", ListSortDirection.Ascending));
            InstructionsView.SortDescriptions.Add(new SortDescription("Entity.Name", ListSortDirection.Ascending));
            InstructionsView.Filter += new Predicate<object>((object o) => (o as SidebarEntity).Enabled);

            ConditionsView = CollectionViewSource.GetDefaultView(sidebarConditions);
            ConditionsView.SortDescriptions.Add(new SortDescription("Entity.Category", ListSortDirection.Ascending));
            ConditionsView.SortDescriptions.Add(new SortDescription("Entity.Name", ListSortDirection.Ascending));
            ConditionsView.Filter += new Predicate<object>((object o) => (o as SidebarEntity).Enabled);

            TriggersView = CollectionViewSource.GetDefaultView(sidebarTriggers);
            TriggersView.SortDescriptions.Add(new SortDescription("Entity.Category", ListSortDirection.Ascending));
            TriggersView.SortDescriptions.Add(new SortDescription("Entity.Name", ListSortDirection.Ascending));
            TriggersView.Filter += new Predicate<object>((object o) => (o as SidebarEntity).Enabled);

            SettingsMode = false;

            ItemsView = CollectionViewSource.GetDefaultView(allEntities);

            profileService.ProfileChanged += ProfileService_ProfileChanged;
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            ViewFilter = string.Empty;
            SettingsMode = false;
        }

        private bool ApplyViewFilter(object obj) {
            var sidebarEntity = obj as SidebarEntity;

            var filterByName = sidebarEntity.Entity.Name.IndexOf(ViewFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            var filterByEnabled = SettingsMode ? true : sidebarEntity.Enabled;
            return filterByEnabled && filterByName;
        }

        private string viewFilter = string.Empty;

        public string ViewFilter {
            get => viewFilter;
            set {
                viewFilter = value;
                ItemsView.Refresh();
            }
        }
        private bool settingsMode;
        public bool SettingsMode {
            get => settingsMode;
            set {
                settingsMode = value;
                RaisePropertyChanged();
                ItemsView.Refresh();
                InstructionsView.Refresh();
                ConditionsView.Refresh();
                TriggersView.Refresh();
            }
        }

        public ICollectionView ItemsView { get; }
        public ICollectionView InstructionsView { get; }
        public ICollectionView ConditionsView { get; }
        public ICollectionView TriggersView { get; }

        public T GetContainer<T>() where T : ISequenceContainer {
            return (T)(Container.FirstOrDefault(x => x.GetType() == typeof(T))?.Clone() ?? default(T));
        }

        public T GetItem<T>() where T : ISequenceItem {
            return (T)(Items.FirstOrDefault(x => x.GetType() == typeof(T))?.Clone() ?? default(T));
        }

        public T GetCondition<T>() where T : ISequenceCondition {
            return (T)(Conditions.FirstOrDefault(x => x.GetType() == typeof(T))?.Clone() ?? default(T));
        }

        public T GetTrigger<T>() where T : ISequenceTrigger {
            return (T)(Triggers.FirstOrDefault(x => x.GetType() == typeof(T))?.Clone() ?? default(T));
        }
    }

    public class SidebarEntity : BaseINPC {
        public SidebarEntity(ISequenceEntity entity, PluginOptionsAccessor entityOptions) {
            Entity = entity;
            this.entityOptions = entityOptions;
        }

        public bool Enabled {
            get => entityOptions.GetValueBoolean(this.Entity.GetType().FullName, true);
            set {
                entityOptions.SetValueBoolean(this.Entity.GetType().FullName, value);
                RaisePropertyChanged();
            }
        }
        public ISequenceEntity Entity { get; }

        private PluginOptionsAccessor entityOptions;
    }
}