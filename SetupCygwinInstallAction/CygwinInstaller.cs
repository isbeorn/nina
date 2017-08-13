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
using System.Security.AccessControl;
using System.Security.Principal;

namespace SetupCygwinInstallAction {
    [RunInstaller(true)]
    public partial class CygwinInstaller : System.Configuration.Install.Installer {
        public CygwinInstaller() {
            InitializeComponent();
        }


        static string LOCALAPPDATA = Environment.GetEnvironmentVariable("LocalAppData") + "\\NINA";
        static string CYGWIN_LOC = LOCALAPPDATA + "\\cygwin";
        static string CYGWIN_SETUP = LOCALAPPDATA + "\\cygwin_setup.exe";
        static string LOCAL_PACKAGE_DIR = LOCALAPPDATA + "\\cygwincache";

        public override void Install(IDictionary stateSaver) {
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
                    startInfo.FileName = CYGWIN_SETUP;
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = false;
                    startInfo.CreateNoWindow = false;
                    startInfo.Arguments = string.Format("-P astrometry.net -K http://astrotortilla.kuntsi.com/tortilla.gpg -s http://astrotortilla.kuntsi.com  -R {1}  -l {2} -O -q", CYGWIN_SETUP, CYGWIN_LOC, LOCAL_PACKAGE_DIR);
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    process.Close();
#if DEBUG
                    int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                    string message = string.Format("Please attach the debugger (elevated on Vista or Win 7) to process [{0}].", processId);
                    System.Windows.Forms.MessageBox.Show(message, "Debug");
#endif
                    if (Directory.Exists(LOCALAPPDATA)) {
                        DirectoryInfo dInfo = new DirectoryInfo(LOCALAPPDATA);
                        DirectorySecurity dSecurity = dInfo.GetAccessControl();
                        var securityidentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);                        
                        dSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                        dSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                        dInfo.SetAccessControl(dSecurity);

                        GrantAccessToSubFolders(dInfo);

                        foreach (var file in dInfo.GetFiles("*", SearchOption.AllDirectories))
                            file.Attributes &= ~FileAttributes.ReadOnly;
                    }
                } catch (Exception) {

                }
            }
            base.Install(stateSaver);
        }

        private void GrantAccessToSubFolders(DirectoryInfo dInfo) {
            foreach (var dir in dInfo.GetDirectories("*", SearchOption.AllDirectories)) {
                DirectorySecurity dSecurity = dir.GetAccessControl();
                var securityidentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                dSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                dSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                dir.SetAccessControl(dSecurity);                
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
