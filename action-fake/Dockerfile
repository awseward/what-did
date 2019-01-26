FROM microsoft/dotnet:2.1.403-sdk

RUN dotnet tool install fake-cli -g

ENV PATH="${HOME}/.dotnet/tools:${PATH}"

ADD entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh", "uhhhhh"]
