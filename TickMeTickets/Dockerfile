FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 50082
EXPOSE 44382

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY TickMeTickets/TickMeTickets.csproj TickMeTickets/
COPY TickMeHelpers/TickMeHelpers.csproj TickMeHelpers/
RUN dotnet restore TickMeTickets/TickMeTickets.csproj
COPY . .
WORKDIR /src/TickMeTickets
RUN dotnet build TickMeTickets.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish TickMeTickets.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TickMeTickets.dll"]
