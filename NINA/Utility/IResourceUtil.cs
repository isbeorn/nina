using System.Windows;

namespace NINA.Utility {

    public interface IResourceUtil {
        object this[string key] { get; }
    }
}