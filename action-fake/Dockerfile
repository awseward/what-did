FROM microsoft/dotnet:2.1.403-sdk

RUN dotnet tool install fake-cli -g

ADD entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh", "heroku:web"]
