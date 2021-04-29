FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS publish
WORKDIR /src
COPY src/ .
WORKDIR /src/Node/Blockcore.Node
RUN dotnet publish *.csproj -c Release -o /app/publish

FROM base AS final
RUN apt-get update && apt install -y libsnappy-dev zlib1g-dev libbz2-dev liblz4-dev libzstd-dev make g++ git

WORKDIR /
RUN git clone https://github.com/facebook/rocksdb.git \
    && cd rocksdb \
    && git checkout tags/v6.2.2 \
    && make shared_lib
WORKDIR /rocksdb
RUN cp librocksdb.so.6.2.2 ../app

WORKDIR /app
RUN ln -fs librocksdb.so.6.2.2 librocksdb.so.6.2
RUN ln -fs librocksdb.so.6.2.2 librocksdb.so.6
RUN ln -fs librocksdb.so.6.2.2 librocksdb.so

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Blockcore.Node.dll"]
