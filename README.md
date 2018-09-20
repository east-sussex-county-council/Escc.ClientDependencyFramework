Escc.ClientDependencyFramework
==============================

[ClientDependency](https://github.com/Shazwazza/ClientDependency) is a framework by Shannon Deminick for simplifying collaborative development of ASP.NET components, allowing components to state what CSS and JavaScript files they require, and ensuring that each file only is only added once to the response. ClientDependency also handles combination, minification and compression of CSS and JavaScript.

This package adds helper classes to read file paths and media queries from `web.config`, enabling them to be reused easily while being maintained in one place.

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

## Escc.ClientDependency.WebForms

This is a previous, home-grown solution for combining CSS and JavaScript files. New implementations should use [ClientDependency](https://github.com/Shazwazza/ClientDependency), which offers all the same features, as well as extra ones such as compatibility with ASP.NET MVC. 

The `Css` and `Script` classes in this project provide a flexible way to load and concatenate client-side files on WebForms pages, with the bundles worked out at runtime rather than compile time. This is documented in `CombineStaticFilesHandler`.