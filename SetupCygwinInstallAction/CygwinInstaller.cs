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

        public override void Install(IDictionary stateSaver) {
            base.Install(stateSaver);
            string input = this.Context.Parameters["INSTALLCYGWIN"];

            if(input == "1") {
                try {
                    DirectoryInfo d = new DirectoryInfo(LOCALAPPDATA);
                    if (!d.Exists) {
                        d.Create();
                    }
                    
                    var zipFile = SetupCygwinInstallAction.Properties.Resources.cygwin;
                    using (var s = new MemoryStream(zipFile)) {
                        var a = ZipFile.Read(s);
                        a.ExtractAll(LOCALAPPDATA);
                        
                    }

                } catch(Exception) {
                    
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
            } catch(Exception) {
                
            }
            
        }
    }

    
}
