#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.PlateSolving.Solvers;
using NINA.Core.Enum;
using NINA.PlateSolving.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    public class PlateSolverFactoryProxy : IPlateSolverFactory {

        public IPlateSolver GetBlindSolver(IPlateSolveSettings plateSolveSettings) {
            return PlateSolverFactory.GetBlindSolver(plateSolveSettings);
        }

        public IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings) {
            return PlateSolverFactory.GetPlateSolver(plateSolveSettings);
        }

        public IImageSolver GetImageSolver(IPlateSolver plateSolver, IPlateSolver blindSolver) {
            return new ImageSolver(plateSolver, blindSolver);
        }

        public ICaptureSolver GetCaptureSolver(IPlateSolver plateSolver, IPlateSolver blindSolver, IImagingMediator imagingMediator, IFilterWheelMediator filterWheelMediator) {
            return new CaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator);
        }

        public ICenteringSolver GetCenteringSolver(IPlateSolver plateSolver, IPlateSolver blindSolver, IImagingMediator imagingMediator, ITelescopeMediator telescopeMediator, IFilterWheelMediator filterWheelMediator, IDomeMediator domeMediator, IDomeFollower domeFollower) {
            return new CenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator, filterWheelMediator, domeMediator, domeFollower);
        }
    }

    public static class PlateSolverFactory {
        private static readonly object providerSync = new object();
        private static IReadOnlyList<IPlateSolverProvider> registeredProviders = Array.AsReadOnly(Array.Empty<IPlateSolverProvider>());
        private static Dictionary<string, IPlateSolverProvider> providersById = new Dictionary<string, IPlateSolverProvider>(StringComparer.Ordinal);

        public static IReadOnlyList<IPlateSolverProvider> Providers {
            get {
                lock (providerSync) {
                    return registeredProviders;
                }
            }
        }

        public static event EventHandler ProvidersChanged;

        public static void RegisterProviders(IEnumerable<IPlateSolverProvider> providers) {
            ArgumentNullException.ThrowIfNull(providers);
            var valid = new Dictionary<string, IPlateSolverProvider>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var provider in providers) {
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

            lock (providerSync) {
                providersById = valid;
                registeredProviders = Array.AsReadOnly(valid.Values.ToArray());
            }

            ProvidersChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Creates an instance of a Platesolver depending on the solver
        /// </summary>
        /// <param name="plateSolveSettings"></param>
        /// <param name="solver"> Plate Solver that should be used</param>
        /// <returns></returns>
        private static IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings, PlateSolverEnum solver) {
            return solver switch {
                PlateSolverEnum.ASTROMETRY_NET => new AstrometryPlateSolver(plateSolveSettings.AstrometryURL, plateSolveSettings.AstrometryAPIKey),
                PlateSolverEnum.LOCAL => new LocalPlateSolver(plateSolveSettings.CygwinLocation),
                PlateSolverEnum.PLATESOLVE2 => new Platesolve2Solver(plateSolveSettings.PS2Location),
                PlateSolverEnum.PLATESOLVE3 => new Platesolve3Solver(plateSolveSettings.PS3Location),
                PlateSolverEnum.ASPS => new AllSkyPlateSolver(plateSolveSettings.AspsLocation),
                PlateSolverEnum.TSX_IMAGELINK => new TheSkyXImageLinkSolver(plateSolveSettings.TheSkyXHost, plateSolveSettings.TheSkyXPort),
                PlateSolverEnum.PINPONT => new Dc3PinPointSolver(plateSolveSettings),
                _ => new ASTAPSolver(plateSolveSettings.ASTAPLocation),
            };
        }

        public static IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings) {
            if (!string.IsNullOrEmpty(plateSolveSettings.PlateSolverProviderId)) {
                return new ProviderPlateSolver(plateSolveSettings.PlateSolverProviderId, false, plateSolveSettings);
            }

            return GetPlateSolver(plateSolveSettings, plateSolveSettings.PlateSolverType);
        }

        public static IPlateSolver GetBlindSolver(IPlateSolveSettings plateSolveSettings) {
            if (!string.IsNullOrEmpty(plateSolveSettings.BlindSolverProviderId)) {
                return new ProviderPlateSolver(plateSolveSettings.BlindSolverProviderId, true, plateSolveSettings);
            }

            var type = plateSolveSettings.BlindSolverType switch {
                BlindSolverEnum.ASTROMETRY_NET => PlateSolverEnum.ASTROMETRY_NET,
                BlindSolverEnum.LOCAL => PlateSolverEnum.LOCAL,
                BlindSolverEnum.PLATESOLVE3 => PlateSolverEnum.PLATESOLVE3,
                BlindSolverEnum.ASPS => PlateSolverEnum.ASPS,
                BlindSolverEnum.PINPOINT => PlateSolverEnum.PINPONT,
                _ => PlateSolverEnum.ASTAP
            };

            return GetPlateSolver(plateSolveSettings, type);
        }

        private sealed class ProviderPlateSolver : IPlateSolver {
            private readonly Lazy<IPlateSolver> solver;

            public ProviderPlateSolver(string id, bool blind, IPlateSolveSettings settings) {
                solver = new Lazy<IPlateSolver>(() => {
                    IPlateSolverProvider provider;
                    lock (providerSync) {
                        providersById.TryGetValue(id, out provider);
                    }

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
}