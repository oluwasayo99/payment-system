FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

COPY nuget.config .
COPY packages/ packages/
COPY Demo.Inventory/ Demo.Inventory/

RUN dotnet restore Demo.Inventory/src/Demo.Inventory.Service/Demo.Inventory.Service.csproj

EXPOSE 8080
