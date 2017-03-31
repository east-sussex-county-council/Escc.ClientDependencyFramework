using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Escc.ClientDependencyFramework.WebForms
{
    /// <summary>
    /// Include a combined and cached set of CSS files in a web page
    /// </summary>
    /// <remarks>See <see cref="CombineStaticFilesHandler"/> for details.</remarks>
    public class Css : CombineStaticFilesControl
    {
        /// <summary>
        /// Gets or sets the media query to apply as an attribute of the &lt;link /&gt; element
        /// </summary>
        /// <value>The media query.</value>
        public string Media
        {
            get
            {
                return this.Attributes.ContainsKey("media") ? this.Attributes["media"] : string.Empty;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    if (this.Attributes.ContainsKey("media")) this.Attributes.Remove("media");
                }
                else
                {
                    this.Attributes["media"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the configuration setting in &lt;EsccWebTeam.Egms&gt;&lt;CssFiles /&gt;&lt;/EsccWebTeam.Egms&gt; which has the media query for this instance.
        /// </summary>
        /// <value>The media configuration.</value>
        public string MediaConfiguration
        {
            get
            {
                // Prefer the new config section, but fall back to the old one
                var config = ConfigurationManager.GetSection("Escc.ClientDependencyFramework/MediaQueries") as NameValueCollection;
                if (config == null) config = ConfigurationManager.GetSection("EsccWebTeam.Egms/CssFiles") as NameValueCollection;
                if (config == null) throw new ConfigurationErrorsException("Configuration section not found <Escc.ClientDependencyFramework><MediaQueries /></Escc.ClientDependencyFramework>");

                string m = this.Media;
                foreach (string key in config.Keys) if (config[key] == m) return key;
                return String.Empty;
            }
            set
            {
                var config = ConfigurationManager.GetSection("Escc.ClientDependencyFramework/MediaQueries") as NameValueCollection;
                if (config == null) config = ConfigurationManager.GetSection("EsccWebTeam.Egms/CssFiles") as NameValueCollection;
                if (config == null) throw new ConfigurationErrorsException("Configuration section not found <Escc.ClientDependencyFramework><MediaQueries /></Escc.ClientDependencyFramework>");
                if (String.IsNullOrEmpty(config[value])) throw new ConfigurationErrorsException(String.Format("{0} setting not found in <Escc.ClientDependencyFramework><MediaQueries /></Escc.ClientDependencyFramework>", value));

                this.Media = config[value];
            }
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            this.BuildCombinedTag("CssFiles", true, "<link rel=\"stylesheet\" type=\"text/css\" href=\"{0}\"{1} />");
        }
    }
}
