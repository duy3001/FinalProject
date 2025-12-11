

# 📝 **README.md – FinalProject (QnA System)**


# # 📌 FinalProject – QnA System (BE + FE + Docker Compose)

Hệ thống Hỏi & Đáp nội bộ gồm:

* **Backend**: .NET 8, Clean Architecture, Identity, JWT
* **Frontend**: HTML/CSS/JS, chạy bằng Nginx
* **Database**: SQL Server 2022 (Docker)
* **Seeder**: Tự tạo User, Member, Category, Tag, Post, Comment…
* **Docker Compose**: chạy tất cả chỉ với 1 lệnh

---

# ## 🚀 1. Yêu cầu môi trường

Cần cài trước:

* **Docker Desktop**
* **Git**
* Không cần cài .NET, SQL Server hay Node.

---

# ## 📥 2. Clone dự án

```bash
git clone https://github.com/YOUR_REPO_HERE.git
cd FinalProject
```

> ⚠️ **Quan trọng:** Tất cả lệnh docker phải chạy trong thư mục `FinalProject` (vì docker-compose.yml nằm ở đây).

---

# ## 🧱 3. Chạy bằng Docker

### 👉 3.1. Build toàn bộ containers

```bash
docker compose build
```

### 👉 3.2. Chạy hệ thống

```bash
docker compose up -d
```

### 👉 3.3. Kiểm tra trạng thái

```bash
docker compose ps
```

Bạn sẽ thấy 4 container:

| Container         | Chức năng                            |
| ----------------- | ------------------------------------ |
| `qna_db`          | SQL Server                           |
| `qna_backend`     | .NET API                             |
| `qna_frontend`    | FE (Nginx)                           |
| `qna-db-migrator` | Tự chạy Migration + Seed (auto exit) |

---

# ## 📊 4. Truy cập hệ thống

### 👉 Backend API (Swagger)

```
http://localhost:7006/swagger
```

### 👉 Frontend (Nginx)

```
http://localhost:3000
```

Trang login nằm ở:

```
http://localhost:3000/page/auth/login.html
```

---

# ## 🔐 5. Tài khoản demo (Seeder tự tạo)

### Admin

```
Email: admin@example.com
Password: Admin@123
```

### User thường

```
user1@example.com / User@123
user2@example.com / User@123
user3@example.com / User@123
user4@example.com / User@123
```

---

# ## 🗃 6. Kiểm tra Database (tuỳ chọn)

Nếu muốn kết nối SQL từ SSMS / Azure Data Studio:

```
Server: localhost,1436
User: sa
Password: Sa123456!
Database: QnA
```

> Port `1436` là port máy thật → map sang port 1433 trong container.

---

# ## 🔁 7. Update code & chạy lại backend/frontend

Sau khi pull code mới, chỉ cần:

```bash
docker compose down
docker compose build
docker compose up -d
```

---

# ## 🐞 8. Debug lỗi thường gặp

### ❌ FE không đăng nhập được → Lý do CORS

Giải pháp: BE đã cấu hình **AllowAnyOrigin()** → restart BE là được.

---

### ❌ Lỗi DB chưa có bảng → Seeder chưa chạy

Run:

```bash
docker compose logs db-migrator
```

---

### ❌ Lỗi BE không chạy

Xem log:

```bash
docker compose logs backend
```

---

# ## ❤️ 9. Mọi thứ đã cấu hình sẵn

* Không cần chạy migration thủ công
* Không cần tạo user
* Không cần tạo DB
* FE/BE đã kết nối đúng
* CORS đã bật cho dev
* Chỉ cần `docker compose up -d` là chạy

---

# 📌 KẾT LUẬN

Repo này cho phép bất kỳ ai:

1. clone
2. build
3. mở FE + BE
4. login bằng tài khoản seed

→ là chạy được toàn bộ dự án Q&A.

---

Nếu bạn muốn mình viết phần **giải thích kiến trúc (Clean Architecture)** hoặc **flow cài đặt** cho phần báo cáo tiểu luận → mình viết cho luôn.
