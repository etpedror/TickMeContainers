FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 50080
EXPOSE 44380

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY TickMe/TickMe.csproj TickMe/
COPY TickMeHelpers/TickMeHelpers.csproj TickMeHelpers/
RUN dotnet restore TickMe/TickMe.csproj
COPY . .
WORKDIR /src/TickMe
RUN dotnet build TickMe.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish TickMe.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TickMe.dll"]
