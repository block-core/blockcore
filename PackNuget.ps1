[cmdletbinding()]

Param(
	[Parameter(Mandatory=$true)]
	[string]$releaseType
)

$projectPaths = @(
	# BASE PROJECTS
	".\src\NBitcoin\NBitcoin.csproj",
	".\src\Stratis.Bitcoin\Stratis.Bitcoin.csproj",
	#FEATURES PROJECTS
	".\src\Stratis.Bitcoin.Features.Api\Stratis.Bitcoin.Features.Api.csproj",
	".\src\Stratis.Bitcoin.Features.BlockStore\Stratis.Bitcoin.Features.BlockStore.csproj",
	".\src\Stratis.Bitcoin.Features.Consensus\Stratis.Bitcoin.Features.Consensus.csproj",
	".\src\Stratis.Bitcoin.Features.Dns\Stratis.Bitcoin.Features.Dns.csproj",
	".\src\Stratis.Bitcoin.Features.LightWallet\Stratis.Bitcoin.Features.LightWallet.csproj",
	".\src\Stratis.Bitcoin.Features.MemoryPool\Stratis.Bitcoin.Features.MemoryPool.csproj",
	".\src\Stratis.Bitcoin.Features.Miner\Stratis.Bitcoin.Features.Miner.csproj",
	".\src\Stratis.Bitcoin.Features.PoA\Stratis.Bitcoin.Features.PoA.csproj",
	".\src\Stratis.Bitcoin.Features.Notifications\Stratis.Bitcoin.Features.Notifications.csproj",
	".\src\Stratis.Bitcoin.Features.RPC\Stratis.Bitcoin.Features.RPC.csproj",
	".\src\Stratis.Bitcoin.Features.Wallet\Stratis.Bitcoin.Features.Wallet.csproj",
	".\src\Stratis.Bitcoin.Features.WatchOnlyWallet\Stratis.Bitcoin.Features.WatchOnlyWallet.csproj",
	".\src\Stratis.Bitcoin.Networks\Stratis.Bitcoin.Networks.csproj",
	".\src\Stratis.Features.Collateral\Stratis.Features.Collateral.csproj",
	".\src\Stratis.Features.FederatedPeg\Stratis.Features.FederatedPeg.csproj",
	# SMARTCONTRACT PROJECTS
	".\src\Stratis.Sidechains.Networks\Stratis.Sidechains.Networks.csproj",
	".\src\Stratis.SmartContracts.CLR\Stratis.SmartContracts.CLR.csproj",
	".\src\Stratis.SmartContracts.CLR.Validation\Stratis.SmartContracts.CLR.Validation.csproj",
	".\src\Stratis.SmartContracts.Core\Stratis.SmartContracts.Core.csproj",
	".\src\Stratis.SmartContracts.Networks\Stratis.SmartContracts.Networks.csproj",
	".\src\Stratis.SmartContracts.RuntimeObserver\Stratis.SmartContracts.RuntimeObserver.csproj",
	".\src\Stratis.Bitcoin.Features.SmartContracts\Stratis.Bitcoin.Features.SmartContracts.csproj"
	# TESTS PROJECTS
	".\src\Stratis.Bitcoin.IntegrationTests.Common\Stratis.Bitcoin.IntegrationTests.Common.csproj",
	".\src\Stratis.Bitcoin.Tests.Common\Stratis.Bitcoin.Tests.Common.csproj",
	".\src\Stratis.Bitcoin.Tests.Wallet.Common\Stratis.Bitcoin.Tests.Wallet.Common.csproj",
	".\src\Stratis.Bitcoin.Features.PoA.IntegrationTests.Common\Stratis.Bitcoin.Features.PoA.IntegrationTests.Common.csproj",
	# TOOLS PROJECTS
	".\src\FodyNlogAdapter\FodyNlogAdapter.csproj"
)

if (-not $Env:BUILD_BUILDNUMBER)
{
	Write-Error ("BUILD_BUILDNUMBER environment variable is missing.")
	exit 1
}

Write-Verbose "Release Type: $releaseType"
Write-Verbose "BUILD_BUILDNUMBER: $Env:BUILD_BUILDNUMBER"

foreach ($projectPath in $projectPaths) {
	if (Test-Path $projectPath -PathType Leaf)
	{
		try 
		{ 
			dotnet pack $projectPath --configuration Debug --include-source --include-symbols --version-suffix $releaseType$Env:BUILD_BUILDNUMBER --verbosity Detailed		
			Write-Verbose "Published - $projectPath";
		}
		catch 
		{ 
			Write-Error ("Failed to publish - $projectPath")	
		}
	}
	else
	{
		Write-Error ("Can't find project to publish - $projectPath")	
	}
}
