[CmdletBinding()]
Param(
	[string]$releaseType,
	[string]$buildNumber
)

if (-not $releaseType) {
	$releaseType = $env:NUGET_RELEASE_TYPE
}
if (-not $releaseType) {
	Write-Error ("releaseType param or NUGET_RELEASE_TYPE environment variable is required.")
	exit 1
}

if (-not $buildNumber) {
	$buildNumber = $env:BUILD_NUMBER
}
if (-not $buildNumber) {
	Write-Error ("buildNumber param or BUILD_NUMBER environment variable is required.")
	exit 1
}

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
	# TESTS PROJECTS
	".\src\Stratis.Bitcoin.IntegrationTests.Common\Stratis.Bitcoin.IntegrationTests.Common.csproj",
	".\src\Stratis.Bitcoin.Tests.Common\Stratis.Bitcoin.Tests.Common.csproj",
	".\src\Stratis.Bitcoin.Tests.Wallet.Common\Stratis.Bitcoin.Tests.Wallet.Common.csproj",
	# TOOLS PROJECTS
	".\src\FodyNlogAdapter\FodyNlogAdapter.csproj"
)

Write-Verbose "Release Type: $releaseType"
Write-Verbose "Build Number: $buildNumber"


if ($releaseType -eq "release") {
	$configuration = "Release"
} else {
	$configuration = "Debug"
	$versionSuffix = "$releaseType$buildNumber"
}
	
Write-Verbose "Configuration: $configuration"
Write-Verbose "Version Suffix: $versionSuffix"

foreach ($projectPath in $projectPaths) {
	if (Test-Path $projectPath -PathType Leaf) {

		if (-not $versionSuffix) {
			dotnet pack $projectPath --configuration $configuration --no-build --include-source --include-symbols -o bin/publish/nuget/
		}
		else {
			dotnet pack $projectPath --configuration $configuration --no-build --include-source --include-symbols --version-suffix $versionSuffix -o bin/publish/nuget/ 
		}
	}
	else {
		Write-Error "Can't find project to pack nuget package $projectPath"
	}
}