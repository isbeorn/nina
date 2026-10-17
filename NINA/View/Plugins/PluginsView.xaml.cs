#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;
using NINA.Plugin.Interfaces;
using NINA.ViewModel.Plugins;

namespace NINA.View.Plugins {

    /// <summary>
    /// Interaction logic for PluginsView.xaml
    /// </summary>
    public partial class PluginsView : UserControl {
        private PluginsVM manager;
        private PluginEntryVM selectedEntry;
        private IPluginManifest settingsManifest;
        private DataTemplate optionsTemplate;
        private bool showingInstalled;
        private readonly double maximumListWidth;
        private readonly PluginChangelogVM changelog = new();
        private readonly PluginShowcaseVM showcase = new();

        public PluginsView() {
            InitializeComponent();
            ChangelogContent.DataContext = changelog;
            ShowcaseContent.DataContext = showcase;
            maximumListWidth = PluginListColumn.MaxWidth;
            Unloaded += OnUnloaded;
            IsVisibleChanged += OnVisibilityChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            AttachManager(e.NewValue as PluginsVM);
        }

        private void AttachManager(PluginsVM value) {
            if (manager != null) {
                manager.PropertyChanged -= OnManagerPropertyChanged;
                manager.IsVersionHistoryVisible = false;
            }

            manager = value;
            if (manager != null) {
                manager.PropertyChanged += OnManagerPropertyChanged;
            }

            SelectEntry();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            AttachManager(DataContext as PluginsVM);
            FetchInitialCatalog();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            AttachManager(null);
        }

        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (IsLoaded) {
                changelog.SetActive(IsVisible && ChangelogTab.IsSelected && ChangelogTab.Visibility == Visibility.Visible);
                showcase.SetActive(IsVisible && ShowcaseTab.IsSelected);
            }
        }

        private async void FetchInitialCatalog() {
            if (IsLoaded && manager?.Initialized == true && manager.FetchPluginsCommand.CanExecute("Initial")) {
                await manager.FetchPluginsCommand.ExecuteAsync("Initial");
            }
        }

        private void OnManagerPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(PluginsVM.SelectedPlugin)) {
                SelectEntry();
            }

            if (e.PropertyName == nameof(PluginsVM.Initialized)) {
                FetchInitialCatalog();
            }

            if (e.PropertyName == nameof(PluginsVM.IsInstalledTab)) {
                UpdateSettings();
            }
        }

        private void SelectEntry() {
            if (selectedEntry != null) {
                selectedEntry.PropertyChanged -= OnEntryPropertyChanged;
            }

            selectedEntry = manager?.SelectedPlugin;
            if (selectedEntry != null) {
                selectedEntry.PropertyChanged += OnEntryPropertyChanged;
            }

            UpdateSettings();
        }

        private void OnEntryPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(PluginEntryVM.SettingsManifest)) {
                UpdateSettings();
            }
        }

        private void UpdateSettings() {
            if (SettingsPresenter == null) {
                return;
            }

            var manifest = selectedEntry?.SettingsManifest;
            bool manifestChanged = !ReferenceEquals(manifest, settingsManifest);
            bool isInstalled = manager?.IsInstalledTab == true;
            bool sectionChanged = showingInstalled != isInstalled;
            showingInstalled = isInstalled;
            if (manifestChanged) {
                settingsManifest = manifest;
                SettingsPresenter.Content = null;
                SettingsPresenter.ContentTemplate = null;
            }

            var selector = (PluginOptionsDataTemplateSelector)Resources["OptionsSelector"];
            optionsTemplate = manifest == null ? null : selector.SelectTemplate(manifest, SettingsPresenter);
            bool hasSettings = optionsTemplate != null && optionsTemplate != selector.Default;
            SettingsTab.Visibility = isInstalled && hasSettings
                ? Visibility.Visible
                : Visibility.Collapsed;
            NoSettingsHint.Visibility = isInstalled && selectedEntry?.CanShowSettings == true && !hasSettings
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (manifestChanged || sectionChanged) {
                SettingsTab.IsSelected = isInstalled && hasSettings;
                DescriptionTab.IsSelected = !SettingsTab.IsSelected;
            }

            var displayedManifest = selectedEntry?.DisplayManifest;
            changelog.Select(displayedManifest?.ChangelogURL, displayedManifest?.Version?.ToString());
            showcase.Select(displayedManifest?.Identifier,
                [displayedManifest?.Descriptions?.ScreenshotURL, displayedManifest?.Descriptions?.AltScreenshotURL],
                displayedManifest?.Version?.ToString());
            ChangelogTab.Visibility = string.IsNullOrWhiteSpace(displayedManifest?.ChangelogURL)
                ? Visibility.Collapsed
                : Visibility.Visible;
            if (ChangelogTab.IsSelected && ChangelogTab.Visibility != Visibility.Visible) {
                DescriptionTab.IsSelected = true;
            }

            UpdateDetailContent();
        }

        private void OnDetailsSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.Source == DetailsNavigation) {
                UpdateDetailContent();
            }
        }

        private void OnMarkdownUpdated(object sender, DataTransferEventArgs e) {
            if (sender is MdXaml.MarkdownScrollViewer viewer && viewer.Document is FlowDocument document) {
                FormatMarkdown(document, viewer == ChangelogMarkdown ? changelog.DocumentUri : null);
            }
        }

        private void FormatMarkdown(DependencyObject element, Uri documentUri) {
            if (element is List list
                && list.MarkerStyle is TextMarkerStyle.Disc or TextMarkerStyle.Circle
                    or TextMarkerStyle.Box or TextMarkerStyle.Square) {
                // MdXaml's local bullet values otherwise override the application's list style.
                list.ClearValue(List.MarkerStyleProperty);
            }

            if (documentUri != null && element is Hyperlink link && link.CommandParameter is string target
                && Uri.TryCreate(target, UriKind.RelativeOrAbsolute, out var uri) && !uri.IsAbsoluteUri) {
                link.CommandParameter = new Uri(documentUri, uri).AbsoluteUri;
            }

            foreach (object child in LogicalTreeHelper.GetChildren(element)) {
                if (child is DependencyObject dependencyObject) {
                    FormatMarkdown(dependencyObject, documentUri);
                }
            }
        }

        private void UpdateDetailContent() {
            if (SettingsPresenter == null || DescriptionContent == null) {
                return;
            }

            bool showSettings = SettingsTab.IsSelected && SettingsTab.Visibility == Visibility.Visible;
            if (showSettings && SettingsPresenter.Content == null) {
                SettingsPresenter.ContentTemplate = optionsTemplate;
                SettingsPresenter.Content = settingsManifest;
            }

            DescriptionContent.Visibility = DescriptionTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
            ChangelogContent.Visibility = ChangelogTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
            changelog.SetActive(IsLoaded && IsVisible && ChangelogTab.IsSelected && ChangelogTab.Visibility == Visibility.Visible);
            ShowcaseContent.Visibility = ShowcaseTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
            showcase.SetActive(IsLoaded && IsVisible && ShowcaseTab.IsSelected);
            VersionsContent.Visibility = VersionsTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
            InfoContent.Visibility = InfoTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
            SettingsContent.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
            if (manager != null) {
                manager.IsVersionHistoryVisible = VersionsTab.IsSelected;
            }

            if (VersionsTab.IsSelected) {
                ScrollToSelectedVersion();
            }
        }

        private void OnVersionSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.Source == PluginVersions) {
                ScrollToSelectedVersion();
            }
        }

        private void ScrollToSelectedVersion() {
            if (PluginVersions?.SelectedItem != null) {
                PluginVersions.ScrollIntoView(PluginVersions.SelectedItem);
            }
        }

        private void OnFind(object sender, ExecutedRoutedEventArgs e) {
            PluginSearch.ApplyTemplate();
            if (PluginSearch.Template.FindName("PART_TextBox", PluginSearch) is TextBox editor) {
                editor.Focus();
                editor.SelectAll();
            }

            e.Handled = true;
        }

        private void OnSearchKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                PluginSearch.Clear();
                e.Handled = true;
            }
        }

        private void OnTagFilterPreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape && PluginTagFilter.IsDropDownOpen) {
                PluginTagFilter.IsDropDownOpen = false;
                e.Handled = true;
            }
        }

        protected override Size MeasureOverride(Size constraint) {
            if (PluginListColumn != null && !double.IsInfinity(constraint.Width)) {
                PluginNavigation.Measure(constraint);
                var margin = BrowserGrid.Margin;
                double reservedWidth = BrowserGrid.ColumnDefinitions[2].MinWidth
                    + BrowserGrid.ColumnDefinitions[1].Width.Value
                    + margin.Left + margin.Right + PluginNavigation.DesiredSize.Width;
                PluginListColumn.MaxWidth = Math.Clamp(constraint.Width - reservedWidth, PluginListColumn.MinWidth, maximumListWidth);
            }

            return base.MeasureOverride(constraint);
        }

    }
}
