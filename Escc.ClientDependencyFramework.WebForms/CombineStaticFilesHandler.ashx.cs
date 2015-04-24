using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Exceptionless;

namespace Escc.ClientDependencyFramework.WebForms
{
    /// <summary>
    /// Include a combined and cached set of CSS or JavaScript files in the page
    /// </summary>
    /// <remarks>
    /// <para>Combining static files into fewer HTTP requests and caching the result means static files are served faster.</para>
    /// <para>Start by registering your CSS and JavaScript files in <c>web.config</c>. This is so that you can easily reorganise where they are
    /// stored on your site without having to update references in each web page, and to use different paths on your live site (for example,
    /// a minified version). If you're not using this HTTP handler, it also allows you to version your files by changing the filename.</para>
    /// <example><code>
    /// &lt;configuration&gt;
    ///   &lt;configSections&gt;
    ///     &lt;sectionGroup name=&quot;EsccWebTeam.Egms&quot;&gt;
    ///       &lt;section name=&quot;CssFiles&quot; type=&quot;System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089&quot; /&gt;
    ///       &lt;section name=&quot;ScriptFiles&quot; type=&quot;System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089&quot; /&gt;
    ///     &lt;/sectionGroup&gt;
    ///   &lt;/configSections&gt;
    ///
    ///   &lt;EsccWebTeam.Egms&gt;
    ///     &lt;CssFiles&gt;
    ///       &lt;add key=&quot;ExampleStyle&quot; value=&quot;/example/style.css&quot; /&gt;
    ///     &lt;/CssFiles&gt;
    ///
    ///     &lt;ScriptFiles&gt;
    ///       &lt;add key=&quot;ExampleScript&quot; value=&quot;/example/script.js&quot; /&gt;
    ///       &lt;add key=&quot;ExampleScript2&quot; value=&quot;/example/script2.js&quot; /&gt;
    ///     &lt;/ScriptFiles&gt;
    ///   &lt;/EsccWebTeam.Egms&gt;
    /// &lt;/configuration&gt;
    /// </code></example>
    /// <para>You also need to register the control namespace so that you can put the control on the page.</para>
    /// <example><code>
    /// &lt;configuration&gt;
    ///  	&lt;system.web&gt;
    ///  	  &lt;pages&gt;
    ///  	    &lt;controls&gt;
    ///  	      &lt;add tagPrefix=&quot;Egms&quot; namespace=&quot;EsccWebTeam.Egms&quot; assembly=&quot;EsccWebTeam.Egms, Version=1.0.0.0, Culture=neutral, PublicKeyToken=06fad7304560ae6f&quot; /&gt;
    ///  	    &lt;/controls&gt;
    ///  	  &lt;/pages&gt;
    ///  	&lt;/system.web&gt;
    /// &lt;/configuration&gt;
    /// </code></example>
    /// <para>Now you can use the <see cref="Css"/> and <see cref="Script"/> controls on your page. You can reference multiple files with one tag by separating the keys with semi-colons.
    /// You can also restrict the file(s) to only certain users using the <see cref="CombineStaticFilesControl.RestrictToGroups"/> property.</para> 
    /// <example><code>
    /// &lt;Egms:Css runat=&quot;server&quot; Files=&quot;ExampleStyle&quot; /&gt;
    /// &lt;Egms:Script runat=&quot;server&quot; Files=&quot;ExampleScript;ExampleScript2&quot; /&gt;
    /// </code></example>
    /// <para>These controls will output ordinary &lt;link /&gt; and &lt;script&gt;&lt;/script&gt; elements. CSS files will be linked from the page &lt;head&gt;&lt;/head&gt; so
    /// long as the &lt;head&gt;&lt;/head&gt; element has <c>runat=&quot;server&quot;</c> applied.</para>
    /// <para><b>Configuring the HTTP handler</b></para>
    /// <para>To take advantage of combining and caching static files, you need to configure the HTTP handler.</para>
    /// <b>In IIS 6:</b>
    /// <list type="bullet">
    /// <item>Go to the application root from where you want to serve static files. This will typically be the root of the website. Right-click and select <c>Properties</c>.</item>
    /// <item>On the <c>Home directory</c> tab, click the <c>Configuration</c> button.</item>
    /// <item>Open the entry for any ASP.NET file type and copy the path to the ISAPI filter.</item>
    /// <item>Click <c>Add</c> to create a new mapping, and paste the ISAPI path into the <c>Executable</c> field.</item>
    /// <item>Enter a file extension, which must include the string <c>&quot;css&quot;</c> or <c>&quot;js&quot;</c> to be recognised. If you want all
    /// CSS or JavaScript files to be processed by ASP.NET you can use the standard <c>css</c> or <c>js</c></item> extensions; 
    /// otherwise use a custom extension such as <c>cssx</c> or <c>jsx</c>.
    /// <item>Set <c>Limit to</c> to <c>GET</c>, select <c>Script engine</c> and deselect <c>Verify that file exists</c>. Click <c>OK</c>.</item>
    /// </list>
    /// <b>In <c>web.config</c> when using IIS6:</b>
    /// <para>Configure the HTTP handler, and in the path specify the same extensions you used in IIS:</para>
    /// <example><code>
    /// &lt;configuration&gt;
    ///  	&lt;system.web&gt;
    ///         &lt;httpHandlers&gt;
    ///             &lt;add verb=&quot;GET&quot; path=&quot;*.cssx&quot; type=&quot;EsccWebTeam.Egms.CombineStaticFilesHandler,EsccWebTeam.Egms, Version=1.0.0.0, Culture=neutral, PublicKeyToken=06fad7304560ae6f&quot;/&gt;
    ///             &lt;add verb=&quot;GET&quot; path=&quot;*.jsx&quot; type=&quot;EsccWebTeam.Egms.CombineStaticFilesHandler,EsccWebTeam.Egms, Version=1.0.0.0, Culture=neutral, PublicKeyToken=06fad7304560ae6f&quot;/&gt;
    ///         &lt;/httpHandlers&gt;
    ///  	&lt;/system.web&gt;
    /// &lt;/configuration&gt;
    /// </code></example>
    /// <b>In IIS 7 (method 1):</b>
    /// <list type="bullet">
    /// <item>Go to the application root from where you want to serve static files. This will typically be the root of the website. Under the features view double click on the <c>Handler mappings</c> icon</item>
    /// <item>Open the entry for any ASP.NET file type and copy the path to the ISAPI filter. For example you would copy PageHandlerFactory-ISAPI-2.0-64 if your application pool is setup against .Net 2.0 running on a 64bit OS</item>
    /// <item>Next, under the actions heading click on the link <c>Add Script Map</c></item>
    /// <item>In the <c>Request path</c> Enter a file extension, which must include the string <c>&quot;css&quot;</c> or <c>&quot;js&quot;</c> to be recognised. If you want all
    /// CSS or JavaScript files to be processed by ASP.NET you can use the standard <c>css</c> or <c>js</c> extensions; 
    /// otherwise use a custom extension such as <c>cssx</c> or <c>jsx</c>. </item>
    /// <item>In the <c>Executable</c> field paste in the path to the ISAPI filter</item>
    /// <item>Next in the  <c>Name</c> field enter the name to describe the handler  e.g. CSS Files</item>
    /// <item>Click on the <c>Request Restrictions</c> button to set the mappings, verbs and access settings</item>
    /// <item>Leave the mappings tab alone and move on to verbs tab</item>
    /// <item>Enter the<c>Verb</c> <c>GET</c>, select <c>Access</c> and then select <c>Script</c>. Click <c>OK</c> to confirm and <c>OK</c> again to the add new script mapping.</item>
    /// <item>IIS will then try to update the web.config file, make sure it is checked out or you will get an access denied error on writing the changes.</item>
    /// </list>
    /// <b>In IIS 7 (method 2):</b>
    /// <para>Alternatively you can just add the two entries to <c>web.config</c> yourself:</para>
    /// <example><code>
    /// &lt;configuration&gt;
    ///  	&lt;system.webServer&gt;
    ///         &lt;handlers&gt;
    ///		        &lt;add name=&quot;JS Files&quot; path=&quot;*.jsx&quot; verb=&quot;GET&quot; modules=&quot;IsapiModule&quot; scriptProcessor=&quot;C:\Windows\Microsoft.NET\Framework\v4.0.30319\aspnet_isapi.dll&quot; resourceType=&quot;Unspecified&quot; requireAccess=&quot;Script&quot; preCondition=&quot;classicMode,bitness32&quot; /&gt;
    ///		        &lt;add name=&quot;CSS Files&quot; path=&quot;*.cssx&quot; verb=&quot;GET&quot; modules=&quot;IsapiModule&quot; scriptProcessor=&quot;C:\Windows\Microsoft.NET\Framework\v4.0.30319\aspnet_isapi.dll&quot; resourceType=&quot;Unspecified&quot; requireAccess=&quot;Script&quot; preCondition=&quot;classicMode,bitness32&quot; /&gt;
    ///         &lt;/handlers&gt;
    ///  	&lt;/system.webServer&gt;
    /// &lt;/configuration&gt;
    /// </code></example>
    /// <b>In IIS 7 (either method):</b>
    /// <para>If the handler is not being used you will get a 404 error. Check that the <c>preCondition</c> attribute in the <c>&lt;system.webServer&gt;</c>
    ///  section matches the application pool and framework version of your application.</para>
    /// <b>Both IIS 6 and 7</b>
    /// <para>Specify a handler path for each type of file. This is the template for the combined links to your static files, 
    /// where <c>{0}</c> will be replaced by the filename at run-time. In a development environment include <c>{1}</c>, 
    /// which will be replaced at run-time by a random string, forcing the browser to download a new copy of the file with each 
    /// request machines. This complete path doesn't reference a real file, it's just a trigger to fire the HTTP handler, so it 
    /// must end with the file extension you registered above.</para>
    /// <example><code>
    /// &lt;configuration&gt;
    ///     &lt;CssFiles&gt;
    ///         &lt;add key=&quot;HandlerPath&quot; value=&quot;/{0}-{1}-v1.cssx&quot; /&gt;
    ///     &lt;/CssFiles&gt;
    ///
    ///     &lt;ScriptFiles&gt;
    ///         &lt;add key=&quot;HandlerPath&quot; value=&quot;/{0}-v2.jsx&quot; /&gt;
    ///     &lt;/ScriptFiles&gt;
    ///   &lt;/EsccWebTeam.Egms&gt;
    /// &lt;/configuration&gt;
    /// </code></example>
    /// <para>Note the version number (<c>-v1</c>). You can leave this out, but when you update your CSS or JavaScript files and you need to ensure users
    /// get the latest version rather than the old cached version, add or increment this number.</para>
    /// <para>You can also specify the <c>ID</c> of a <see cref="System.Web.UI.WebControls.ContentPlaceHolder"/> where the elements should appear on the rendered page. 
    /// CSS files are linked from the page &lt;head&gt;&lt;/head&gt; by default, but for JavaScript this lets you move all script file references to a placeholder at the 
    /// end of the page.</para>
    /// <para>The order that files are loaded is altered automatically (alphabetically, by the key used to identify them). This makes the 
    /// generated filenames most consistent and therefore more likely to be cached, and for CSS it means the cascade will operate predictably because files 
    /// are loaded in the same order regardless of where on the page they were included. It can be important to override this though, for example when you need to have 
    /// JavaScript library code loaded first, before other code that relies on it. In this case add a number between 1 and 9 followed by an underscore at the start of 
    /// the <c>web.config</c> reference. Entries without a number will be treated like a 5. You don't need to remember the prefix when referencing the library on the page.</para>
    /// <example><code>
    /// &lt;configuration&gt;
    ///     &lt;ScriptFiles&gt;
    ///         &lt;add key=&quot;HandlerPlaceholder&quot; value=&quot;javascript&quot; /&gt;
    ///
    ///       &lt;add key=&quot;1_ExampleLibrary&quot; value=&quot;/example/library.js&quot; /&gt;
    ///       &lt;add key=&quot;ExampleScript&quot; value=&quot;/example/script.js&quot; /&gt;
    ///     &lt;/ScriptFiles&gt;
    ///   &lt;/EsccWebTeam.Egms&gt;
    /// &lt;/configuration&gt;
    /// </code></example>
    /// <para>Combined files are cached for 30 days by default. If you want to change this, set the <c>HttpCacheDays</c> configuration setting.</para>
    /// <example><code>
    /// &lt;configuration&gt;
    ///     &lt;CssFiles&gt;
    ///         &lt;add key=&quot;HttpCacheDays&quot; value=&quot;60&quot; /&gt;
    ///     &lt;/CssFiles&gt;
    ///   &lt;/EsccWebTeam.Egms&gt;
    /// &lt;/configuration&gt;
    /// </code></example>
    /// <para>The code for the handler was downloaded from <a href="http://archive.msdn.microsoft.com/HttpCombiner">http://archive.msdn.microsoft.com/HttpCombiner</a> and tweaked to fit our configuration.</para>
    /// <para><b>Notes</b></para>
    /// <list type="bullet">
    /// <item>
    ///     <para>You should also minify your static files using a tool such as the <a href="http://developer.yahoo.com/yui/compressor/">YUI Compressor</a>, 
    ///     kicked off by a post-build action on your project such as:</para>
    ///     <example><code>if "$(ConfigurationName)" == "Release" java -jar "$(ProjectDir)yuicompressor-2.3.5.jar" "$(ProjectDir)css/styles.css" &gt; "$(ProjectDir)css/min/styles.css"</code></example>
    /// </item>
    /// <item>When you build <c>EsccWebTeam.Egms.dll</c> in <c>Debug</c> mode static files will be combined but not cached. This is so that you can 
    /// keep changing them while developing. Build in <c>Release</c> mode to enable caching.</item>
    /// <item>Rather than having separate stylesheets for older versions of Internet Explorer, use the conditional comments and <c>.ie6</c>, <c>.ie7</c> and <c>.ie8</c>
    /// classes defined by <a href="http://www.modernizr.com/">Modernizr</a>.</item>
    /// <item>Use an <c>@media</c> rule to target specific media and combine your stylesheets using this method. This avoids the extra HTTP requests
    /// needed when you use the <c>media</c> attribute of the &lt;link /&gt; element.</item>
    /// <item>If you ever need to support another file type you need to create a new derived control like <see cref="Css"/> or <see cref="Script"/>, 
    /// and update <see cref="CombineStaticFilesHandler.ProcessRequest"/> to recognise the file extension and use the correct media type.</item>
    /// </list>
    /// </remarks>
    public class CombineStaticFilesHandler : IHttpHandler
    {
        private const bool DO_GZIP = true;
        private TimeSpan CACHE_DURATION = TimeSpan.FromDays(30);

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"/> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                // recognise a URL built up by a CombineStaticFilesControl and pick out the relevant information
                var match = Regex.Match(Path.GetFileName(context.Request.Path), @"(?<nocache>nocache[0-9]+-)?(?<keys>[a-z0-9-]+?)(?<version>-v[0-9]+)?\.(?<type>[A-Za-z0-9]+)$");
                if (!match.Success)
                {
                    new FormatException("Unable to parse the request URL: " + Path.GetFileName(context.Request.Path)).ToExceptionless().Submit();
                    context.Response.Status = "400 Bad Request";
                    context.Response.StatusCode = 400;
                    try
                    {
                        context.Response.End();
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                    }
                    return;
                }

                string configKeys = match.Groups["keys"].Value;
                string contentType = match.Groups["type"].Value.ToUpperInvariant();
                string version = match.Groups["version"].Value;
                string cacheKey = configKeys + contentType + version;
                string configSection = String.Empty;
                if (contentType.Contains("CSS"))
                {
                    contentType = "text/css";
                    configSection = "CssFiles";
                }
                else if (contentType.Contains("JS"))
                {
                    contentType = "text/javascript";
                    configSection = "ScriptFiles";
                }
                else
                {
                    new FormatException("File extension of request must include the string \"css\" or \"js\"").ToExceptionless().Submit();
                    context.Response.Status = "400 Bad Request";
                    context.Response.StatusCode = 400;
                    try
                    {
                        context.Response.End();
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                    }
                    return;
                }

                // Decide if browser supports compressed response
                bool isCompressed = DO_GZIP && this.CanGZip(context.Request);

                // Response is written as UTF8 encoding. 
                UTF8Encoding encoding = new UTF8Encoding(false);
                byte[] fileDivider = encoding.GetBytes(Environment.NewLine);

                // If the set has already been cached, write the response directly from
                // cache. Otherwise generate the response and cache it
                if (!this.WriteFromCache(context, cacheKey, isCompressed, contentType))
                {
                    using (MemoryStream memoryStream = new MemoryStream(5000))
                    {
                        // Decide regular stream or GZipStream based on whether the response
                        // can be cached or not
                        using (Stream writer = isCompressed ?
                            (Stream)(new GZipStream(memoryStream, CompressionMode.Compress)) :
                            memoryStream)
                        {

                            // Load the files defined in config and process each file.
                            var config = LoadConfiguration(configSection);

                            // Check whether config overrides the default cache duration
                            if (ConfigurationValue(config, "HttpCacheDays") != null)
                            {
                                try
                                {
                                    this.CACHE_DURATION = TimeSpan.FromDays(Double.Parse(ConfigurationValue(config, "HttpCacheDays")));
                                }
                                catch (FormatException)
                                {
                                    new ConfigurationErrorsException("HttpCacheDays in web.config must be an integer. The value found was " + ConfigurationValue(config, "HttpCacheDays")).ToExceptionless().Submit();
                                }
                            }

                            // Allow keys to be preceded by a number between 1 and 9 followed by an underscore. This allows prioritisation of some files so that,
                            // for example, a script library will always be loaded first. If this pattern is not used and the key is an exact match, treat it like 
                            // priority 5, or normal priority.
                            var keys = new List<string>(configKeys.Split('-'));
                            for (var i = 1; i <= 9; i++)
                            {
                                foreach (string key in keys)
                                {
                                    string prioritisedKey;
                                    if (i == 5)
                                    {
                                        // Is the key present without a priority?
                                        prioritisedKey = key;
                                        if (ConfigurationValue(config, prioritisedKey) == null)
                                        {
                                            // No? Then how about with a priority of 5?
                                            prioritisedKey = i.ToString(CultureInfo.InvariantCulture) + "_" + key;
                                            if (ConfigurationValue(config, prioritisedKey) == null)
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Is the key present with the current priority?
                                        prioritisedKey = i.ToString(CultureInfo.InvariantCulture) + "_" + key;
                                        if (ConfigurationValue(config, prioritisedKey) == null)
                                        {
                                            continue;
                                        }
                                    }


                                    byte[] fileBytes = this.GetFileBytes(context, ConfigurationValue(config, prioritisedKey).Trim(), encoding);
                                    writer.Write(fileBytes, 0, fileBytes.Length);
                                    writer.Write(fileDivider, 0, fileDivider.Length); // add newline to separate from next file
                                }
                            }
                            writer.Close();
                        }

                        // Cache the combined response so that it can be directly written
                        // in subsequent calls 
                        byte[] responseBytes = memoryStream.ToArray();
                        if (responseBytes.Length > 0)
                        {
                            context.Cache.Insert(GetCacheKey(cacheKey, isCompressed),
                                responseBytes, null, System.Web.Caching.Cache.NoAbsoluteExpiration,
                                CACHE_DURATION);

                            // Generate the response
                            this.WriteBytes(responseBytes, context, isCompressed, contentType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
            }
        }

        private static string ConfigurationValue(Dictionary<string, string> config, string configKey)
        {
            var key = configKey.ToUpperInvariant();
            if (config != null && config.ContainsKey(key) && !String.IsNullOrEmpty(config[key]))
            {
                return config[key];
            }
            return null;
        }

        private Dictionary<string, string> LoadConfiguration(string configSection)
        {
            // Support two names for config section to aid transition from this method to the Client Dependency Framework
            var config = new Dictionary<string, string>();
            var originalConfig = ConfigurationManager.GetSection("EsccWebTeam.Egms/" + configSection) as NameValueCollection;
            if (originalConfig != null)
            {
                foreach (var configKey in originalConfig.AllKeys)
                {
                    var key = configKey.ToUpperInvariant();
                    config.Add(key, originalConfig[key]);
                }
            }

            var clientDependencyConfig = ConfigurationManager.GetSection("Escc.ClientDependencyFramework/" + configSection) as NameValueCollection;
            if (clientDependencyConfig != null)
            {
                foreach (var configKey in clientDependencyConfig.AllKeys)
                {
                    var key = configKey.ToUpperInvariant();
                    if (config.ContainsKey(key)) config.Remove(key);
                    config.Add(key, clientDependencyConfig[key]);
                }
            }

            return config;
        }

        private byte[] GetFileBytes(HttpContext context, string virtualPath, Encoding encoding)
        {
            if (virtualPath.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || virtualPath.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                using (WebClient client = new WebClient())
                {
                    return client.DownloadData(virtualPath);
                }
            }
            else
            {
                // Assuming files are either ASCII or UTF8
                string physicalPath = context.Server.MapPath(virtualPath);
                if (!File.Exists(physicalPath)) return new byte[] { };

                // Original code was byte[] bytes = File.ReadAllBytes(physicalPath), but that adds an EOF (or similar)
                // marker which prevents the first selector in the subsequent file from being applied. Reading as text 
                // first means the marker is excluded.
                byte[] bytes = encoding.GetBytes(File.ReadAllText(physicalPath));
                return bytes;
            }
        }

        private bool WriteFromCache(HttpContext context, string cacheKey, bool isCompressed, string contentType)
        {
            // If we don't want to cache the result because it's in development just return false
#if DEBUG
            return false;
#else
            // If we don't want the result from cache because we're trying to force the cache to update just return false
            if (!String.IsNullOrEmpty(context.Request.QueryString["ForceCacheRefresh"]))
            {
                return false;
            }

            byte[] responseBytes = context.Cache[GetCacheKey(cacheKey, isCompressed)] as byte[];

            if (null == responseBytes || 0 == responseBytes.Length) return false;

            this.WriteBytes(responseBytes, context, isCompressed, contentType);
            return true;
#endif
        }

        private void WriteBytes(byte[] bytes, HttpContext context,
            bool isCompressed, string contentType)
        {
            HttpResponse response = context.Response;

            response.AppendHeader("Content-Length", bytes.Length.ToString());
            response.ContentType = contentType;
            if (isCompressed)
                response.AppendHeader("Content-Encoding", "gzip");

            response.Cache.SetCacheability(HttpCacheability.Public);
            response.Cache.SetExpires(DateTime.Now.Add(CACHE_DURATION));
            response.Cache.SetMaxAge(CACHE_DURATION);
            response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");

            // Advice from Google Page Speed: Set the Vary: Accept-Encoding response header. 
            // Unfortunately ASP.net doesn't let you do that!

            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Flush();
        }

        private bool CanGZip(HttpRequest request)
        {
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (!string.IsNullOrEmpty(acceptEncoding) &&
                 (acceptEncoding.Contains("gzip") || acceptEncoding.Contains("deflate")))
                return true;
            return false;
        }

        private string GetCacheKey(string cacheKey, bool isCompressed)
        {
            return "EsccWebTeam.Egms.CombineStaticFilesHandler." + cacheKey + "." + isCompressed;
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"/> instance.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"/> instance is reusable; otherwise, false.
        /// </returns>
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}