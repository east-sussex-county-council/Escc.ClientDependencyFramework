using System;
using System.Configuration;

namespace Escc.ClientDependencyFramework
{
    public class ClientDependencySection : ClientDependency.Core.Config.ClientDependencySection
    {
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
