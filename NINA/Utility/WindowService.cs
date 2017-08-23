using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Utility
{
    /// <summary>
    /// A window should be associated to a viewmodel by the DataTemplates.xaml
    /// </summary>
    class WindowService : IWindowService {
        public void ShowWindow(object viewModel) {
            var win = new Window();
            win.Content = viewModel;
            win.Show();
        }

        public void ShowDialog(object viewModel) {
            var win = new Window();
            win.Content = viewModel;
            win.ShowDialog();
        }
    }

    interface IWindowService {
        void ShowWindow(object dataContext);
    }
}
