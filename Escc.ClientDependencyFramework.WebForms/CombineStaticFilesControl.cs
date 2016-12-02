using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web.UI;
using Exceptionless;

namespace Escc.ClientDependencyFramework.WebForms
{
    /// <summary>
    /// Base class for including a combined and cached set of static files in a web page
    /// </summary>
    /// <remarks>Derived classes should be implemented for each type of static file. See <see cref="CombineStaticFilesHandler"/> for details.</remarks>
    public abstract class CombineStaticFilesControl : Control
    {
        #region Properties for lists of keys and attributes
        private List<string> fileList = new List<string>();
        private Dictionary<string, string> attributes = new Dictionary<string, string>();
        private string attributesHtml;
        private string handlerPath = String.Empty;
        private string handlerPlaceholder = String.Empty;
        private NameValueCollection config;
        private string configurationSection;
        private string tagPattern;
        private bool mergeWithSimilar = true;
        private bool moveable = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombineStaticFilesControl"/> class.
        /// </summary>
        public CombineStaticFilesControl()
        {
            // Not only does ViewState have nothing to offer for this control, it can cause the control to be hidden when used on a CMS page and "submit" is clicked
            this.EnableViewState = false;
        }

        /// <summary>
        /// Gets the list of web.config keys identifying files to include
        /// </summary>
        /// <value>The file list.</value>
        public IList<string> FileList { get { return this.fileList; } }

        /// <summary>
        /// Gets or sets a semi-colon-separated list of web.config keys identifying files to include.
        /// </summary>
        /// <value>The files.</value>
        public string Files
        {
            get
            {
                var fileArray = new string[this.fileList.Count];
                fileList.CopyTo(fileArray);
                return String.Join(";", fileArray);
            }
            set
            {
                this.fileList.Clear();
                if (!String.IsNullOrEmpty(value)) this.fileList.AddRange(value.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        /// <summary>
        /// Gets attributes which may be added to the rendered element
        /// </summary>
        /// <value>The attributes.</value>
        public IDictionary<string, string> Attributes { get { return this.attributes; } }

        /// <summary>
        /// Gets attributes which may be added to the rendered element, serialised as an HTML string.
        /// </summary>
        /// <value>The attributes as HTML.</value>
        private string AttributesHtml
        {
            get
            {
                if (this.attributesHtml == null)
                {
                    this.attributesHtml = String.Empty;
                    foreach (string key in this.attributes.Keys)
                    {
                        this.attributesHtml += " " + key + "=\"" + this.attributes[key] + "\"";
                    }
                }
                return this.attributesHtml;
            }
        }

        /// <summary>
        /// Gets or sets whether to merge this resource with similar resources.
        /// </summary>
        /// <value><c>true</c> to merge; otherwise, <c>false</c>.</value>
        public bool MergeWithSimilar
        {
            get { return this.mergeWithSimilar; }
            set { this.mergeWithSimilar = value; }
        }

        /// <summary>
        /// Gets or sets whether this control can be moved from its current location.
        /// </summary>
        /// <value><c>true</c> to move; otherwise, <c>false</c>.</value>
        public bool Moveable
        {
            get { return this.moveable; }
            set { this.moveable = value; }
        }

        #endregion // Properties for lists of keys and attributes

        #region Build the tag(s)
        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected abstract override void CreateChildControls();

        /// <summary>
        /// Adds the tag(s) to the page.
        /// </summary>
        /// <param name="configurationSection">The name of the configuration section containing the keys and filenames.</param>
        /// <param name="preferHeadSection">if set to <c>true</c> and no placeholder set in web.config, put any tags in the page &lt;head&gt;&lt;/head&gt; section.</param>
        /// <param name="tagPattern">An empty HTML tag with {0} where the filename should go, and {1} where any additional attributes should go.</param>
        protected void BuildCombinedTag(string configurationSection, bool preferHeadSection, string tagPattern)
        {
            // Stash some parameters for use in PreRender
            this.configurationSection = configurationSection;
            this.tagPattern = tagPattern;

            try
            {
                // Stop straight away if the user is not in the correct group to receive this file
                if (!this.PermissionToShow()) return;

                // Validate parameters
                if (String.IsNullOrEmpty(configurationSection)) throw new ArgumentNullException("configurationSection");
                if (String.IsNullOrEmpty(tagPattern)) throw new ArgumentNullException("tagPattern");

                // The settings should be in web.config. Fall back to the old section name if required.
                this.config = ConfigurationManager.GetSection("Escc.ClientDependencyFramework/" + configurationSection) as NameValueCollection;
                if (this.config == null) this.config = ConfigurationManager.GetSection("EsccWebTeam.Egms/" + configurationSection) as NameValueCollection;

                // Start with the assumption that this control will render its own HTML
                this.destinationControl = this;

                if (this.config != null)
                {
                    // Allow separate handler path for secure access, because the non-secure one might use a cookieless domain that has no certificate
                    if (this.Context.Request.Url.Scheme.ToUpperInvariant() == "HTTPS" && !String.IsNullOrEmpty(this.config["HttpsHandlerPath"]))
                    {
                        this.handlerPath = this.config["HttpsHandlerPath"];
                    }

                    // If not secure or there's no secure handler, use the standard handler
                    else if (!String.IsNullOrEmpty(this.config["HandlerPath"]))
                    {
                        this.handlerPath = this.config["HandlerPath"];

                        // Expand protocol relative URL into absolute URL because IE7/8 will download CSS twice if it's loaded with a protocol relative URL
                        // http://www.stevesouders.com/blog/2010/02/10/5a-missing-schema-double-download/
                        if (this.handlerPath.StartsWith("//", StringComparison.Ordinal)) this.handlerPath = this.Context.Request.Url.Scheme + ":" + this.handlerPath;
                    }
                    if (Moveable && !String.IsNullOrEmpty(this.config["HandlerPlaceholder"])) this.handlerPlaceholder = this.config["HandlerPlaceholder"];
                }

                if (this.fileList.Count > 0)
                {
                    // Try to find another instance of this control and append the keys to that, to ensure as few as possible are loaded

                    // 1. If we we can get the page head, optionally try there.
                    //    Do this before looking in the preferred placeholder, because then the destinationControl 
                    //    ends up set as the placeholder, not the page header
                    if (Moveable && preferHeadSection && this.Visible && Page.Header != null)
                    {
                        this.destinationControl = Page.Header;
                        foreach (Control control in this.destinationControl.Controls)
                        {
                            if (LookForOtherInstances(control)) break;
                        }
                    }

                    // 2. If web.config has been set to put all files in a particular placeholder, try to put it there
                    if (!String.IsNullOrEmpty(this.handlerPlaceholder) && Page.Master != null && this.Visible)
                    {
                        // Look in the master page, and if it's not there support one level of nested master pages
                        var masterPagePlaceholder = Page.Master.FindControl(this.handlerPlaceholder);
                        if (masterPagePlaceholder == null && Page.Master.Master != null) masterPagePlaceholder = Page.Master.Master.FindControl(this.handlerPlaceholder);

                        if (masterPagePlaceholder != null)
                        {
                            this.destinationControl = masterPagePlaceholder;
                            foreach (Control control in this.destinationControl.Controls)
                            {
                                if (LookForOtherInstances(control)) break;
                            }
                        }
                    }

                    // 3. If still not found another instance, try looking through the whole page
                    if (this.Visible)
                    {
                        foreach (Control control in Page.Controls)
                        {
                            if (LookForOtherInstances(control)) break;
                        }
                    }
                }

                // And finally add this control to the destinationControl, but later in the page lifecycle
                // Do this even if this control has no files, because it may have files added to it by others combining with this one
                this.Page.PreRenderComplete += new EventHandler(Page_PreRenderComplete);
            }
            catch (Exception ex)
            {
                ex.Data.Add("File keys", this.Files);
                ex.ToExceptionless().Submit();
                throw;
            }
        }


        /// <summary>
        /// Avoids the mixed content warning that happens when an HTTPS page includes a static file over HTTP
        /// </summary>
        /// <param name="url">The static file URL.</param>
        /// <returns></returns>
        private string AvoidMixedContentWarning(string url)
        {
            if (this.Context.Request.Url.Scheme == Uri.UriSchemeHttps && url.StartsWith(Uri.UriSchemeHttp + ":", StringComparison.OrdinalIgnoreCase))
            {
                url = Uri.UriSchemeHttps + url.Substring(4);
            }
            return url;
        }

        /// <summary>
        /// Looks for other instances of the same control type, so that we end up with just one instance per page.
        /// </summary>
        /// <param name="control">The control to check.</param>
        /// <returns></returns>
        private bool LookForOtherInstances(Control control)
        {
            if (!this.Moveable) return false; // to make this stay still, other controls have to find this one, not vice versa
            if (!this.MergeWithSimilar) return false; // don't want to merge so just pretend others don't exist

            // Is this control another instance we can pass the keys to?
            var currentControlType = this.GetType();
            if (control.GetType() == currentControlType && control.Visible && control != this)
            {
                // Attributes must match for the controls to be combined
                var otherControl = (CombineStaticFilesControl)control;
                if (otherControl.AttributesHtml == this.AttributesHtml && otherControl.MergeWithSimilar)
                {
                    foreach (string file in this.fileList)
                    {
                        otherControl.FileList.Add(file);
                    }
                    this.Visible = false;
                    return true;
                }
            }

            // Are any of the child controls other instances we can pass the keys to?
            foreach (Control child in control.Controls)
            {
                if (LookForOtherInstances(child)) return true;
            }

            return false;
        }

        private Control destinationControl;


        /// <summary>
        /// Handles the PreRenderComplete event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// <para>If you add a CombineStaticFilesControl to the Controls collection of Page.Header which then tries to add its own
        /// controls to the same collection, it will fail with the error message: "The control collection cannot be modified during 
        /// DataBind, Init, Load, PreRender or Unload phases." Adding controls at this point in the life-cycle avoids the error.</para>
        /// <para>Generating the HTML at this point in the life-cycle also means there's a chance for other controls later in the page
        /// to combine with this one <i>before</i> this one determines which files to render.</para>
        /// </remarks>
        void Page_PreRenderComplete(object sender, EventArgs e)
        {
            // If this control is still visible, no alternative control was found
            if (this.Visible)
            {
                // Sort the list of keys, so that files are always referenced in the same order. Means the filename is more likely to
                // have been cached, and for CSS the cascade behaves predictably too.
                this.fileList.Sort();

                // Remove any duplicates which might've come from combining with other controls. Again means filename is more likely to
                // have been cached, and prevents unnecessary load of two copies of a file.
                var uniqueList = new List<string>();
                foreach (string key in fileList)
                {
                    if (!uniqueList.Contains(key)) uniqueList.Add(key);
                }
                this.fileList = uniqueList;

                // If the handler has been configured, build a path for it to handle
                if (!String.IsNullOrEmpty(handlerPath))
                {
                    if (this.fileList.Count > 0)
                    {
                        // Convert the list of file keys into a format suitable for use as a URL
                        var keys = Regex.Replace(this.Files.Replace(";", "-").ToLowerInvariant(), "[^a-z0-9-]", String.Empty);

                        // If we don't want to cache the result (because it's in development) add a number that changes every time
                        string nocache = "nocache" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);

                        // Wrap that in a URL
                        keys = String.Format(CultureInfo.InvariantCulture, handlerPath, keys, nocache);

                        // Ensure SSL requests also load static files using SSL
                        keys = AvoidMixedContentWarning(keys);

                        // Wrap that URL in an appropriate tag and put it in the best available location
                        string tag = String.Format(CultureInfo.InvariantCulture, tagPattern + Environment.NewLine, keys, AttributesHtml);
                        destinationControl.Controls.Add(new LiteralControl(FilterHtml(tag)));
                    }
                }
                else if (this.config != null)
                {
                    // Otherwise just fall back to linking to the file

                    // Allow keys to be preceded by a number between 1 and 9 followed by an underscore. This allows prioritisation of some files so that,
                    // for example, a script library will always be loaded first. If this pattern is not used and the key is an exact match, treat it like 
                    // priority 5, or normal priority.
                    for (var i = 1; i <= 9; i++)
                    {
                        foreach (string key in fileList)
                        {
                            string prioritisedKey;
                            if (i == 5)
                            {
                                // Is the key present without a priority?
                                prioritisedKey = key;
                                if (config[prioritisedKey] == null)
                                {
                                    // No? Then how about with a priority of 5?
                                    prioritisedKey = i.ToString(CultureInfo.InvariantCulture) + "_" + key;
                                    if (String.IsNullOrEmpty(config[prioritisedKey]))
                                    {
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                // Is the key present with the current priority?
                                prioritisedKey = i.ToString(CultureInfo.InvariantCulture) + "_" + key;
                                if (String.IsNullOrEmpty(config[prioritisedKey]))
                                {
                                    continue;
                                }
                            }

                            string tag = String.Format(CultureInfo.InvariantCulture, tagPattern + Environment.NewLine, AvoidMixedContentWarning(config[prioritisedKey]), AttributesHtml);
                            destinationControl.Controls.Add(new LiteralControl(FilterHtml(tag)));
                        }
                    }
                }
                else
                {
                    // web.config not set up so this control won't work
                    throw new ConfigurationErrorsException("The " + this.configurationSection + " section was not found in web.config");
                }
            }
        }

        /// <summary>
        /// When overridden in a child class, allows the HTML to be modified before output
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns></returns>
        protected virtual string FilterHtml(string html)
        {
            return html;
        }

        #endregion // Build the tag(s)

        #region Permission checks

        /// <summary>
        /// Gets or sets the AD groups to load this CSS file for
        /// </summary>
        /// <value>AD groups names separated by semi-colons</value>
        public string RestrictToGroups { get; set; }

        /// <summary>
        /// Check whether the current user is in the AD group(s) this CSS file is restricted to, if it is restricted
        /// </summary>
        /// <returns></returns>
        private bool PermissionToShow()
        {
            if (String.IsNullOrEmpty(this.RestrictToGroups)) return true;

            try
            {
                // Get a list of the current user's groups. Can't use WindowsPrincipal.IsIsRole() because it doesn't
                // work when web.config is set to use forms authentication. This does work.
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                IdentityReferenceCollection groups = identity.Groups.Translate(typeof(NTAccount));
                List<string> userRoles = new List<string>();
                foreach (IdentityReference group in groups) userRoles.Add(group.Value);

                // See whether any one of the allowed groups is in the user's list of groups
                string[] allowedGroups = this.RestrictToGroups.Split(';');
                foreach (string groupName in allowedGroups)
                {
                    if (userRoles.Contains(groupName.Trim())) return true;
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                return false;
            }
            return false;
        }

        #endregion // Permission checks
    }
}
