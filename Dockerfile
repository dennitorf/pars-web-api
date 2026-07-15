#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Src/KellyServices.PARS.WebApi/KellyServices.PARS.WebApi.csproj", "Src/KellyServices.PARS.WebApi/"]
COPY ["Src/KellyServices.PARS.Application/KellyServices.PARS.Application.csproj", "Src/KellyServices.PARS.Application/"]
COPY ["Src/KellyServices.PARS.Persistence/KellyServices.PARS.Persistence.csproj", "Src/KellyServices.PARS.Persistence/"]
COPY ["Src/KellyServices.PARS.Domain/KellyServices.PARS.Domain.csproj", "Src/KellyServices.PARS.Domain/"]
COPY ["Src/KellyServices.PARS.Infrastructure/KellyServices.PARS.Infrastructure.csproj", "Src/KellyServices.PARS.Infrastructure/"]
RUN dotnet restore "Src/KellyServices.PARS.WebApi/KellyServices.PARS.WebApi.csproj"
COPY . .
WORKDIR "/src/Src/KellyServices.PARS.WebApi"
RUN dotnet build "KellyServices.PARS.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KellyServices.PARS.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KellyServices.PARS.WebApi.dll"]
