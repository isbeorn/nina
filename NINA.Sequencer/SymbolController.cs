using Accord.Math;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Logic;
using Parlot.Fluent;
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
    public partial class SymbolController : BaseINPC {
        public SymbolController(ISymbolBroker symbolBroker, IProfileService profileService) {
            SymbolBroker = symbolBroker;
            ProfileService = profileService;

            dataSymbols = new ObservableCollection<Symbol>(SymbolBroker.GetSymbols());
            symbolsView = new CollectionViewSource { Source = DataSymbols };
            symbolsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Symbol.Category)));
            SymbolsView.Filter += new Predicate<object>(ApplyViewFilter);

            _cts = new CancellationTokenSource();
            _refreshInterval = TimeSpan.FromSeconds(5);
            _backgroundTask = RunRefreshLoopAsync(_cts.Token);

        }

        private bool ApplyViewFilter(object obj) {
            return (obj as Symbol).Key.IndexOf(ViewFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private CollectionViewSource symbolsView;
        public ICollectionView SymbolsView => symbolsView.View;
        public ISymbolBroker SymbolBroker { get; }
        public IProfileService ProfileService { get; }

        [ObservableProperty]
        private ObservableCollection<Symbol> dataSymbols;

        [ObservableProperty]
        private string viewFilter = string.Empty;

        partial void OnViewFilterChanged(string value) {
            SymbolsView.Refresh();
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
            // Build lookup of the latest symbols by (Category, Key).
            // If there are duplicates in 'latest', keep the last one.
            var latestByCatKey = latest
                .GroupBy(s => (s.Category, s.Key))
                .ToDictionary(g => g.Key, g => g.Last());

            // Update existing symbols or remove if they no longer exist in latest
            for (int i = 0; i < DataSymbols.Count; i++) {
                var cur = DataSymbols[i];
                var key = (cur.Category, cur.Key);

                if (latestByCatKey.TryGetValue(key, out var src)) {
                    if (!Equals(cur.Value, src.Value))
                        cur.Value = src.Value;
                } else {
                    // Not present in latest -> remove
                    DataSymbols.RemoveAt(i);
                    i--;
                }
            }

            // Add any symbols that are in latest but missing in DataSymbols
            foreach (var kv in latestByCatKey) {
                if (!DataSymbols.Any(s => s.Category == kv.Key.Category && s.Key == kv.Key.Key)) {
                    DataSymbols.Add(kv.Value);
                }
            }
        }


        public IList<Symbol> GetHiddenSymbols(string category) => SymbolBroker.GetHiddenSymbols(category) ?? Array.Empty<Symbol>();
    }
}
