FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS build
WORKDIR /source
COPY global.json .
COPY src/Rentavia.Domain/Rentavia.Domain.csproj src/Rentavia.Domain/
COPY src/Rentavia.Application/Rentavia.Application.csproj src/Rentavia.Application/
COPY src/Rentavia.Infrastructure/Rentavia.Infrastructure.csproj src/Rentavia.Infrastructure/
COPY src/Rentavia.Web/Rentavia.Web.csproj src/Rentavia.Web/
RUN dotnet restore src/Rentavia.Web/Rentavia.Web.csproj
COPY src/ src/
RUN dotnet publish src/Rentavia.Web/Rentavia.Web.csproj -c Release --no-restore -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish .
RUN mkdir -p /app/wwwroot/uploads/vehiculos /app/keys && chown -R app:app /app/wwwroot/uploads /app/keys
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Rentavia.Web.dll"]
