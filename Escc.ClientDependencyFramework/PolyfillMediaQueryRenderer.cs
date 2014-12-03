using System.Collections.Generic;
using ClientDependency.Core.FileRegistration.Providers;

namespace Escc.ClientDependencyFramework
{
    public class PolyfillMediaQueryRenderer : StandardRenderer
    {
        protected override string RenderSingleCssFile(string css, IDictionary<string, string> htmlAttributes)
        {
            var html = base.RenderSingleCssFile(css, htmlAttributes);

            htmlAttributes.Remove("media");
            htmlAttributes["class"] = htmlAttributes["class"] + " mqIE";
            html += "<!--[if (lte IE 8) & !(IEMobile 7) ]>" + base.RenderSingleCssFile(css, htmlAttributes) + "<![endif]-->";

            return html;
        }
    }
}