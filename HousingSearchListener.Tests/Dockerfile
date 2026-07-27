FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine

# disable microsoft telematry
ENV DOTNET_CLI_TELEMETRY_OPTOUT='true'

ARG LBHPACKAGESTOKEN
ENV LBHPACKAGESTOKEN=$LBHPACKAGESTOKEN

ARG SONAR_TOKEN
ENV SONAR_TOKEN=$SONAR_TOKEN

WORKDIR /app

RUN dotnet tool install --global dotnet-sonarscanner
RUN dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.2.0

ENV PATH="$PATH:/root/.dotnet/tools"

# Copy csproj and nuget config and restore as distinct layers
COPY HousingSearchListener/HousingSearchListener.csproj ./HousingSearchListener/
COPY HousingSearchListener.Tests/HousingSearchListener.Tests.csproj ./HousingSearchListener.Tests/
COPY /NuGet.Config /root/.nuget/NuGet/NuGet.Config
RUN dotnet restore ./HousingSearchListener/HousingSearchListener.csproj
RUN dotnet restore ./HousingSearchListener.Tests/HousingSearchListener.Tests.csproj

# Copy everything else and build
COPY . .

RUN dotnet build -c Release -o out HousingSearchListener/HousingSearchListener.csproj
RUN dotnet build -c debug -o out HousingSearchListener.Tests/HousingSearchListener.Tests.csproj

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
