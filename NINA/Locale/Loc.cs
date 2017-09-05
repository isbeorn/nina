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


            ReloadLocale();


        }

        public void ReloadLocale() {            
            try {
                _locale = new ResourceDictionary { Source = new Uri(@"\Locale\Locale." + Settings.Language.Name + ".xaml", UriKind.Relative) };
            } catch (System.IO.IOException) {
                // Fallback to default locale if setting is invalid
                _locale = new ResourceDictionary { Source = new Uri(@"\Locale\Locale.xaml", UriKind.Relative) };
            }
#if DEBUG
            var tmp = new ResourceDictionary();
            foreach (System.Collections.DictionaryEntry l in _locale) {
                tmp.Add(l.Key, "##" + l.Value + "##");
            }
            _locale = tmp;
            RaiseAllPropertiesChanged();

            Mediator.Instance.Notify(MediatorMessages.LocaleChanged, null);
#endif
        }

        private static readonly Lazy<Loc> lazy =
        new Lazy<Loc>(() => new Loc());

        public static Loc Instance { get { return lazy.Value; } }
        
        ResourceDictionary _locale = null;
                        
        public string this[string key] {
            get {
                return _locale[key]?.ToString() ?? "MISSING LABEL " + key ;                
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
