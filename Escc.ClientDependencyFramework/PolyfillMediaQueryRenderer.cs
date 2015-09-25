using System;
using System.Collections.Generic;
using ClientDependency.Core.FileRegistration.Providers;

namespace Escc.ClientDependencyFramework
{
    /// <summary>
    /// The standard renderer for the Client Dependency Framework except that, when a stylesheet is restricted by a media query, 
    /// a copy without the media query is output for old versions of Internet Explorer.
    /// </summary>
    /// <remarks>On its own this makes IE render all stylesheets. However classes are added which work with 
    /// https://github.com/east-sussex-county-council/Escc.EastSussexGovUK/blob/master/js/media-queries.js
    /// to polyfill media query support in Internet Explorer.</remarks>
    public class PolyfillMediaQueryRenderer : StandardRenderer
    {
        /// <summary>
        /// Renders a single link element, with a conditional comment when required.
        /// </summary>
        /// <param name="css">The CSS.</param>
        /// <param name="htmlAttributes">The HTML attributes.</param>
        /// <returns></returns>
        protected override string RenderSingleCssFile(string css, IDictionary<string, string> htmlAttributes)
        {
            var linkElement = base.RenderSingleCssFile(css, htmlAttributes);
            var conditionalComment = BuildInternetExplorerConditionalComment(css, htmlAttributes);

            if (!String.IsNullOrEmpty(conditionalComment)) linkElement += conditionalComment;

            return linkElement;
        }

        private string BuildInternetExplorerConditionalComment(string css, IDictionary<string, string> htmlAttributes)
        {
            // If no media attribute, no comment needed
            if (htmlAttributes == null) return null;
            if (!htmlAttributes.ContainsKey("media")) return null;

            // IE does support these CSS2 media types
            var mediaValue = htmlAttributes["media"].ToUpperInvariant();
            if (mediaValue == "SCREEN" || mediaValue == "PRINT" || mediaValue == "ALL")
            {
                return null;
            }

            // Remove the media attribute, and add a class for the polyfill script to look for
            htmlAttributes.Remove("media");

            var classValue = "mqIE";
            if (htmlAttributes.ContainsKey("class"))
            {
                classValue += " " + htmlAttributes["class"];
                htmlAttributes.Remove("class");
            }
            htmlAttributes.Add("class", classValue);

            // Note that this output is HTML encoded by the base framework, so complex media queries involving an & aren't supported
            return "<!--[if (lte IE 8)]>" + base.RenderSingleCssFile(css, htmlAttributes) + "<![endif]-->";
        }
    }
}