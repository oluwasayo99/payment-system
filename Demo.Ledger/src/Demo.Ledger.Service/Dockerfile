FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

COPY nuget.config .
COPY packages/ packages/
COPY Demo.Ledger/ Demo.Ledger/

RUN dotnet restore Demo.Ledger/src/Demo.Ledger.Service/Demo.Ledger.Service.csproj

EXPOSE 8080
