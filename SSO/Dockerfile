#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

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


ENV ASPNETCORE_Kestrel__Certificates__Default__Password="123456" 
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/server.pfx
ENV certPassword 123456
 
RUN openssl genrsa -des3 -passout pass:${certPassword} -out server.key 2048
RUN openssl rsa -passin pass:${certPassword} -in server.key -out server.key
RUN openssl req -sha256 -new -key server.key -out server.csr -subj '/CN=localhost'
RUN openssl x509 -req -sha256 -days 365 -in server.csr -signkey server.key -out server.crt
RUN openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt -certfile server.crt -passout pass:${certPassword}


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SSO.dll"]