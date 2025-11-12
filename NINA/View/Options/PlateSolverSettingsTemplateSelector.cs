using NINA.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Options {
    internal class PlateSolverSettingsTemplateSelector : DataTemplateSelector {
        public DataTemplate AstrometryNetTemplate { get; set; }
        public DataTemplate LocalPlateSolverTemplate { get; set; }
        public DataTemplate Platesolve2Template { get; set; }
        public DataTemplate Platesolve3Template { get; set; }
        public DataTemplate ASTAPTemplate { get; set; }
        public DataTemplate ASPSTemplate { get; set; }
        public DataTemplate PinpointTemplate { get; set; }
        public DataTemplate TSXTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if ((item is PlateSolverEnum.ASTROMETRY_NET) || (item is BlindSolverEnum.ASTROMETRY_NET)) {
                return AstrometryNetTemplate;
            } else if ((item is PlateSolverEnum.LOCAL) || (item is BlindSolverEnum.LOCAL)) {
                return LocalPlateSolverTemplate;
            } else if (item is PlateSolverEnum.PLATESOLVE2) {
                return Platesolve2Template;
            } else if ((item is PlateSolverEnum.PLATESOLVE3) || (item is BlindSolverEnum.PLATESOLVE3)) {
                return Platesolve3Template;
            } else if ((item is PlateSolverEnum.ASTAP) || (item is BlindSolverEnum.ASTAP)) {
                return ASTAPTemplate;
            } else if ((item is PlateSolverEnum.ASPS) || (item is BlindSolverEnum.ASPS)) {
                return ASPSTemplate;
            } else if ((item is PlateSolverEnum.PINPONT) || (item is BlindSolverEnum.PINPOINT)) {
                return PinpointTemplate;
            } else if (item is PlateSolverEnum.TSX_IMAGELINK) {
                return TSXTemplate;
            } else {
                return base.SelectTemplate(item, container);
            }
        }
    }
}
