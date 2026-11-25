FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Vagalume.sln ./
COPY src/Vagalume.Domain/Vagalume.Domain.csproj src/Vagalume.Domain/
COPY src/Vagalume.Application/Vagalume.Application.csproj src/Vagalume.Application/
COPY src/Vagalume.Infra/Vagalume.Infra.csproj src/Vagalume.Infra/
COPY src/Vagalume.Api/Vagalume.Api.csproj src/Vagalume.Api/
RUN dotnet restore src/Vagalume.Api/Vagalume.Api.csproj
COPY src/ src/
COPY global.json ./
RUN dotnet publish src/Vagalume.Api/Vagalume.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# libgssapi evita warning de carga do Npgsql (Kerberos opcional)
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
# uploads volume
RUN mkdir -p /app/uploads && chown -R app:app /app
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Vagalume.Api.dll"]
