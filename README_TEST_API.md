# 🚀 HƯỚNG DẪN TEST API QUẢN LÝ ĐẶT HÀNG

## 📋 Tổng quan
API Quản lý đặt hàng cung cấp các chức năng:
- **Xác thực**: Đăng ký, đăng nhập với JWT
- **Quản lý cửa hàng**: CRUD cửa hàng
- **Quản lý menu**: CRUD món ăn
- **Quản lý admin**: Quản lý người dùng, thống kê
- **SignalR**: Chat real-time
- **Email**: Gửi email thông báo

## 🛠️ Cách chạy API

### 1. Khởi động API
```bash
cd QuanLyDatHang
dotnet run
```

### 2. Kiểm tra API đang chạy
- URL: `https://localhost:7001`
- Swagger UI: `https://localhost:7001/swagger`

## 📝 Các cách test API

### 1. Sử dụng file HTTP (Khuyến nghị)
File: `API_Test_Collection.http`

**Cách sử dụng:**
1. Mở file trong VS Code với extension "REST Client"
2. Click "Send Request" trên từng request
3. Thay thế các biến `{{variable}}` bằng giá trị thực

### 2. Sử dụng PowerShell Script
File: `Test_API_PowerShell.ps1`

**Cách sử dụng:**
```powershell
# Chạy với tham số mặc định
.\Test_API_PowerShell.ps1

# Chạy với tham số tùy chỉnh
.\Test_API_PowerShell.ps1 -BaseUrl "https://localhost:7001" -Username "myuser" -Password "mypass"
```

### 3. Sử dụng Postman
Import các request từ file HTTP vào Postman

### 4. Sử dụng Swagger UI
Truy cập `https://localhost:7001/swagger` để test trực tiếp

## 🔐 Quy trình test xác thực

### Bước 1: Đăng ký tài khoản
```http
POST https://localhost:7001/api/auth/register
Content-Type: application/json

{
  "username": "testuser",
  "email": "test@example.com",
  "password": "Test123!",
  "fullName": "Nguyễn Văn Test",
  "phoneNumber": "0123456789"
}
```

### Bước 2: Đăng nhập lấy token
```http
POST https://localhost:7001/api/auth/login
Content-Type: application/json

{
  "username": "testuser",
  "password": "Test123!"
}
```

### Bước 3: Sử dụng token cho các API khác
```http
GET https://localhost:7001/api/stores
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## 🏪 Test quản lý cửa hàng

### Lấy danh sách cửa hàng
```http
GET https://localhost:7001/api/stores
Authorization: Bearer {{authToken}}
```

### Tạo cửa hàng mới
```http
POST https://localhost:7001/api/stores
Authorization: Bearer {{authToken}}
Content-Type: application/json

{
  "name": "Nhà hàng Test",
  "description": "Nhà hàng phục vụ món ăn ngon",
  "address": "123 Đường ABC, Quận 1, TP.HCM",
  "phoneNumber": "0123456789",
  "email": "nhahang@example.com",
  "openingHours": "08:00-22:00",
  "categoryId": "{{categoryId}}"
}
```

### Cập nhật cửa hàng
```http
PUT https://localhost:7001/api/stores/{{storeId}}
Authorization: Bearer {{authToken}}
Content-Type: application/json

{
  "name": "Nhà hàng Test - Đã cập nhật",
  "description": "Nhà hàng phục vụ món ăn ngon và chất lượng",
  "address": "456 Đường XYZ, Quận 2, TP.HCM",
  "phoneNumber": "0987654321",
  "email": "nhahangmoi@example.com",
  "openingHours": "07:00-23:00",
  "categoryId": "{{categoryId}}"
}
```

### Xóa cửa hàng
```http
DELETE https://localhost:7001/api/stores/{{storeId}}
Authorization: Bearer {{authToken}}
```

## 🍽️ Test quản lý menu

### Lấy menu của cửa hàng
```http
GET https://localhost:7001/api/menus/store/{{storeId}}
Authorization: Bearer {{authToken}}
```

### Tạo món ăn mới
```http
POST https://localhost:7001/api/menus
Authorization: Bearer {{authToken}}
Content-Type: application/json

{
  "name": "Phở Bò",
  "description": "Phở bò truyền thống Việt Nam",
  "price": 45000,
  "imageUrl": "https://example.com/pho-bo.jpg",
  "storeId": "{{storeId}}",
  "categoryId": "{{categoryId}}",
  "isAvailable": true
}
```

### Cập nhật món ăn
```http
PUT https://localhost:7001/api/menus/{{menuId}}
Authorization: Bearer {{authToken}}
Content-Type: application/json

{
  "name": "Phở Bò Đặc Biệt",
  "description": "Phở bò đặc biệt với nhiều loại thịt",
  "price": 55000,
  "imageUrl": "https://example.com/pho-bo-dac-biet.jpg",
  "storeId": "{{storeId}}",
  "categoryId": "{{categoryId}}",
  "isAvailable": true
}
```

### Xóa món ăn
```http
DELETE https://localhost:7001/api/menus/{{menuId}}
Authorization: Bearer {{authToken}}
```

## 👨‍💼 Test quản lý admin

### Lấy danh sách người dùng (Admin only)
```http
GET https://localhost:7001/api/admin/users
Authorization: Bearer {{adminToken}}
```

### Khóa/Mở khóa tài khoản
```http
PUT https://localhost:7001/api/admin/users/{{userId}}/lock
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "isLocked": true
}
```

### Lấy thống kê hệ thống
```http
GET https://localhost:7001/api/admin/statistics
Authorization: Bearer {{adminToken}}
```

## 🌤️ Test API mẫu

### Weather Forecast (không cần auth)
```http
GET https://localhost:7001/weatherforecast
```

## 💬 Test SignalR Chat

### Kết nối SignalR Hub
```javascript
// Sử dụng JavaScript client
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7001/chatHub?access_token=YOUR_JWT_TOKEN")
    .build();

connection.start().then(() => {
    console.log("Đã kết nối SignalR");
});
```

## 🔧 Cấu hình cần thiết

### 1. Database
- SQL Server: `PHAMCONGDAT`
- Database: `QuanLyDatHang`
- Connection string trong `appsettings.json`

### 2. Email Settings
- SMTP: Gmail
- Port: 587
- SSL: Enabled

### 3. JWT Settings
- Secret Key: Đã cấu hình
- Expiry: 60 phút

### 4. CORS
- Allowed Origins: localhost:3000, localhost:4200, localhost:5173

## 🚨 Xử lý lỗi thường gặp

### 1. Lỗi kết nối database
```
- Kiểm tra SQL Server đang chạy
- Kiểm tra connection string
- Chạy migration: dotnet ef database update
```

### 2. Lỗi JWT token
```
- Kiểm tra token có hợp lệ không
- Kiểm tra token đã hết hạn chưa
- Đăng nhập lại để lấy token mới
```

### 3. Lỗi CORS
```
- Kiểm tra origin trong CORS settings
- Thêm origin vào AllowedOrigins nếu cần
```

### 4. Lỗi 404 Not Found
```
- Kiểm tra URL endpoint có đúng không
- Kiểm tra API đang chạy trên port đúng
```

## 📊 Kiểm tra kết quả

### Response thành công
```json
{
  "success": true,
  "message": "Thao tác thành công",
  "data": { ... }
}
```

### Response lỗi
```json
{
  "success": false,
  "message": "Mô tả lỗi",
  "errors": [ ... ]
}
```

## 🎯 Checklist test

- [ ] API khởi động thành công
- [ ] Database kết nối được
- [ ] Đăng ký tài khoản
- [ ] Đăng nhập lấy token
- [ ] Lấy danh sách cửa hàng
- [ ] Tạo cửa hàng mới
- [ ] Cập nhật cửa hàng
- [ ] Xóa cửa hàng
- [ ] Lấy menu cửa hàng
- [ ] Tạo món ăn mới
- [ ] Cập nhật món ăn
- [ ] Xóa món ăn
- [ ] Test admin functions
- [ ] Test SignalR connection
- [ ] Test email service

## 📞 Hỗ trợ

Nếu gặp vấn đề, hãy:
1. Kiểm tra logs trong console
2. Kiểm tra database connection
3. Xem cấu hình trong `appsettings.json`
4. Chạy migration nếu cần: `dotnet ef database update` 