
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
            if (section != null)
            {
                // These prefixes set the priority when loading using Escc.ClientDependencyFramework.WebForms.
                // Here we can just ignore them because priorities are set in a different way.
                var possiblePrefixes = new [] {String.Empty, "1_", "2_", "3_", "4_", "5_", "6_", "7_", "8_", "9_"};
                foreach (var possiblePrefix in possiblePrefixes)
                {
                    var key = possiblePrefix + setting;
                    if (!String.IsNullOrEmpty(section[key]))
                    {
                        return section[key];
                    }
                }
            }
            return null;
        }
    }
}
