#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Web.WebView2.Core;
using NINA.Core.Utility;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.WPF.Base.View {

    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : UserControl {

        public Browser() {
            InitializeComponent();
            IsVisibleChanged += Browser_IsVisibleChanged;
        }

        private async void Browser_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (!IsLoaded) {
                await InitializeWebView2Async();
                webView.Source = Source;
            }            
        }

        public async Task InitializeAndSetSource() {
            await InitializeWebView2Async();
            webView.Source = Source;
        }

        private async Task InitializeWebView2Async() {
            try {
                if (webView.CoreWebView2 != null) { return; }

                string cacheFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "WebView2Cache");

                var environment = await CoreWebView2Environment.CreateAsync(null, cacheFolder);
                await webView.EnsureCoreWebView2Async(environment);
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            
        }

        //public static DependencyProperty CanGoForwardProperty = DependencyProperty.Register("CanGoForward", typeof(bool), typeof(Browser), new PropertyMetadata(true));
        //public bool CanGoForward {
        //    get => (bool)GetValue(CanGoForwardProperty);
        //    set => SetValue(CanGoForwardProperty, value);
        //}

        //public static DependencyProperty CanGoBackProperty = DependencyProperty.Register("CanGoBack", typeof(bool), typeof(Browser), new PropertyMetadata(true));
        //public bool CanGoBack {
        //    get => (bool)GetValue(CanGoBackProperty);
        //    set => SetValue(CanGoBackProperty, value);
        //}

        public static DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(Uri), typeof(Browser), new PropertyMetadata(null));

        public Uri Source {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static DependencyProperty AllowExternalDropProperty = DependencyProperty.Register("AllowExternalDrop", typeof(bool), typeof(Browser), new PropertyMetadata(true));
        public bool AllowExternalDrop {
            get {
                return (bool)GetValue(AllowExternalDropProperty);
            }
            set {
                SetValue(AllowExternalDropProperty, value);
            }
        }

        public static DependencyProperty DefaultBackgroundColorProperty = DependencyProperty.Register("DefaultBackgroundColor", typeof(System.Drawing.Color), typeof(Browser), new PropertyMetadata(System.Drawing.Color.White));
        public System.Drawing.Color DefaultBackgroundColor {
            get {
                return (System.Drawing.Color)GetValue(DefaultBackgroundColorProperty);
            }
            set {
                SetValue(DefaultBackgroundColorProperty, value);
            }
        }

        public static DependencyProperty ZoomFactorProperty = DependencyProperty.Register("ZoomFactor", typeof(double), typeof(Browser), new PropertyMetadata(1.0d));
        public double ZoomFactor {
            get => (double)GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }
    }
}