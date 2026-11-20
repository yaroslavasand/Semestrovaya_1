FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .

RUN dotnet publish *.csproj \
    -c Release \
    -o /app/publish \
    --self-contained false \
    -p:UseAppHost=false \
    -p:AssemblyName=OrisApp

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrisApp.dll"]