using System.Windows;

namespace NINA.Utility {
    public class ResourceDictionary : IResourceDictionary {
        public object this[string key] => Application.Current.Resources[key];
    }
}