﻿

namespace Escc.ClientDependencyFramework
{
    /// <summary>
    /// Reads the path to a JavaScript file from web.config using an alias
    /// </summary>
    public static class JsFileAlias
    {
        /// <summary>
        /// Resolves the specified alias.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <returns></returns>
        public static string Resolve(string alias)
        {
            return ClientDependencyConfiguration.GetSetting("ScriptFiles", alias);
        }
    }
}
