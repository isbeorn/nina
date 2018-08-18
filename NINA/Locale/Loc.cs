using NINA.Utility;
using NINA.Utility.Mediator;
using System;
using System.Windows;
using System.Windows.Data;

namespace NINA.Locale {

    internal class Loc : BaseINPC {

        private Loc() {
        }

        public void ReloadLocale(string culture) {
            try {
                try {
                    _locale = new ResourceDictionary { Source = new Uri(@"\Locale\Locale." + culture + ".xaml", UriKind.Relative) };
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
#endif
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            RaiseAllPropertiesChanged();
        }

        private static readonly Lazy<Loc> lazy =
        new Lazy<Loc>(() => new Loc());

        public static Loc Instance { get { return lazy.Value; } }

        private ResourceDictionary _locale = null;

        public string this[string key] {
            get {
                return _locale?[key]?.ToString() ?? "MISSING LABEL " + key;
            }
        }
    }

    public class LocExtension : Binding {

        public LocExtension(string name) : base($"[{name}]") {
            this.Mode = BindingMode.OneWay;
            this.Source = Loc.Instance;
        }
    }
}