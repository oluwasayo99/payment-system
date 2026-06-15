FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

COPY nuget.config .
COPY packages/ packages/
COPY Demo.Payment/ Demo.Payment/

RUN dotnet restore Demo.Payment/src/Demo.Payment.Service/Demo.Payment.Service.csproj

EXPOSE 8080
