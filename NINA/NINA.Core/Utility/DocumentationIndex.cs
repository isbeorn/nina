using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Utility {
    public static class DocumentationIndex {
        private static string Root_Directory = Path.Combine(CoreUtil.APPLICATIONDIRECTORY, "docs");
        private static string Advanced_Directory = Path.Combine(Root_Directory, "advanced");
        private static string Tabs_Directory = Path.Combine(Root_Directory, "tabs");
        private static string Tabs_Options_Directory = Path.Combine(Tabs_Directory, "options");

        public static Uri Tabs_Framing_General => new UriBuilder(Path.Combine(Tabs_Directory, "framing.html")).Uri;
        public static Uri Tabs_FlatWizard_General => new UriBuilder(Path.Combine(Tabs_Directory, "flatwizard.html")).Uri;
        public static Uri Advanced_MeridianFlip => new UriBuilder(Path.Combine(Advanced_Directory, "meridianflip.html")).Uri;
        public static Uri Tabs_Options_General => new UriBuilder(Path.Combine(Tabs_Options_Directory, "general.html")) { Fragment = "general" }.Uri;
        public static Uri Tabs_Options_AstrometrySettings => new UriBuilder(Path.Combine(Tabs_Options_Directory, "general.html")) { Fragment = "astrometry-settings" }.Uri;
        public static Uri Tabs_Options_Autofocus => new UriBuilder(Path.Combine(Tabs_Options_Directory, "autofocus.html")) { Fragment = "general-settings" }.Uri;
        public static Uri Tabs_Options_MeridianFlip => new UriBuilder(Path.Combine(Tabs_Options_Directory, "imaging.html")) { Fragment = "auto-meridian-flip" }.Uri;
        public static Uri Tabs_Options_FileSettings => new UriBuilder(Path.Combine(Tabs_Options_Directory, "imaging.html")) { Fragment = "file-settings" }.Uri;
        public static Uri Tabs_Options_ImageOptions => new UriBuilder(Path.Combine(Tabs_Options_Directory, "imaging.html")) { Fragment = "image-options" }.Uri;
        public static Uri Tabs_Options_Sequence => new UriBuilder(Path.Combine(Tabs_Options_Directory, "imaging.html")) { Fragment = "sequence" }.Uri;
        public static Uri Tabs_Options_Layout => new UriBuilder(Path.Combine(Tabs_Options_Directory, "imaging.html")) { Fragment = "layout" }.Uri;
        public static Uri Tabs_Options_PlateSolving => new UriBuilder(Path.Combine(Tabs_Options_Directory, "platesolving.html")) { Fragment = "plate-solving" }.Uri;
        public static Uri Tabs_Options_Dome => new UriBuilder(Path.Combine(Tabs_Options_Directory, "dome.html")) { Fragment = "mount-type" }.Uri;
    }
}
