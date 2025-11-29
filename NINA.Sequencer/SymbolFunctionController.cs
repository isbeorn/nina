using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial class SymbolFunctionController : BaseINPC {
        public SymbolFunctionController(ISymbolBroker symbolBroker, IProfileService profileService) {
            SymbolBroker = symbolBroker;
            ProfileService = profileService;

            dataSymbolFunctions = new ObservableCollection<SymbolFunction>(SymbolBroker.GetFunctions());
            symbolFunctionsView = new CollectionViewSource { Source = DataSymbolFunctions };
            symbolFunctionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SymbolFunction.Category)));
            symbolFunctionsView.SortDescriptions.Add(new SortDescription(nameof(SymbolFunction.Category), ListSortDirection.Ascending));
            symbolFunctionsView.SortDescriptions.Add(new SortDescription(nameof(SymbolFunction.Key), ListSortDirection.Ascending));

            SymbolFunctionsView.Filter += new Predicate<object>(ApplyViewFilter);

            _cts = new CancellationTokenSource();
            _refreshInterval = TimeSpan.FromSeconds(60);
            _backgroundTask = RunRefreshLoopAsync(_cts.Token);

        }

        private bool ApplyViewFilter(object obj) {
            return (obj as SymbolFunction).Key.IndexOf(ViewFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private CollectionViewSource symbolFunctionsView;
        public ICollectionView SymbolFunctionsView => symbolFunctionsView.View;
        public ISymbolBroker SymbolBroker { get; }
        public IProfileService ProfileService { get; }

        [ObservableProperty]
        private ObservableCollection<SymbolFunction> dataSymbolFunctions;

        [ObservableProperty]
        private string viewFilter = string.Empty;

        partial void OnViewFilterChanged(string value) {
            SymbolFunctionsView.Refresh();
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
            var latest = await Task.Run(() => SymbolBroker.GetFunctions(), token).ConfigureAwait(false);

            // Switch to UI thread to update bindings & view
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess()) {
                ApplySymbolFunctions(latest);
            } else {
                await dispatcher.InvokeAsync(
                    () => ApplySymbolFunctions(latest),
                    System.Windows.Threading.DispatcherPriority.DataBind,
                    token);
            }
        }

        private void ApplySymbolFunctions(IReadOnlyCollection<SymbolFunction> latest) {
            // Build lookup of the latest symbols by (Category, Key).
            // If there are duplicates in 'latest', keep the last one.
            var latestByCatKey = latest
                .GroupBy(s => (s.Category, s.Key))
                .ToDictionary(g => g.Key, g => g.Last());

            // Update existing symbol functions or remove if they no longer exist in latest
            for (int i = 0; i < DataSymbolFunctions.Count; i++) {
                var cur = DataSymbolFunctions[i];
                var key = (cur.Category, cur.Key);

                if (latestByCatKey.TryGetValue(key, out var src)) {
                    //if (!Equals(cur.Name, src.Name))
                    //    cur.Name = src.Name;
                } else {
                    // Not present in latest -> remove
                    DataSymbolFunctions.RemoveAt(i);
                    i--;
                }
            }

            // Add any symbols that are in latest but missing in DataSymbols
            foreach (var kv in latestByCatKey) {
                if (!DataSymbolFunctions.Any(s => s.Category == kv.Key.Category && s.Key == kv.Key.Key)) {
                    DataSymbolFunctions.Add(kv.Value);
                }
            }
        }
    }
}
