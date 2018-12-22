namespace NINA.Utility {

    public interface IResourceDictionary {
        object this[string key] { get; }
    }
}