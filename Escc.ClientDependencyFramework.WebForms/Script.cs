
namespace Escc.ClientDependencyFramework.WebForms
{
    /// <summary>
    /// Include a combined and cached set of JavaScript files in a web page
    /// </summary>
    /// <remarks>See <see cref="CombineStaticFilesHandler"/> for details.</remarks>
    public class Script : CombineStaticFilesControl
    {
        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            this.BuildCombinedTag("ScriptFiles", false, "<script src=\"{0}\"{1}></script>");
        }
    }
}
