#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Sequencer.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer {

    public enum TemplateReferenceSourceKind {
        Default,
        User
    }

    public enum TemplateLinkState {
        Pending,
        Loading,
        Resolved,
        Missing,
        Invalid
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TemplateReference {
        private string relativePath = string.Empty;

        [JsonProperty]
        public TemplateReferenceSourceKind SourceKind { get; set; }

        [JsonProperty]
        public string RelativePath {
            get => relativePath;
            set => relativePath = NormalizeRelativePath(value);
        }

        [JsonProperty]
        public string DisplayName { get; set; } = string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(RelativePath);

        public TemplateReference Clone() {
            return new TemplateReference {
                SourceKind = SourceKind,
                RelativePath = RelativePath,
                DisplayName = DisplayName
            };
        }

        public static string NormalizeRelativePath(string path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return string.Empty;
            }

            string normalized = path.Replace('\\', '/');
            string[] parts = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("/", parts);
        }
    }

    public interface ITemplateLinkResolver {
        bool InitialLoadComplete { get; }
        event EventHandler TemplatesChanged;

        Task WaitForInitialLoad(CancellationToken token);
        bool TryResolve(TemplateReference reference, out TemplatedSequenceContainer template);
        Task SaveTemplate(TemplateReference reference, ISequenceContainer container, CancellationToken token);
        void UpdateTemplates(
            IEnumerable<TemplatedSequenceContainer> templates,
            bool initialLoadComplete,
            Func<TemplateReference, ISequenceContainer, CancellationToken, Task> saveTemplate);
    }

    public class TemplateLinkResolver : ITemplateLinkResolver {
        private readonly object lockObj = new object();
        private readonly TaskCompletionSource<bool> initialLoadCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private List<TemplatedSequenceContainer> templates = new List<TemplatedSequenceContainer>();
        private Func<TemplateReference, ISequenceContainer, CancellationToken, Task> saveTemplate;
        private bool initialLoadComplete;

        public event EventHandler TemplatesChanged;

        public bool InitialLoadComplete {
            get {
                lock (lockObj) {
                    return initialLoadComplete;
                }
            }
        }

        public async Task WaitForInitialLoad(CancellationToken token) {
            if (InitialLoadComplete) {
                return;
            }

            await initialLoadCompletionSource.Task.WaitAsync(token);
        }

        public bool TryResolve(TemplateReference reference, out TemplatedSequenceContainer template) {
            template = null;
            if (reference == null || !reference.IsValid) {
                return false;
            }

            string relativePath = TemplateReference.NormalizeRelativePath(reference.RelativePath);
            lock (lockObj) {
                template = templates.FirstOrDefault(t =>
                    t.Reference != null
                    && t.Reference.SourceKind == reference.SourceKind
                    && string.Equals(t.Reference.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));
            }

            return template != null;
        }

        public Task SaveTemplate(TemplateReference reference, ISequenceContainer container, CancellationToken token) {
            Func<TemplateReference, ISequenceContainer, CancellationToken, Task> callback;
            lock (lockObj) {
                callback = saveTemplate;
            }

            if (callback == null) {
                throw new InvalidOperationException("Template saving is not available yet.");
            }

            return callback(reference, container, token);
        }

        public void UpdateTemplates(
            IEnumerable<TemplatedSequenceContainer> templates,
            bool initialLoadComplete,
            Func<TemplateReference, ISequenceContainer, CancellationToken, Task> saveTemplate) {
            lock (lockObj) {
                this.templates = templates?.Where(t => t.Reference != null).ToList() ?? new List<TemplatedSequenceContainer>();
                this.saveTemplate = saveTemplate ?? this.saveTemplate;
                if (initialLoadComplete) {
                    this.initialLoadComplete = true;
                    initialLoadCompletionSource.TrySetResult(true);
                }
            }

            TemplatesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}