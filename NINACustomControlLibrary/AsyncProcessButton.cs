using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NINACustomControlLibrary {
    public class AsyncProcessButton : CancellableButton {
        static AsyncProcessButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AsyncProcessButton), new FrameworkPropertyMetadata(typeof(AsyncProcessButton)));
        }

        public static readonly DependencyProperty ResumeCommandProperty =
                    DependencyProperty.Register(nameof(ResumeCommand), typeof(ICommand), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public ICommand ResumeCommand {
            get {
                return (ICommand)GetValue(ResumeCommandProperty);
            }
            set {
                SetValue(ResumeCommandProperty, value);
            }
        }

        public static readonly DependencyProperty ResumeButtonImageProperty =
           DependencyProperty.Register(nameof(ResumeButtonImage), typeof(Geometry), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public Geometry ResumeButtonImage {
            get {
                return (Geometry)GetValue(ResumeButtonImageProperty);
            }
            set {
                SetValue(ResumeButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty IsPausedProperty =
           DependencyProperty.Register(nameof(IsPaused), typeof(bool), typeof(AsyncProcessButton), new UIPropertyMetadata(false));

        public bool IsPaused {
            get {
                return (bool)GetValue(IsPausedProperty);
            }
            set {
                SetValue(IsPausedProperty, value);
            }
        }

        public static readonly DependencyProperty PauseCommandProperty =
                    DependencyProperty.Register(nameof(PauseCommand), typeof(ICommand), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public ICommand PauseCommand {
            get {
                return (ICommand)GetValue(PauseCommandProperty);
            }
            set {
                SetValue(PauseCommandProperty, value);
            }
        }

        public static readonly DependencyProperty PauseButtonImageProperty =
           DependencyProperty.Register(nameof(PauseButtonImage), typeof(Geometry), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public Geometry PauseButtonImage {
            get {
                return (Geometry)GetValue(PauseButtonImageProperty);
            }
            set {
                SetValue(PauseButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty LoadingImageBrushProperty =
           DependencyProperty.Register(nameof(LoadingImageBrush), typeof(Brush), typeof(AsyncProcessButton), new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush LoadingImageBrush {
            get {
                return (Brush)GetValue(LoadingImageBrushProperty);
            }
            set {
                SetValue(LoadingImageBrushProperty, value);
            }
        }

    }
}
