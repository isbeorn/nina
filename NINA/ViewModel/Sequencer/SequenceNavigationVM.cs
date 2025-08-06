#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Utility;
using NINA.Utility;
using NINA.ViewModel.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Sequencer {

    public class SequenceNavigationVM : DockableVM, ISequenceNavigationVM {
        private object activeSequencerVM;
        private ISequence2VM sequence2VM;
        private ISequenceMediator sequenceMediator;
        private ISequencerFactory factory;
        private ISimpleSequenceVM simpleSequenceVM;

        /// <summary>
        /// Backwards compatible ContentId due to sequencer replacement
        /// </summary>
        public override string ContentId => "SequenceVM";

        public SequenceNavigationVM(
                IProfileService profileService,
                ICommandLineOptions commandLineOptions,
                ISequenceMediator sequenceMediator,
                IPluginLoader pluginProvider,
                ICameraMediator cameraMediator,
                IApplicationStatusMediator applicationStatusMediator,
                INighttimeCalculator nighttimeCalculator,
                IPlanetariumFactory planetariumFactory,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                ISymbolBrokerVM symbolBrokerVM) : base(profileService) {
            Title = Loc.Instance["LblSequence"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current?.Resources["SequenceSVG"];

            //this.factory = factory;
            //this.simpleSequenceVM = simpleSequenceVM;
            //this.sequence2VM = sequence2VM;
            this.sequenceMediator = sequenceMediator;
            this.sequenceMediator.RegisterSequenceNavigation(this);

            AddTargetCommand = new RelayCommand((object o) => {
                simpleSequenceVM.AddTargetCommand.Execute(o);
                ActiveSequencerVM = simpleSequenceVM;
            });
            SwitchToTargetSetCommand = new RelayCommand((object o) => {
                ActiveSequencerVM = simpleSequenceVM;
            });
            LoadSequenceCommand = new RelayCommand((object o) => {
                if (SimpleSequenceVM.LoadTarget()) {
                    ActiveSequencerVM = simpleSequenceVM;
                }
            });
            LoadTargetSetCommand = new RelayCommand((object o) => {
                if (SimpleSequenceVM.LoadTargetSet()) {
                    ActiveSequencerVM = simpleSequenceVM;
                }
            });
            ImportTargetsCommand = new RelayCommand((object o) => {
                if (SimpleSequenceVM.ImportTargets()) {
                    ActiveSequencerVM = simpleSequenceVM;
                }
            });
            SwitchToAdvancedSequenceCommand = new RelayCommand((object o) => SwitchToAdvancedView());

            Task.Run(async () => {
                await pluginProvider.Load();
                this.factory = new SequencerFactory(profileService, pluginProvider.Items, pluginProvider.Conditions, pluginProvider.Triggers, pluginProvider.Container, pluginProvider.DateTimeProviders);

                this.simpleSequenceVM = new SimpleSequenceVM(profileService, sequenceMediator, cameraMediator, applicationStatusMediator, nighttimeCalculator, planetariumFactory, framingAssistantVM, applicationMediator, factory);
                this.sequence2VM = new Sequence2VM(profileService, commandLineOptions, sequenceMediator, applicationMediator, applicationStatusMediator, cameraMediator, factory, symbolBrokerVM);

                await Task.WhenAll(simpleSequenceVM.Initialize(), sequence2VM.Initialize());

                Initialized = true;

                if (profileService.ActiveProfile.SequenceSettings.DisableSimpleSequencer || commandLineOptions.RunSequence) {
                    this.ActiveSequencerVM = sequence2VM;
                } else {
                    ActiveSequencerVM = this;
                }
            });
        }

        public object ActiveSequencerVM {
            get => activeSequencerVM;
            set {
                activeSequencerVM = value;
                RaisePropertyChanged();
            }
        }

        public ICommand AddTargetCommand { get; private set; }
        public ICommand LoadSequenceCommand { get; private set; }
        public ICommand LoadTargetSetCommand { get; private set; }
        public ICommand ImportTargetsCommand { get; private set; }
        public ICommand SwitchToAdvancedSequenceCommand { get; private set; }
        public ICommand SwitchToTargetSetCommand { get; private set; }

        public ISimpleSequenceVM SimpleSequenceVM => simpleSequenceVM;

        public ISequence2VM Sequence2VM => sequence2VM;

        public bool SimpleSequenceHasTargets => ((SimpleSequenceVM)simpleSequenceVM).Targets.Items.Count > 0;

        private object lockObj = new object();
        private bool initialized;

        public bool Initialized {
            get {
                lock (lockObj) {
                    return initialized;
                }
            }
            private set {
                lock (lockObj) {
                    initialized = value;
                    RaisePropertyChanged();
                }
            }
        }

        public override string ToString() {
            return string.Empty;
        }

        public void SwitchToAdvancedView() {
            ActiveSequencerVM = sequence2VM;
        }

        public void SwitchToOverview() {
            ActiveSequencerVM = this;
        }

        public void AddSimpleTarget(IDeepSkyObject deepSkyObject) {
            SimpleSequenceVM.AddTarget(deepSkyObject);
            ActiveSequencerVM = SimpleSequenceVM;
        }

        public void AddAdvancedTarget(IDeepSkyObjectContainer container) {
            Sequence2VM.AddTarget(container);
            ActiveSequencerVM = Sequence2VM;
        }

        public void SetAdvancedSequence(ISequenceRootContainer container) {
            Sequence2VM.Sequencer.MainContainer = container;
        }

        public IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates() {
            return Sequence2VM.GetDeepSkyObjectContainerTemplates();
        }

        public void AddTargetToTargetList(IDeepSkyObjectContainer container) {
            Sequence2VM.AddTargetToTargetList(container);
        }

        public IList<IDeepSkyObjectContainer> GetAllTargetsInSimpleSequence() {
            return ItemUtility.LookForTargetsDownwards(SimpleSequenceVM.Sequencer.MainContainer);
        }

        public IList<IDeepSkyObjectContainer> GetAllTargetsInAdvancedSequence() {
            return ItemUtility.LookForTargetsDownwards(Sequence2VM.Sequencer.MainContainer);
        }
    }
}