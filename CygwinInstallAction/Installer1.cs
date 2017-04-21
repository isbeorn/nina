using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;

namespace CygwinInstallAction
{
    [RunInstaller(true)]
    public partial class Installer1: System.Configuration.Install.Installer
    {
        public Installer1()
        {
            InitializeComponent();
        }
    }
}
