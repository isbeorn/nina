#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
