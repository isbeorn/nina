using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Utility {

    public class ResourceUtil : IResourceUtil {
        public object this[string key] => Application.Current.Resources[key];
    }
}