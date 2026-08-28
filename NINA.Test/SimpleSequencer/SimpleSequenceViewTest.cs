#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.View.SimpleSequencer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Test.SimpleSequencer {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class SimpleSequenceViewTest {
        private static bool resourcesLoaded;
        private Window ownerWindow = null!;
        private Window? previousMainWindow;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();
            previousMainWindow = Application.Current.MainWindow;
            ownerWindow = new Window {
                Width = 900,
                Height = 700,
                ShowInTaskbar = false
            };
            Application.Current.MainWindow = ownerWindow;
            ownerWindow.Show();
        }

        [TearDown]
        public void TearDown() {
            foreach (MyMessageBoxView dialog in Application.Current.Windows.OfType<MyMessageBoxView>().ToArray()) {
                dialog.Close();
            }

            ownerWindow.Content = null;
            ownerWindow.Close();
            Application.Current.MainWindow = previousMainWindow;
        }

        [Test]
        public void ResetTargetSetButton_BindsRootCommandAndDisablesWhileRunning() {
            CaptureCommand resetCommand = new CaptureCommand();
            Mock<ISequenceRootContainer> rootMock = new Mock<ISequenceRootContainer>();
            rootMock.SetupGet(x => x.ResetProgressCommand).Returns(resetCommand);
            rootMock.SetupGet(x => x.Items).Returns([
                new SequentialContainer(),
                new TargetAreaContainer(),
                new SequentialContainer()
            ]);
            TestViewModel viewModel = new TestViewModel {
                Sequencer = new NINA.Sequencer.Sequencer(rootMock.Object)
            };
            SimpleSequenceView view = ShowView(viewModel);

            Button resetButton = FindResetTargetSetButton(view);

            resetButton.IsEnabled.Should().BeTrue();
            resetButton.Command.Should().BeSameAs(resetCommand);
            resetButton.Command!.Execute(resetButton.CommandParameter);
            resetCommand.ExecutionCount.Should().Be(1);

            viewModel.IsRunning = true;
            DrainDispatcher();

            resetButton.IsEnabled.Should().BeFalse();
        }

        [Test]
        public void ResetTargetSetButton_ResetsEveryTargetOnlyAfterConfirmation() {
            Mock<ISequenceItem> firstSequence = new Mock<ISequenceItem>();
            Mock<ISequenceItem> secondSequence = new Mock<ISequenceItem>();
            SequenceRootContainer root = CreateTargetSet(firstSequence.Object, secondSequence.Object);
            TestViewModel viewModel = new TestViewModel {
                Sequencer = new NINA.Sequencer.Sequencer(root)
            };
            SimpleSequenceView view = ShowView(viewModel);
            Button resetButton = FindResetTargetSetButton(view);

            ExecuteWithConfirmation(resetButton, false);

            firstSequence.Verify(x => x.ResetProgress(), Times.Never);
            secondSequence.Verify(x => x.ResetProgress(), Times.Never);

            ExecuteWithConfirmation(resetButton, true);

            firstSequence.Verify(x => x.ResetProgress(), Times.AtLeastOnce);
            secondSequence.Verify(x => x.ResetProgress(), Times.AtLeastOnce);
        }

        private SimpleSequenceView ShowView(TestViewModel viewModel) {
            SimpleSequenceView view = new SimpleSequenceView {
                DataContext = viewModel
            };
            ownerWindow.Content = view;
            ownerWindow.UpdateLayout();
            return view;
        }

        private static SequenceRootContainer CreateTargetSet(ISequenceItem firstSequence, ISequenceItem secondSequence) {
            SequenceRootContainer root = new SequenceRootContainer();
            TargetAreaContainer targetArea = new TargetAreaContainer();
            SequentialContainer firstTarget = new SequentialContainer();
            SequentialContainer secondTarget = new SequentialContainer();
            firstTarget.Add(firstSequence);
            secondTarget.Add(secondSequence);
            targetArea.Add(firstTarget);
            targetArea.Add(secondTarget);
            root.Add(new SequentialContainer());
            root.Add(targetArea);
            root.Add(new SequentialContainer());
            return root;
        }

        private static Button FindResetTargetSetButton(DependencyObject root) {
            string tooltip = Loc.Instance["LblTooltipResetTargetSet"];
            return GetVisualDescendants(root)
                .OfType<Button>()
                .Single(button => button.ToolTip is ToolTip toolTip
                    && toolTip.Content is TextBlock textBlock
                    && textBlock.Text == tooltip);
        }

        private static IEnumerable<DependencyObject> GetVisualDescendants(DependencyObject parent) {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                yield return child;

                foreach (DependencyObject descendant in GetVisualDescendants(child)) {
                    yield return descendant;
                }
            }
        }

        private static void ExecuteWithConfirmation(Button button, bool confirm) {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                MyMessageBoxView dialog = Application.Current.Windows.OfType<MyMessageBoxView>().Single();
                dialog.DialogResult = confirm;
            }));
            button.Command!.Execute(button.CommandParameter);
        }

        private static void DrainDispatcher() {
            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static void EnsureApplicationResources() {
            if (resourcesLoaded) {
                return;
            }

            Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            string[] resourceSources = [
                "/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/SVGDictionary.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Converters.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Expander.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Button.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Path.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TextBlock.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TabControl.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ListView.xaml",
                "/NINA;component/Resources/Styles/Window.xaml"
            ];

            foreach (string resourceSource in resourceSources) {
                if (!application.Resources.MergedDictionaries.Any(x => x.Source?.OriginalString == resourceSource)) {
                    application.Resources.MergedDictionaries.Add(new ResourceDictionary {
                        Source = new Uri(resourceSource, UriKind.Relative)
                    });
                }
            }

            resourcesLoaded = true;
        }

        private sealed class TestViewModel : INotifyPropertyChanged {
            private bool isRunning;

            public required ISequencer Sequencer { get; init; }

            public bool IsRunning {
                get => isRunning;
                set {
                    if (isRunning != value) {
                        isRunning = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private sealed class CaptureCommand : ICommand {
            public int ExecutionCount { get; private set; }

            public bool CanExecute(object? parameter) {
                return true;
            }

            public void Execute(object? parameter) {
                ExecutionCount++;
            }

            public event EventHandler? CanExecuteChanged {
                add { }
                remove { }
            }
        }
    }
}
