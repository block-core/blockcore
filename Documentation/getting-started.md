

# Getting started - Build instructions for x42-BlockCore

---------------

## Supported Platforms

* <b>Windows</b> - works from Windows 7 and later, on both x86 and x64 architecture. Most of the development and testing is happening here.
* <b>Linux</b> - works and Ubuntu 14.04 and later (x64). It's been known to run on some other distros so your mileage may vary.
* <b>MacOS</b> - works from OSX 10.12 and later. 

## Prerequisites

To install and run the node, you need
* [.NET Core 3.1](https://www.microsoft.com/net/download/core)
* [Git](https://git-scm.com/)

## Build instructions

### Get the repository and its dependencies

```
git clone https://github.com/x42protocol/x42-BlockCore.git
cd src/Networks/x42/x42.Node
```

### Build and run the code
With this node, you can connect to either the x42 MainNet or TestNet.
So you have 4 options:

1. To run a <b>x42.Node</b> node on <b>MainNet</b>, do
```
dotnet run
```  

2. To run a <b>x42.Node</b>  node on <b>TestNet</b>, do
```
dotnet run -testnet
```  

### Advanced options

You can get a list of command line arguments to pass to the node with the -help command line argument. For example:
```
dotnet run -help
```  


Simple UI/API
-------------------

Once the node is running, Simple dashboard and a Swagger interface (web UI for testing an API) is available.

* For Simple UI: http://localhost:42220/
* For Swagger API: http://localhost:42220/docs/index.html

* For Simple UI (Testnet): http://localhost:42221/
* For Swagger API (Testnet): http://localhost:42221/docs/index.html
