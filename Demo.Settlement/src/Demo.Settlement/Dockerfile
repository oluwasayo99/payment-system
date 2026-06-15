FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

COPY nuget.config .
COPY packages/ packages/
COPY Demo.Settlement/ Demo.Settlement/

RUN dotnet restore Demo.Settlement/src/Demo.Settlement/Demo.Settlement.csproj

