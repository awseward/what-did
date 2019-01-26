FROM microsoft/dotnet:2.1.403-sdk

RUN apt update -y \
 && apt install -y apt-transport-https dirmngr \
 && apt-key adv --no-tty --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
 && echo "deb https://download.mono-project.com/repo/debian stable-stretch main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
 && apt update -y \
 && apt install -y mono-complete


RUN apt-get update -qq \ 
 && dotnet tool install fake-cli -g

ADD entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh", "heroku:web"]
