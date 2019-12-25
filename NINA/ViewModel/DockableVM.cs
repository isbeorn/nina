#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Profile;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.ViewModel {

    public class DockableVM : BaseVM {

        public DockableVM(IProfileService profileService) : base(profileService) {
            this.CanClose = true;
            this.IsClosed = false;
            IsVisible = true;

            HideCommand = new RelayCommand(Hide);

            profileService.LocationChanged += (object sender, EventArgs e) => {
                RaisePropertyChanged(nameof(Title));
            };
        }

        private bool _isClosed;

        public bool IsClosed {
            get {
                return _isClosed;
            }
            set {
                _isClosed = value;
                RaisePropertyChanged();
            }
        }

        private bool _canClose;

        public bool CanClose {
            get {
                return _canClose;
            }
            set {
                _canClose = value;
                RaisePropertyChanged();
            }
        }

        public string ContentId {
            get {
                return this.GetType().Name;
            }
        }

        private string _titleLabel;

        public string Title {
            get {
                return Locale.Loc.Instance[_titleLabel];
                ;
            }
            set {
                _titleLabel = value;
                RaisePropertyChanged();
            }
        }

        protected bool _isVisible;

        public bool IsVisible {
            get {
                return _isVisible;
            }
            set {
                _isVisible = value;
                RaisePropertyChanged();
            }
        }

        private GeometryGroup _imageGeometry;

        public GeometryGroup ImageGeometry {
            get {
                return _imageGeometry;
            }
            set {
                _imageGeometry = value;
                RaisePropertyChanged();
            }
        }

        public ICommand HideCommand { get; private set; }

        public virtual void Hide(object o) {
            this.IsVisible = !IsVisible;
        }
    }
}