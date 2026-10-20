#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal sealed class PlateSolverProviderRegistry {
        private readonly object sync = new object();
        private IReadOnlyList<IPlateSolverProvider> providers = Array.AsReadOnly(Array.Empty<IPlateSolverProvider>());
        private Dictionary<string, IPlateSolverProvider> byId = new Dictionary<string, IPlateSolverProvider>(StringComparer.Ordinal);

        public event EventHandler Changed;

        public IReadOnlyList<IPlateSolverProvider> Providers {
            get {
                lock (sync) {
                    return providers;
                }
            }
        }

        public IPlateSolverProvider Find(string id) {
            lock (sync) {
                return byId.TryGetValue(id, out var provider) ? provider : null;
            }
        }

        public void Replace(IEnumerable<IPlateSolverProvider> candidates) {
            ArgumentNullException.ThrowIfNull(candidates);
            var valid = new Dictionary<string, IPlateSolverProvider>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var provider in candidates) {
                try {
                    var id = provider?.Id;
                    if (string.IsNullOrWhiteSpace(id)) {
                        Logger.Warning("Ignoring a plate solver provider without a stable ID");
                        continue;
                    }

                    if (!seen.Add(id)) {
                        valid.Remove(id);
                        Logger.Warning($"Conflicting plate solver provider ID '{id}'; all providers with this ID are unavailable");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(provider.Name) || (!provider.SupportsPrimary && !provider.SupportsBlind)) {
                        Logger.Warning($"Ignoring invalid plate solver provider '{id}'");
                        continue;
                    }

                    valid.Add(id, provider);
                } catch (Exception ex) {
                    Logger.Error("Failed to inspect a plate solver provider", ex);
                }
            }

            lock (sync) {
                byId = valid;
                providers = Array.AsReadOnly(valid.Values.ToArray());
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    internal sealed class ProviderPlateSolver : IPlateSolver {
        private readonly Lazy<IPlateSolver> solver;

        public ProviderPlateSolver(PlateSolverProviderRegistry registry, string id, bool blind, IPlateSolveSettings settings) {
            solver = new Lazy<IPlateSolver>(() => {
                var provider = registry.Find(id);
                if (provider == null || (blind ? !provider.SupportsBlind : !provider.SupportsPrimary)) {
                    throw new InvalidOperationException(string.Format(Loc.Instance["LblPlateSolverProviderUnavailable"], provider?.Name ?? id));
                }

                return provider.CreateSolver(settings)
                    ?? throw new InvalidOperationException(string.Format(Loc.Instance["LblPlateSolverProviderCreationFailed"], provider.Name));
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Task<PlateSolveResult> SolveAsync(IImageData source, PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            canceltoken.ThrowIfCancellationRequested();
            return solver.Value.SolveAsync(source, parameter, progress, canceltoken);
        }
    }
}
