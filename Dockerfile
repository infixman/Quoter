FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /src

COPY . /src
RUN dotnet build ./Quoter/Quoter.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ./Quoter/Quoter.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS runtime
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "Quoter.dll"]