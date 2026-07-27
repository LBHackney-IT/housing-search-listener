FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine

# disable microsoft telematry
ENV DOTNET_CLI_TELEMETRY_OPTOUT='true'

ARG LBHPACKAGESTOKEN
ENV LBHPACKAGESTOKEN=$LBHPACKAGESTOKEN

ARG SONAR_TOKEN
ENV SONAR_TOKEN=$SONAR_TOKEN

WORKDIR /app

RUN apk update && apk add --no-cache nodejs

RUN dotnet tool install --global dotnet-sonarscanner
RUN dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.2.0

ENV PATH="$PATH:/root/.dotnet/tools"

COPY . .

CMD if [ -n "$SONAR_TOKEN" ]; then \
      dotnet sonarscanner begin \
        /k:"LBHackney-IT_housing-search-listener" \
        /o:"lbhackney-it" \
        /d:sonar.host.url=https://sonarcloud.io \
        /d:sonar.scm.disabled=true \
        /d:sonar.login="${SONAR_TOKEN}" && \
      dotnet test && \
      dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}"; \
    else \
      dotnet test; \
    fi
