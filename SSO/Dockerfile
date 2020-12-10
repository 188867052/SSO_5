FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["SSO/SSO.csproj", "SSO/"]
RUN dotnet restore "SSO/SSO.csproj"
COPY . .
WORKDIR "/src/SSO"
RUN dotnet build "SSO.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SSO.csproj" -c Release -o /app/publish

#RUN dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p 111111
#RUN dotnet dev-certs https --trust

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SSO.dll"]