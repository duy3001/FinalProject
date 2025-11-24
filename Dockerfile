# --- Giai đoạn 1: Build ---
# Sử dụng SDK để build code. Chọn version phù hợp (ví dụ .NET 6, 7 hoặc 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ source code từ thư mục gốc vào image
# Điều này quan trọng vì QnA_BE cần tham chiếu đến Domain, Application, Infrastructure
COPY . .

# Restore các package cho project chính
RUN dotnet restore "QnA_BE/QnA_BE.csproj"

# Build và Publish
# -c Release: Cấu hình tối ưu
# -o /app/publish: Thư mục đầu ra
RUN dotnet publish "QnA_BE/QnA_BE.csproj" -c Release -o /app/publish

# --- Giai đoạn 2: Runtime ---
# Sử dụng Runtime nhẹ hơn để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy file đã build từ giai đoạn 1
COPY --from=build /app/publish .

# Mở port 7006 (mặc định của .NET 8) 
EXPOSE 7006

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "QnA_BE.dll"]