# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj từng layer để tận dụng cache
COPY QnA_BE/QnA_BE.csproj ./QnA_BE/
COPY Application/*.csproj ./Application/
COPY Domain/*.csproj ./Domain/
COPY Infrastructure/*.csproj ./Infrastructure/

# Restore packages cho project BE
RUN dotnet restore "QnA_BE/QnA_BE.csproj"

# Copy toàn bộ source
COPY . .

# Build & publish
WORKDIR /src/QnA_BE
RUN dotnet publish -c Release -o /app/publish

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# BE lắng nghe cổng 7006
EXPOSE 7006
ENV ASPNETCORE_URLS=http://+:7006

ENTRYPOINT ["dotnet", "QnA_BE.dll"]
