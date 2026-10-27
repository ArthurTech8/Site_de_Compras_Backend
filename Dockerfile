# Build: SDK completo só nesta etapa; a imagem final leva apenas o runtime.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para o Docker reaproveitar o cache quando só o código muda.
COPY src/SiteDeCompras.Domain/SiteDeCompras.Domain.csproj src/SiteDeCompras.Domain/
COPY src/SiteDeCompras.Application/SiteDeCompras.Application.csproj src/SiteDeCompras.Application/
COPY src/SiteDeCompras.Infrastructure/SiteDeCompras.Infrastructure.csproj src/SiteDeCompras.Infrastructure/
COPY src/SiteDeCompras.Api/SiteDeCompras.Api.csproj src/SiteDeCompras.Api/
RUN dotnet restore src/SiteDeCompras.Api/SiteDeCompras.Api.csproj

COPY src/ src/
RUN dotnet publish src/SiteDeCompras.Api/SiteDeCompras.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Pastas do banco SQLite e das chaves de sessão (Data Protection). Ficam em volumes no
# docker-compose, então o banco e os logins sobrevivem quando o container é recriado.
RUN mkdir -p /app/data /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /app/data /home/app/.aspnet

COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Default="Data Source=/app/data/site_de_compras.db" \
    Banco__AplicarMigrationsNaInicializacao=true \
    ProxyReverso__Habilitado=true

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SiteDeCompras.Api.dll"]
