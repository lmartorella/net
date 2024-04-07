# The server: core C# projects

These are the projects that need to be built to implement the .NET server.

> The project doesn't have any 3rd party dependency other than the .NET Framework, in order to be ported to [.NET Core](https://dotnet.microsoft.com/download) or other platforms.

It requires [Visual Studio](https://visualstudio.microsoft.com/) (the free Community version is fine) and the [.NET Framework 4.x](https://dotnet.microsoft.com/download) for the administrative UI.

## Home.Common

This is C# library that shares code for the other two applications.

You will find here:
- Small dependency injection code
- Serialization services
- IPC services
- Logging a DB service
- The API to write applications: `IDevice` (the logic abstraction that needs one or more drivers) and `ISink`  (the driver part that communicates with the node over the Home protocol).
- An e-mail notification system.


## Home.Server

This is the command-line server process. It dynamically loads applicative *dlls* at startup (see samples) to integrate all the logic.

Place the sample dlls in the same folder of the main application: they will be recognized via attributes and loaded. 

## Home.Manager.UI

This WPF application can be used to:
- Visualize the network topology
- Initialize nodes
- Configure devices linking them to individual node sinks.
- Spawn simulated nodes on the IP network, for testing.

Like the server, also this application loads applicative dlls at startup. In addition to logic dlls, it allow UI dlls to load simulator and mock code as well.

## Application and samples

See [here](../../samples/server/README.md) for the samples, to build an application that does something. 
