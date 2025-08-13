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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Core.Model {

    public class PagedList<T> : BaseINPC {

        public PagedList(int pageSize, IEnumerable<T> items) {
            _items = new List<T>(items);
            PageSize = pageSize;

            var counter = 1;
            for (int i = 0; i < _items.Count; i += PageSize) {
                Pages.Add(counter++);
            }

            LoadFirstPage().Wait();

            FirstPageCommand = new AsyncCommand<bool>(LoadFirstPage, (object o) => { return CurrentPage > 1; });
            PrevPageCommand = new AsyncCommand<bool>(LoadPrevPage, (object o) => { return CurrentPage > 1; });
            NextPageCommand = new AsyncCommand<bool>(LoadNextPage, (object o) => { return CurrentPage < Pages.Count; });
            LastPageCommand = new AsyncCommand<bool>(LoadLastPage, (object o) => { return CurrentPage < Pages.Count; });
            PageByNumberCommand = new AsyncCommand<bool>(async () => await LoadPage(CurrentPage));
        }

        private List<T> _items;

        private T _selectedItem;

        public T SelectedItem {
            get => _selectedItem;
            set {
                _selectedItem = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> LoadFirstPage() {
            return await LoadPage(Pages.FirstOrDefault());
        }

        private async Task<bool> LoadNextPage() {
            return await LoadPage(CurrentPage + 1);
        }

        private async Task<bool> LoadPrevPage() {
            return await LoadPage(CurrentPage - 1);
        }

        private async Task<bool> LoadLastPage() {
            return await LoadPage(Pages.Count);
        }

        private async Task<bool> LoadPage(int page) {
            var idx = page - 1;
            if (idx < 0) {
                return false;
            } else if (idx > (Count / (double)PageSize)) {
                return false;
            }

            var itemChunk = await Task.Run(() => {
                var offset = Math.Min(_items.Count - (idx * PageSize), PageSize);
                return _items.GetRange(idx * PageSize, offset);
            });

            ItemPage = new AsyncObservableCollection<T>(itemChunk);

            CurrentPage = page;
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged(nameof(PageStartIndex));
            RaisePropertyChanged(nameof(PageEndIndex));
            return true;
        }

        private AsyncObservableCollection<T> _itemPage = new AsyncObservableCollection<T>();

        public AsyncObservableCollection<T> ItemPage {
            get => _itemPage;
            private set {
                _itemPage = value;
                RaisePropertyChanged();
            }
        }

        public int PageStartIndex {
            get {
                if (_items.Count == 0) {
                    return 0;
                } else {
                    return PageSize * (CurrentPage - 1) + 1;
                }
            }
        }

        public int PageEndIndex {
            get {
                if (PageSize * CurrentPage > _items.Count) {
                    return _items.Count;
                } else {
                    return PageSize * CurrentPage;
                }
            }
        }

        private int _pageSize;

        public int PageSize {
            get => _pageSize;
            set {
                _pageSize = value;
                RaisePropertyChanged();
            }
        }

        private int _currentPage;

        public int CurrentPage {
            get => _currentPage;
            set {
                if (value >= Pages.FirstOrDefault() && value <= Pages.LastOrDefault()) {
                    _currentPage = value;
                    RaisePropertyChanged();
                }
            }
        }

        private AsyncObservableCollection<int> _pages = new AsyncObservableCollection<int>();

        public AsyncObservableCollection<int> Pages {
            get => _pages;
            private set {
                _pages = value;
                RaisePropertyChanged();
            }
        }

        public int Count => _items.Count;

        public ICommand FirstPageCommand { get; private set; }
        public ICommand PrevPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        public ICommand LastPageCommand { get; private set; }
        public ICommand PageByNumberCommand { get; private set; }
    }
}