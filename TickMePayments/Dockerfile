FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 50081
EXPOSE 44381

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY TickMePayments/TickMePayments.csproj TickMePayments/
COPY TickMeHelpers/TickMeHelpers.csproj TickMeHelpers/
RUN dotnet restore TickMePayments/TickMePayments.csproj
COPY . .
WORKDIR /src/TickMePayments
RUN dotnet build TickMePayments.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish TickMePayments.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TickMePayments.dll"]
