
using System;
using System.Collections.Generic;

namespace Escc.ClientDependencyFramework
{
    /// <summary>
    /// Reads a media query from web.config using an alias
    /// </summary>
    public static class MediaQueryAlias
    {
        /// <summary>
        /// Resolves the specified alias.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <returns></returns>
        public static Dictionary<string, string> Resolve(string alias)
        {
            if (String.IsNullOrEmpty(alias)) return null;

            // Try reading from settings for this project, but fallback to configuration for the method we used before using the Client Dependency Framework
            var mediaQuery = ClientDependencyConfiguration.GetSetting("MediaQueries", alias) ??
                             ClientDependencyConfiguration.GetSetting("CssFiles", alias);
            if (!String.IsNullOrEmpty(mediaQuery))
            {
                return new Dictionary<string, string>
                {
                    {"media", mediaQuery}
                };
            }
            return null;
        }
    }
}
