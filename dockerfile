FROM registry.cn-shanghai.aliyuncs.com/wzyuchen/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM registry.cn-shanghai.aliyuncs.com/wzyuchen/sdk:3.1-buster AS build
COPY . .
RUN dotnet restore "sso.csproj"
RUN dotnet build "sso.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sso.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sso.dll"]