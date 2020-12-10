#!/bin/bash

sudo docker pull 542153354/sso:v2.0 

containerId="`sudo docker ps | grep "8443->80" | awk  '{print $1}'`"
echo "containerId:$containerId"
if [ -n "$containerId" ]
then
 sudo docker stop $containerId
 sudo docker rm $containerId
fi

 sudo docker run --rm -it -p 8000:80 -p 8443:443 -e ASPNETCORE_URLS="https://+;http://+" -e ASPNETCORE_HTTPS_PORT=8001 -e ASPNETCORE_Kestrel__Certificates__Default__Password="222222" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx -v ${HOME}/.aspnet/https:/https/ 542153354/sso:v2.0
exit
