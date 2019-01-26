FROM microsoft/dotnet:2.1.403-sdk

RUN apt-get update -qq               && \
    apt-get -y install mono-complete && \
    dotnet tool install fake-cli -g

ADD entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh", "heroku:web"]
