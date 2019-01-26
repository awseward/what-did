FROM microsoft/dotnet:2.1.403-sdk

ENV PATH="${HOME}/.dotnet/tools:${PATH}"

RUN dotnet tool install fake-cli -g

ADD entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh", "uhhhhh"]
