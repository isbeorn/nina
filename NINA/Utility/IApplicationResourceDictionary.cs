namespace NINA.Utility {

    public interface IApplicationResourceDictionary {
        object this[string key] { get; }
    }
}