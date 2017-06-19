using NINA.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NINA.Locale {
    class Loc : BaseINPC {

        private Loc() {
            locale = new ResourceDictionary { Source = new Uri(@"\Locale\Locale.xaml", UriKind.Relative) };
        }

        private static readonly Lazy<Loc> lazy =
        new Lazy<Loc>(() => new Loc());

        public static Loc Instance { get { return lazy.Value; } }
        
        ResourceDictionary locale = null;
                        
        public string this[string key] {
            get {
                return (string)locale[key];                
            }
        }
        

    }

    public class LocExtension : Binding {
        public LocExtension(string name) : base ($"[{name}]") {
            this.Mode = BindingMode.OneWay;
            this.Source = Loc.Instance;
        }
    }
}
