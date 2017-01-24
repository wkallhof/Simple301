namespace Simple301.Core.Utilities.Caching
{
    /// <summary>
    /// Represents a single cached item.
    /// Category is used for organizing
    /// </summary>
    public class CacheItem
    {
        public string Category { get; set; }
        public string Key { get; set; }
    }
}
