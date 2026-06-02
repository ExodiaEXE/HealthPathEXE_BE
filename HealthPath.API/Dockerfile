# Giai đoạn 1: Sử dụng SDK để khôi phục thư viện và biên dịch
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Sao chép file giải pháp và các file dự án để chạy restore trước
COPY *.slnx ./
COPY HealthPath.API/*.csproj ./HealthPath.API/
COPY HealthPath.Tests/*.csproj ./HealthPath.Tests/
RUN dotnet restore

# Sao chép toàn bộ code và xuất bản (publish)
COPY . ./
RUN dotnet publish HealthPath.API/HealthPath.API.csproj -c Release -o /app/out

# Giai đoạn 2: Môi trường Runtime siêu nhẹ để chạy thực tế
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HealthPath.API.dll"]