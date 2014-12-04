using System;
using System.Configuration;

namespace Escc.ClientDependencyFramework
{
    /// <summary>
    /// Configuration section for the Client Dependency Framework, adapted to allow an appSetting to override the version. This allows it to be updated on Azure without modifying web.config.
    /// </summary>
    public class ClientDependencySection : ClientDependency.Core.Config.ClientDependencySection
    {
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public new int Version
        {
            get
            {
                if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["ClientDependencyVersion"]))
                {
                    int appSettingsVersion;
                    if (Int32.TryParse(ConfigurationManager.AppSettings["ClientDependencyVersion"], out appSettingsVersion))
                    {
                        return appSettingsVersion;
                    }
                }
                return base.Version;
            }
            set { base.Version = value; }
        }
    }
}
