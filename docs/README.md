# Motivation

The NuGet package RimuTec.AspNetCore.SpaService.Extensions is intended to provide a replacement for [`UseWebpackDevMiddleWare()` that has been declared depracated in ASP.NET Core 3.1](https://github.com/dotnet/aspnetcore/issues/12890).

RimuTec's SpaService.Extensions implements a method called `ISpaBuilder.UseWebpackDevelopmentServer()`. The goal was to provide a replacement that allows its use with a limited amount of code changes. We decided to use a different name because I wanted to make sure it is obvious that this is a new implementation by a different person.

WebpackDevServer (WPS) is meant to be used during development to speed up the inner development cycle.

## Outer and Inner Development Cycle

To better understand this concept consider an ASP.NET Core 3.1 application with a single page application (SPA), e.g. using React. The default development cycle would be to change code, then compile/build the application and then start it with `F5` within Visual Studio (`F5` is the the default mapping for running with debugger).

This default development cycle is also referred to as the outer development cycle. The challenge starts when you consider that generally an SPA consists of a large number of files. These can be JavaScript, TypeScript, style sheets, images, HTML pages and many more. If you use Webpack to create minified (and obfuscated) bundles, you would have to stop and rebuild the project and restart the application. This can slow down the process when working on the SPA.

WebpackDevServer monitors a set of files and each time a file changes it automatically rebuilds the bundle. In this mode it even adds some additional code that allows the resulting JavaScript code to interact with the server to load the bundle should there be a new version. This mechanism is called Hot Module Reload (HMR).

SpaService.Extensions makes it easier to use WPS in this setup.
