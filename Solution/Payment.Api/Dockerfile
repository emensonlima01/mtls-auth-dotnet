# syntax=docker/dockerfile:1.6
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Solution/Payment.Api/Payment.Api.csproj Solution/Payment.Api/
RUN dotnet restore Solution/Payment.Api/Payment.Api.csproj
COPY Solution/Payment.Api/ Solution/Payment.Api/
WORKDIR /src/Solution/Payment.Api
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY Solution/Payment.Api/Certificates/ ./Certificates/
ENV ASPNETCORE_URLS=https://+:8080 \
    ASPNETCORE_Kestrel__Certificates__Default__Path=/app/Certificates/payment-api-server.pfx \
    ASPNETCORE_Kestrel__Certificates__Default__Password=change_me \
    Webhook__BaseUrl=https://webhook:8081/ \
    Webhook__ClientCertificate__Path=/app/Certificates/payment-api-client.pfx \
    Webhook__ClientCertificate__Password=change_me \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
ENTRYPOINT ["dotnet","Payment.Api.dll"]
