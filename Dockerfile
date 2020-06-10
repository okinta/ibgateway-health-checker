FROM mcr.microsoft.com/dotnet/core/sdk:5.0-alpine

COPY ibgateway-health-checker.csproj /app/ibgateway-health-checker.csproj
RUN dotnet restore /app/ibgateway-health-checker.csproj

COPY . /app
RUN dotnet build -c Release /app/ibgateway-health-checker.csproj

FROM mcr.microsoft.com/dotnet/core/runtime:5.0-alpine

COPY --from=0 /app/bin/Release/net5.0 /app
RUN ln -s /app/ibgateway-health-checker /usr/local/bin/ibgateway-health-checker

CMD ["ibgateway-health-checker", "--help"]
