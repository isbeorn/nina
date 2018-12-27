namespace NINA.Locale {

    public interface ILoc {
        string this[string key] { get; }

        void ReloadLocale(string culture);
    }
}