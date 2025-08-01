# Giai đoạn build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file dự án và khôi phục dependency
COPY QuanLyDatHang.csproj ./
RUN dotnet restore

# Copy toàn bộ source code và build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Giai đoạn runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "QuanLyDatHang.dll"]
