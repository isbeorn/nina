using System.Windows;
using System.Windows.Controls;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for MeridianFlipView.xaml
    /// </summary>
    public partial class MeridianFlipView : UserControl {

        public MeridianFlipView() {
            InitializeComponent();
        }
    }

    public class MeridianFlipDataTemplateSelector : DataTemplateSelector {
        public DataTemplate RecenterTemplate { get; set; }
        public DataTemplate PassMeridianTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate FlipDataTemplate { get; set; }
        public DataTemplate SettleTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item,
                   DependencyObject container) {
            ViewModel.WorkflowStep step = item as ViewModel.WorkflowStep;
            if (step.Id == "PassMeridian") {
                return PassMeridianTemplate;
            }
            if (step.Id == "StopAutoguider") {
                return DefaultTemplate;
            }
            if (step.Id == "Flip") {
                return FlipDataTemplate;
            }
            if (step.Id == "Recenter") {
                return RecenterTemplate;
            }
            if (step.Id == "ResumeAutoguider") {
                return DefaultTemplate;
            }
            if (step.Id == "Settle") {
                return SettleTemplate;
            }

            return DefaultTemplate;
        }
    }
}