using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SetupCygwinInstallAction {

    [RunInstaller(true)]
    public partial class CygwinInstaller : System.Configuration.Install.Installer {

        public CygwinInstaller() {
            InitializeComponent();
        }

        public static string LOCALAPPDATA = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA");
        private static string CYGWIN_LOC = Path.Combine(LOCALAPPDATA, "cygwin");
        private static string CYGWIN_SETUP = Path.Combine(LOCALAPPDATA, "cygwin_setup.exe");
        private static string LOCAL_PACKAGE_DIR = Path.Combine(LOCALAPPDATA, "cygwincache");

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
#if DEBUG
                    int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                    string message = string.Format("Please attach the debugger (elevated on Vista or Win 7) to process [{0}].", processId);
                    System.Windows.Forms.MessageBox.Show(message, "Debug");
#endif
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

                    if (Directory.Exists(LOCALAPPDATA)) {
                        DirectoryInfo dInfo = new DirectoryInfo(LOCALAPPDATA);
                        SetAccessControl(dInfo);
                    }

                    RebaseCygwinDLLs();
                } catch (Exception) {
                }
            }
            base.Install(stateSaver);
        }

        private void RebaseCygwinDLLs() {
            var ashLoc = Path.Combine(CYGWIN_LOC, "bin", "ash.exe");
            if (File.Exists(ashLoc)) {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                startInfo.FileName = ashLoc;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = false;
                startInfo.CreateNoWindow = false;
                startInfo.Arguments = string.Format("-c /bin/rebaseall -v", CYGWIN_SETUP, CYGWIN_LOC, LOCAL_PACKAGE_DIR);
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                process.Close();
            }
        }

        private void SetAccessControl(DirectoryInfo dInfo) {
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            var securityidentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            dSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
            dSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            dSecurity.SetOwner(securityidentifier);
            dInfo.SetAccessControl(dSecurity);

            GrantAccessToSubFolders(dInfo);

            foreach (var file in dInfo.GetFiles("*", SearchOption.AllDirectories))
                file.Attributes &= ~FileAttributes.ReadOnly;
        }

        private void GrantAccessToSubFolders(DirectoryInfo dInfo) {
            var securityidentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            foreach (var dir in dInfo.GetDirectories("*", SearchOption.AllDirectories)) {
                DirectorySecurity dSecurity = dir.GetAccessControl();

                dSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                dSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                dir.SetAccessControl(dSecurity);
            }

            foreach (var file in dInfo.GetFiles("*", SearchOption.AllDirectories)) {
                var fileSecurity = File.GetAccessControl(file.FullName);
                fileSecurity.AddAccessRule(new FileSystemAccessRule(securityidentifier, FileSystemRights.FullControl, AccessControlType.Allow));
                File.SetAccessControl(file.FullName, fileSecurity);
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
                    if (dir.FullName == CYGWIN_LOC) {
                        if (System.Windows.Forms.MessageBox.Show("Local Astrometry.Net Client detected. Uninstall?", "", System.Windows.Forms.MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes) {
                            continue;
                        }
                    }
                    dir.Delete(true);
                }
                di.Delete();
            } catch (Exception) {
            }
        }
    }
}