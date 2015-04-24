Escc.ClientDependencyFramework
==============================

[ClientDependency](https://github.com/Shazwazza/ClientDependency) is a framework by Shannon Deminick for simplifying collaborative development of ASP.NET components, allowing components to state what CSS and JavaScript files they require, and ensuring that each file only is only added once to the response. ClientDependency also handles combination, minification and compression of CSS and JavaScript.

This package adds:

*  The ability to set the version by adding a `ClientDependencyFramework.Version` setting to the `appSettings` section in `web.config` rather than the Client Dependency Framework's own configuration section. This is implemented in [a fork of the original project](https://github.com/east-sussex-county-council/ClientDependency), and means the version can be set using the Microsoft Azure Portal without editing `web.config`. 
*  A new renderer which looks for media queries and outputs a conditional comment targeting Internet Explorer 8 and below. This can be paired with [a polyfill script](https://github.com/east-sussex-county-council/Escc.EastSussexGovUK/blob/master/js/media-queries.js) to add media query support in those browsers.
* Helper classes to read file paths and media queries from `web.config`, enabling them to be reused easily while being maintained in one place.

	**C#**

	    Html.RequiresCss(CssFileAlias.Resolve("mystyles"), MediaQueryAlias.Resolve("large"));
	    Html.RequiresJs(JsFileAlias.Resolve("myscript"));

	**web.config**

		<configuration>
		  <configSections>
		    <sectionGroup name="Escc.ClientDependencyFramework">
		      <section name="MediaQueries" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
		      <section name="CssFiles" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
		      <section name="ScriptFiles" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
		    </sectionGroup>
		  </configSections>
		
		  <Escc.ClientDependencyFramework>
			<MediaQueries>
		      <add key="large" value="only screen and (min-width: 800px)" />
			</MediaQueries>
		    <CssFiles>
		      <add key="mystyles" value="/css/styles.css" />
		    </CssFiles>
		    <ScriptFiles>
		      <add key="myscript" value="/js/script.js" />
		    </ScriptFiles>
		  </Escc.ClientDependencyFramework>
		</configuration>



NuGet
-----

This project is published as a set of NuGet packages to our private feed. We use [NuBuild](https://github.com/bspell1/NuBuild) to make creating NuGet packages really easy, and [reference our private feed using a nuget.config file](http://blog.davidebbo.com/2014/01/the-right-way-to-restore-nuget-packages.html).

### Our fork of ClientDependency
The NuGet version number of this fork is deliberately higher (1.9.9.x) than the original so that it will be favoured over the original when resolving dependencies. However, it is kept lower than 2.0.0.0 so that it will be compatible with [Umbraco](https://github.com/umbraco/Umbraco-CMS/), which depends on versions below 2.0.0.0.

### Escc.ClientDependencyFramework

If you're not using Umbraco, use this NuGet package to add this version of the Client Dependency Framework to your project. You'll still need to install the separate package for your type of project. For example [ClientDependency-Mvc](http://www.nuget.org/packages/ClientDependency-Mvc/).


### Escc.ClientDependencyFramework.Umbraco

Umbraco already depends on the original Client Dependency Framework and has its configuration settings in a non-standard location (config/ClientDependency.config). This NuGet package:

* Depends upon ClientDependency, specifying a version number which favours our fork over the original, yet still fits within the constraint specified by Umbraco, ensuring we get our DLL even though Umbraco depends upon the original
* Has a slightly different config transform to target the non-standard location used by Umbraco
* Depends on Umbraco, so that Umbraco is added to the target project first, meaning its configuration file is present and able to be transformed by this package.

## Escc.ClientDependency.WebForms

This is a previous, home-grown solution for combining CSS and JavaScript files. New implementations should use the newer approach above, which offers all the same features, as well as extra ones such as compatibility with ASP.NET MVC. 

The `Css` and `Script` classes in this project provide a flexible way to load and concatenate client-side files on WebForms pages, with the bundles worked out at runtime rather than compile time. This is documented in `CombineStaticFilesHandler`.