FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine

# disable microsoft telematry
ENV DOTNET_CLI_TELEMETRY_OPTOUT='true'

ARG LBHPACKAGESTOKEN
ENV LBHPACKAGESTOKEN=$LBHPACKAGESTOKEN

ARG SONAR_TOKEN
ENV SONAR_TOKEN=$SONAR_TOKEN

WORKDIR /app

RUN apk update && apk add --no-cache nodejs

RUN apk update \
 && apk upgrade --no-cache \
 && apk add --no-cache openjdk21

RUN dotnet tool install --global dotnet-sonarscanner
RUN dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.2.0

ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet sonarscanner begin \
    /k:"LBHackney-IT_housing-search-listener" \
    /o:"lbhackney-it" \
    /d:sonar.host.url=https://sonarcloud.io \
    /d:sonar.login="${SONAR_TOKEN}" \
    /d:sonar.coverageReportPaths="coverage/SonarQube.xml" \
    /d:sonar.dotnet.excludeTestProjects=true \
    /d:sonar.exclusions="**/*.js, **/*.ts, **/*.css"

COPY . .

RUN dotnet restore

RUN dotnet build ./HousingSearchListener/HousingSearchListener.csproj --no-restore --no-dependencies
RUN dotnet build ./HousingSearchListener.Tests/HousingSearchListener.Tests.csproj --no-restore

CMD dotnet test ./HousingSearchListener.Tests/HousingSearchListener.Tests.csproj --no-build --collect:"XPlat Code Coverage" --results-directory ./coverage \
    && reportgenerator "-reports:./coverage/*/coverage.cobertura.xml" "-targetdir:coverage" "-reporttypes:SonarQube" "-verbosity:Off" \
    && (dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}" > /dev/null)
