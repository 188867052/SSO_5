#!/bin/bash

echo 'docker-entrypoint'
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p 222222
dotnet dev-certs https --trust
dotnet SSO.dll
exit
