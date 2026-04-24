FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OmniBizAI.slnx ./
COPY src/OmniBizAI.Domain/OmniBizAI.Domain.csproj src/OmniBizAI.Domain/
COPY src/OmniBizAI.Application/OmniBizAI.Application.csproj src/OmniBizAI.Application/
COPY src/OmniBizAI.Infrastructure/OmniBizAI.Infrastructure.csproj src/OmniBizAI.Infrastructure/
COPY src/OmniBizAI.WebAPI/OmniBizAI.WebAPI.csproj src/OmniBizAI.WebAPI/
RUN dotnet restore OmniBizAI.slnx

COPY src/ src/
WORKDIR /src/src/OmniBizAI.WebAPI
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN adduser --disabled-password --gecos "" appuser
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OmniBizAI.WebAPI.dll"]
