FROM microsoft/dotnet:2.1-aspnetcore-runtime-alpine
COPY deploy /
WORKDIR /WhatDid
EXPOSE 8085
CMD ["dotnet", "WhatDid.dll"]
