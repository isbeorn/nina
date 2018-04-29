using System;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Utility {

    public class ButtonHelper {

        // Boilerplate code to register attached property "bool? DialogResult"
        public static bool? GetDialogResult(DependencyObject obj) { return (bool?)obj.GetValue(DialogResultProperty); }

        public static void SetDialogResult(DependencyObject obj, bool? value) { obj.SetValue(DialogResultProperty, value); }

        public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(ButtonHelper), new UIPropertyMetadata {
            PropertyChangedCallback = (obj, e) => {
                // Implementation of DialogResult functionality
                Button button = obj as Button;
                if (button == null)
                    throw new InvalidOperationException(
                      "Can only use ButtonHelper.DialogResult on a Button control");
                button.Click += (sender, e2) => {
                    Window.GetWindow(button).DialogResult = GetDialogResult(button);
                    Window.GetWindow(button).Close();
                };
            }
        });
    }
}