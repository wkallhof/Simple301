namespace Simple301.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Short hand extension for Is Null or White Space
        /// </summary>
        /// <param name="s">string to check if set</param>
        /// <returns>true if string is set</returns>
        public static bool IsSet(this string s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        /// <summary>
        /// Ensures that the string starts with the provided
        /// prefix
        /// </summary>
        /// <param name="s">Current string</param>
        /// <param name="prefix">Prefix</param>
        public static string EnsurePrefix(this string s, string prefix)
        {
            if (!s.IsSet()) return string.Empty;
            return s.StartsWith(prefix) ? s : prefix + s;
        }
    }
}
