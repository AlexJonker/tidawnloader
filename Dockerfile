FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN apt-get update \
    && apt-get install curl -y \
    && curl -fsSL https://deb.nodesource.com/setup_25.x | bash - \
    && apt-get install nodejs -y \
    && apt-get clean

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN npm install
RUN npm run build:minified:css

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update && apt-get install -y xz-utils wget && apt-get clean \
    && ARCH=$(dpkg --print-architecture) \
    && case "$ARCH" in \
        amd64) FFMPEG_ARCH="linux64" ;; \
        arm64) FFMPEG_ARCH="linuxarm64" ;; \
        *) echo "Unsupported arch: $ARCH" && exit 1 ;; \
    esac \
    && wget -q https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-${FFMPEG_ARCH}-gpl.tar.xz \
    && tar -xf ffmpeg-master-latest-${FFMPEG_ARCH}-gpl.tar.xz \
    && mv ffmpeg-master-latest-${FFMPEG_ARCH}-gpl/bin/ffmpeg /usr/local/bin/ffmpeg \
    && mv ffmpeg-master-latest-${FFMPEG_ARCH}-gpl/bin/ffprobe /usr/local/bin/ffprobe \
    && rm -rf ffmpeg-master-latest-${FFMPEG_ARCH}-gpl*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Tidawnloader.dll"]