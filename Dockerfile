FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS publish
WORKDIR /src
COPY src/ .
WORKDIR /src/Node/Blockcore.Node
RUN dotnet publish *.csproj -c Release -o /app/publish

FROM base AS final
RUN apt-get update -y
RUN apt-get install -y libsnappy-dev
RUN apt-get install -y libc6-dev
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Blockcore.Node.dll"]
