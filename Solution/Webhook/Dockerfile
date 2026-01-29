# syntax=docker/dockerfile:1.6
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Solution/Webhook/Webhook.csproj Solution/Webhook/
RUN dotnet restore Solution/Webhook/Webhook.csproj
COPY Solution/Webhook/ Solution/Webhook/
WORKDIR /src/Solution/Webhook
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY Solution/Webhook/Certificates/ ./Certificates/
ENV ASPNETCORE_URLS=https://+:8081 \
    ASPNETCORE_Kestrel__Certificates__Default__Path=/app/Certificates/webhook-server.pfx \
    ASPNETCORE_Kestrel__Certificates__Default__Password=change_me \
    Webhook__ClientCertificate__AuthorityPath=/app/Certificates/webhook-ca.cer \
    DOTNET_EnableDiagnostics=0
EXPOSE 8081
ENTRYPOINT ["dotnet","Webhook.dll"]
