using Accord.Math;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NINA.Sequencer {
    public class SymbolController : BaseINPC {
        public SymbolController(ISymbolBroker symbolBroker, IProfileService profileService) {
            SymbolBroker = symbolBroker;
            ProfileService = profileService;

            DataSymbols = new ObservableCollection<Symbol>(SymbolBroker.GetSymbols());
            symbolsView = new CollectionViewSource { Source = DataSymbols };
            symbolsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Symbol.Category)));

            _cts = new CancellationTokenSource();
            _refreshInterval = TimeSpan.FromSeconds(5);
            _backgroundTask = RunRefreshLoopAsync(_cts.Token);

        }

        private CollectionViewSource symbolsView;
        public ICollectionView SymbolsView => symbolsView.View;
        public ISymbolBroker SymbolBroker { get; }
        public IProfileService ProfileService { get; }

        private ObservableCollection<Symbol> dataSymbols;

        public ObservableCollection<Symbol> DataSymbols {
            get => dataSymbols;
            set {
                dataSymbols = value;
                RaisePropertyChanged();
            }
        }

        private readonly CancellationTokenSource _cts;
        private readonly TimeSpan _refreshInterval;
        private readonly Task _backgroundTask;

        private async Task RunRefreshLoopAsync(CancellationToken token) {
            using var timer = new PeriodicTimer(_refreshInterval);

            try {
                await RefreshOnceAsync(token).ConfigureAwait(false);

                while (await timer.WaitForNextTickAsync(token)) {
                    try {
                        await RefreshOnceAsync(token).ConfigureAwait(false);
                    } catch (OperationCanceledException) {
                        break;
                    } catch (Exception) {
                    }
                }
            } catch (OperationCanceledException) {
                // normal shutdown
            }
        }

        private async Task RefreshOnceAsync(CancellationToken token) {
            var latest = await Task.Run(() => SymbolBroker.GetSymbols(), token).ConfigureAwait(false);

            // Switch to UI thread to update bindings & view
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess()) {
                ApplySymbols(latest);
            } else {
                await dispatcher.InvokeAsync(
                    () => ApplySymbols(latest),
                    System.Windows.Threading.DispatcherPriority.DataBind,
                    token);
            }
        }

        private void ApplySymbols(IReadOnlyList<Symbol> latest) {
            var latestByCatKey = latest.ToDictionary(s => (s.Category, s.Key));

            foreach (var cur in DataSymbols) {
                if (latestByCatKey.TryGetValue((cur.Category, cur.Key), out var src)) {
                    if (!Equals(cur.Value, src.Value))
                        cur.Value = src.Value;
                }
            }

            foreach (var kv in latestByCatKey) {
                if (!DataSymbols.Any(s => s.Category == kv.Key.Category && s.Key == kv.Key.Key))
                    DataSymbols.Add(kv.Value);
            }

        }
    }
}
