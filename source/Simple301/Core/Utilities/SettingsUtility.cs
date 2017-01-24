using Simple301.Core.Extensions;
using System;
using System.Configuration;
using System.Linq;

namespace Simple301.Core.Utilities
{
    /// <summary>
    /// Utility to manage the reading of application
    /// settings.
    /// </summary>
    public class SettingsUtility
    {
        /// <summary>
        /// Reads the web.config application settings for the 
        /// given settings name
        /// </summary>
        /// <param name="name">Lookup name for value</param>
        /// <returns>string</returns>
        public virtual string GetAppSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            return value ?? string.Empty;
        }

        /// <summary>
        /// Looks for the existence of the app setting 
        /// based on the provided key
        /// </summary>
        /// <param name="key">Key to check if exists</param>
        /// <returns>True if exists, False if not</returns>
        public virtual bool AppSettingExists(string key)
        {
            return ConfigurationManager.AppSettings.AllKeys.Contains(key);
        }

        /// <summary>
        /// Reads the web.config AppSettings for the given setting and returns
        /// as the given type
        /// </summary>
        /// <typeparam name="T">Type to return as</typeparam>
        /// <param name="key">Key for value lookup</param>
        public virtual T GetAppSetting<T>(string key) where T : IConvertible
        {
            var value = this.GetAppSetting(key);
            if (!value.IsSet()) return default(T);
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
