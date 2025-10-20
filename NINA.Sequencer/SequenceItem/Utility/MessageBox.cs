#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_MessageBox_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_MessageBox_Description")]
    [ExportMetadata("Icon", "MessageBoxSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MessageBox : SequenceItem {
        private IWindowServiceFactory windowServiceFactory;

        [ImportingConstructor]
        public MessageBox(IWindowServiceFactory windowServiceFactory) {
            this.windowServiceFactory = windowServiceFactory;
        }

        private MessageBox(MessageBox cloneMe) : this(cloneMe.windowServiceFactory) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new MessageBox(this) {
                Text = Text
            };
        }

        [JsonProperty]
        public string Text { get; set; }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            var service = windowServiceFactory.Create();
            var msgBoxResult = new MessageBoxResult(Text);

            using (token.Register(() => service?.Close())) {
                await service.ShowDialog(msgBoxResult, Loc.Instance["Lbl_Sequencer_Title"]);
            }
            token.ThrowIfCancellationRequested();

            if(!msgBoxResult.Continue) {
                Logger.Info("Stopping Sequence");
                var root = ItemUtility.GetRootContainer(this.Parent);
                root?.Interrupt();
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MessageBox)}, Text: {Text}";
        }
    }

    public class MessageBoxResult {
        public MessageBoxResult(string message) {
            this.Message = message;
            Continue = true;
            ContinueCommand = new GalaSoft.MvvmLight.Command.RelayCommand(() => Continue = true);
            CancelCommand = new GalaSoft.MvvmLight.Command.RelayCommand(() => Continue = false);
        }

        public string Message { get; }
        public bool Continue { get; private set; }

        public ICommand ContinueCommand { get; }
        public ICommand CancelCommand { get; }
    }
}