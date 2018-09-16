using NINA.Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for OptionsImagingView.xaml
    /// </summary>
    public partial class OptionsImagingView : UserControl {

        public OptionsImagingView() {
            InitializeComponent();
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e) {
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e) {
            var tb = sender as TextBox;
            string tstring;
            tstring = e.Data.GetData(DataFormats.StringFormat).ToString();
            tb.Text += tstring;
        }

        private void TextBox_DragEnter(object sender, DragEventArgs e) {
            e.Effects = DragDropEffects.Copy;
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var item = sender as ListViewItem;
            if (item != null) {
                ImagePattern mySelectedItem = item.Content as ImagePattern;
                if (mySelectedItem != null) {
                    DragDrop.DoDragDrop(ImagePatternList, mySelectedItem.Key, DragDropEffects.Copy);
                }
            }
        }

        private void ListViewItem_PreviewMouseDoubleClick(object sender, RoutedEventArgs e) {
            var item = sender as ListViewItem;
            if (item != null) {
                ImagePattern mySelectedItem = item.Content as ImagePattern;
                if (mySelectedItem != null) {
                    ImageFilePatternTextBox.Text += mySelectedItem.Key;
                }
            }
        }
    }
}