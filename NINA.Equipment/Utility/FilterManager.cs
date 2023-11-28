using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.Utility {
    internal class FilterManager {
        public FilterManager() { }

        public AsyncObservableCollection<FilterInfo> SyncFiltersWithPositions(AsyncObservableCollection<FilterInfo> filtersList, int wheelPositions) {
            RemoveDuplicateFilters(filtersList);
            FillMissingPositions(filtersList, wheelPositions);
            EnsureCorrectNumberOfFilters(filtersList, wheelPositions);
            return filtersList;
        }

        private void RemoveDuplicateFilters(AsyncObservableCollection<FilterInfo> filtersList) {
            var duplicates = filtersList.GroupBy(x => x.Position).Where(x => x.Count() > 1);

            foreach (var group in duplicates) {
                foreach (var filterToRemove in group) {
                    Logger.Warning($"Duplicate filter position defined in filter list. Removing the duplicates and importing from filter wheel again. Removing filter: {filterToRemove.Name}, focus offset: {filterToRemove.FocusOffset}");
                    filtersList.Remove(filterToRemove);
                }
            }
        }

        private void FillMissingPositions(AsyncObservableCollection<FilterInfo> filtersList, int positions) {
            if (!filtersList.Any()) return;

            var existingPositions = filtersList.Select(x => (int)x.Position);
            var missingPositions = Enumerable.Range(0, existingPositions.Max()).Except(existingPositions);

            foreach (var position in missingPositions) {
                if (positions > position) {
                    var filterToAdd = new FilterInfo($"Slot {position}", 0, (short)position);
                    Logger.Warning($"Missing filter position. Importing filter: {filterToAdd.Name}, focus offset: {filterToAdd.FocusOffset}");
                    filtersList.Insert(position, filterToAdd);
                }
            }
        }

        private void EnsureCorrectNumberOfFilters(AsyncObservableCollection<FilterInfo> filtersList, int positions) {
            if (positions < filtersList.Count) {
                TruncateExcessFilters(filtersList, positions);
            } else {
                AddMissingFilters(filtersList, positions);
            }
        }

        private void TruncateExcessFilters(AsyncObservableCollection<FilterInfo> filtersList, int positions) {
            while (filtersList.Count > positions) {
                var filterToRemove = filtersList.Last();
                Logger.Warning($"Too many filters defined in the equipment filter list. Removing filter: {filterToRemove.Name}, focus offset: {filterToRemove.FocusOffset}");
                filtersList.Remove(filterToRemove);
            }
        }

        private void AddMissingFilters(AsyncObservableCollection<FilterInfo> filtersList, int positions) {
            while (filtersList.Count < positions) {
                var filterToAdd = new FilterInfo($"Slot {filtersList.Count}", 0, (short)filtersList.Count);
                Logger.Info($"Not enough filters defined in the equipment filter list. Importing filter: {filterToAdd.Name}, focus offset: {filterToAdd.FocusOffset}");
                filtersList.Add(filterToAdd);
            }
        }
    }
}
