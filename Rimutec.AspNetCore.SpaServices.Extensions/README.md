# Webpack Development Server Support for ASP.NET Core 3.1

With ASP.NET Core 3.1 the NuGet package Microsoft.AspNetCore.SpaServices has been declared [deprecated](https://github.com/dotnet/aspnetcore/issues/12890). 

RimuTec's package offers a replacement for the webpack case by allowing using the webpack-dev-server in a way that is similar to what is available for React and Angular. More specifically, this nuget package implements an extension method ```ISpaBuilder.UseWebpackDevelopmentServer()```.

NuGet package `Microsoft.AspNetCore.NodeServices` which has been [deprecated](https://github.com/dotnet/aspnetcore/issues/12890), too. However, our NuGet package does not offer a replacement for that.

Create-React-App (CRA) assumes there is a single client app. CRA also requires you to "eject" our project to gain more flexibility. In other words CRA is great if you really can design your application such that everything can be done in a single page.

This extensions starts on the other end. It assumes that you start with a simple HTML page and a single bundle. A "hello, world"-example for Webpack if you like. You can then add as much or as little as you wish. This packages allows you to use hot-module-reloading (HMR) for your bundle.