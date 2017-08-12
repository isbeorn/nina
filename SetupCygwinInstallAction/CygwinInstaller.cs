using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using Ionic.Zip;
using System.IO;

namespace SetupCygwinInstallAction {
    [RunInstaller(true)]
    public partial class CygwinInstaller : System.Configuration.Install.Installer {
        public CygwinInstaller() {
            InitializeComponent();
        }


        static string LOCALAPPDATA = Environment.GetEnvironmentVariable("LocalAppData") + "\\NINA";
        static string CYGWIN_LOC = LOCALAPPDATA + "\\cygwin";
        static string CYGWIN_SETUP = LOCALAPPDATA + "\\cygwin_setup.exe";

        public override void Install(IDictionary stateSaver) {
            base.Install(stateSaver);
            string input = this.Context.Parameters["INSTALLCYGWIN"];

            if (input == "1") {
                try {
                    DirectoryInfo d = new DirectoryInfo(LOCALAPPDATA);
                    if (!d.Exists) {
                        d.Create();
                    }


                    var setup = SetupCygwinInstallAction.Properties.Resources.setup;
                    File.WriteAllBytes(CYGWIN_SETUP, setup);

                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    startInfo.FileName = "cmd.exe";
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.CreateNoWindow = false;
                    startInfo.Arguments = string.Format("/C {0} -P astrometry.net -K http://astrotortilla.kuntsi.com/tortilla.gpg -s http://astrotortilla.kuntsi.com  -R {1}  -l {2} -O -q", CYGWIN_SETUP, CYGWIN_LOC, LOCALAPPDATA);
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    process.Close();

                } catch (Exception) {

                }
            }

        }

        public override void Rollback(IDictionary savedState) {
            base.Rollback(savedState);

            try {
                System.IO.DirectoryInfo di = new DirectoryInfo(LOCALAPPDATA);

                foreach (FileInfo file in di.GetFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories()) {
                    dir.Delete(true);
                }
                di.Delete();
            } catch (Exception) {

            }
        }

        public override void Uninstall(IDictionary savedState) {
            base.Uninstall(savedState);

            try {
                System.IO.DirectoryInfo di = new DirectoryInfo(LOCALAPPDATA);

                foreach (FileInfo file in di.GetFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories()) {
                    dir.Delete(true);
                }
                di.Delete();
            } catch (Exception) {

            }

        }
    }


}
