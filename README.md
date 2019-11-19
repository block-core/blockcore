| Windows | MacOs | Ubuntu64
| :---- | :------ | :---- |
| [![Build Status](https://dev.azure.com/StratisProject/StratisBitcoinFullNode/_apis/build/status/HostedWindowsContainer-CI)](https://dev.azure.com/StratisProject/StratisBitcoinFullNode/_build/latest?definitionId=4) | [![Build Status](https://dev.azure.com/StratisProject/StratisBitcoinFullNode/_apis/build/status/HostedmacOS-CI)](https://dev.azure.com/StratisProject/StratisBitcoinFullNode/_build/latest?definitionId=6) | [![Build Status](https://dev.azure.com/StratisProject/StratisBitcoinFullNode/_apis/build/status/HostedUbuntu1604-CI)](https://dev.azure.com/StratisProject/StratisBitcoinFullNode/_build/latest?definitionId=5)

Blockcore
===============

https://blockcore.net

Bitcoin Implementation in C#
----------------------------

**What is Blockcore?**

- Blockcore is a platform to build Layer 1 consensus networks based on the Bitcoin protocol, built on the [.NET Core](https://dotnet.github.io/) framework and written entirely in C#. 
- Blockcore aims to maintain an alternative C# Bitcoin implementation, based on the [NBitcoin](https://github.com/MetacoSA/NBitcoin) & [Stratis](https://github.com/stratisproject/StratisBitcoinFullNode) projects.
- Blockcore is neither a coin or a for profit business.

**Why Blockcore?**

- We see a need within the crypto ecosystem for development of the C# full node technology.
- [Stratis](https://github.com/stratisproject/StratisBitcoinFullNode) has provided an excellent starting point but their focus is enterprise and businesses and we feel strongly that there is also value focusing on open and public blockchains.

**Blockcore objectives**

- Continue development of the C# Stratis fullnode.
- Maintain the C# Bitcoin fullnode.
- Support projects and teams that use the underlying technology.
- Extend the technology by building developer and user tools
- Provide a forum for developers and teams to collaborate and improve on the technology.

**Blockcore principles**

- We help each other, and all projects that utilise the underlying technology.
- We encourage contribution to the Blockcore open source software.
- We aim to make it easier for everyone to contribute to the ecosystem.
- We encourage projects to adopt Blockcore technology as we believe every project has something to offer and help make the technology stronger.

Join our community on [discord](https://discord.gg/TXx4Rm3).  


**Running a Full Node**
------------------

The master branch is actively developed and regularly committed to, and it can become unstable.  
To compile a stable (production) release use the most recent release tags.  

```
git clone https://github.com/block-core/blockcore.git  
cd StratisBitcoinFullNode\src
```

To run on the Bitcoin network:
```
cd Stratis.BitcoinD
dotnet run
```  

To run on the Stratis network:
```
cd Stratis.StratisD
dotnet run
```  

Getting Started Guide
-----------
More details on getting started are available [here](https://github.com/block-core/blockcore/blob/master/Documentation/getting-started.md)

Development
-----------
Up for some blockchain development?

Check this guides for more info:
* [Contributing Guide](Documentation/contributing.md)
* [Coding Style](Documentation/coding-style.md)

There is a lot to do and we welcome contributers developers and testers who want to get some Blockchain experience.

You can find tasks at the issues/projects or visit us on [discord](https://discord.gg/TXx4Rm3).

Testing
-------
* [Testing Guidelines](Documentation/testing-guidelines.md)
