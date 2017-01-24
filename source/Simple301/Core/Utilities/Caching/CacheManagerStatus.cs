namespace Simple301.Core.Utilities.Caching
{
    /// <summary>
    /// Represents the current status of the
    /// CacheManager service
    /// </summary>
    public class CacheManagerStatus
    {
        public bool CacheEnabled { get; set; }
        public int CacheDuration { get; set; }
    }
}
