
using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Escc.ClientDependencyFramework
{
    /// <summary>
    /// Reads paths and media queries from web.config identified by their aliases
    /// </summary>
    internal static class ClientDependencyConfiguration
    {
        /// <summary>
        /// Gets the setting from web.config.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="setting">The setting.</param>
        /// <returns></returns>
        internal static string GetSetting(string sectionName, string setting)
        {
            // Try reading from settings for this project, but fallback to configuration for the method we used before using the Client Dependency Framework
            var section = (ConfigurationManager.GetSection("Escc.ClientDependencyFramework/" + sectionName) ??
                           ConfigurationManager.GetSection("EsccWebTeam.Egms/" + sectionName))
                           as NameValueCollection;
            if (section != null && !String.IsNullOrEmpty(section[setting]))
            {
                return section[setting];
            }
            return null;
        }
    }
}
