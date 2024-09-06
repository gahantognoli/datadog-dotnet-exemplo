FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ApiDataDog.csproj", "."]
RUN dotnet restore "./ApiDataDog.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ApiDataDog.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ApiDataDog.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final

WORKDIR /app

ARG DD_API_KEY

ENV DD_API_KEY=$DD_API_KEY \
    DD_AGENT_MAJOR_VERSION=7 \
    DD_APM_ENABLED=true \
    DD_HOSTNAME="ApiDataDog" \
    DD_SITE="us5.datadoghq.com" \
    DD_APM_NON_LOCAL_TRAFFIC=true \
    DD_ENV="dev" \
    DD_VERSION="1.0.0" \
    DD_SERVICE="ApiDataDog" \
    DD_PROFILING_ENABLED=true \
    DD_LOGS_INJECTION=true \
    DD_APPSEC_ENABLED=true \
    DD_IAST_ENABLED=true \
    DD_APPSEC_SCA_ENABLED=true

RUN apt-get update \
    && apt-get install -y curl dpkg \
    && apt-get clean \
    && TRACER_VERSION=$(curl -s https://api.github.com/repos/DataDog/dd-trace-dotnet/releases/latest | grep tag_name | cut -d '"' -f 4 | cut -c2-) \
    && curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb \
    && dpkg -i ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb || apt-get -f install -y \
    && rm ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog

RUN apt-get update && \
    apt-get install -y ca-certificates

COPY --from=datadog/serverless-init:1 /datadog-init /app/datadog-init

COPY --from=publish /app/publish .

CMD [ "/app/datadog-init", "dotnet", "ApiDataDog.dll"]