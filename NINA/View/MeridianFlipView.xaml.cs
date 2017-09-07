using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View
{
    /// <summary>
    /// Interaction logic for MeridianFlipView.xaml
    /// </summary>
    public partial class MeridianFlipView : UserControl
    {
        public MeridianFlipView()
        {
            InitializeComponent();
        }
    }

    public class MeridianFlipDataTemplateSelector : DataTemplateSelector {
        public DataTemplate RecenterTemplate { get; set; }
        public DataTemplate PassMeridianTemplate { get; set; }
        public DataTemplate EnumDataTemplate { get; set; }
        public DataTemplate FlipDataTemplate { get; set; }
        public DataTemplate SettleTemplate { get; set; }
        

        public override DataTemplate SelectTemplate(object item,
                   DependencyObject container) {
            ViewModel.WorkflowStep step = item as ViewModel.WorkflowStep;
            if (step.Id == "PassMeridian") {
                return PassMeridianTemplate;
            }
            if (step.Id == "StopAutoguider") {
                return EnumDataTemplate;
            }
            if (step.Id == "Flip") {
                return FlipDataTemplate;
            }
            if (step.Id == "Recenter") {
                return RecenterTemplate;
            }
            if (step.Id == "ResumeAutoguider") {
                return EnumDataTemplate;
            }
            if (step.Id == "Settle") {
                return SettleTemplate;
            }

            return RecenterTemplate;
        }
    }
}
