using System.Windows;

namespace NINA.Utility {

    public class ApplicationResourceDictionary : IApplicationResourceDictionary {
        public object this[string key] => Application.Current.Resources[key];
    }
}