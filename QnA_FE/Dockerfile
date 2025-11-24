# Sử dụng Nginx bản alpine cho nhẹ
FROM nginx:alpine

# Xóa config mặc định của Nginx (tùy chọn, nhưng nên làm để tránh xung đột)
RUN rm -rf /usr/share/nginx/html/*

# Copy toàn bộ code FE vào thư mục gốc của Nginx
# Lưu ý: Dấu chấm (.) ở đây là thư mục QnA_FE (vì file này nằm trong QnA_FE)
COPY . /usr/share/nginx/html

# Port mặc định của Nginx là 80
EXPOSE 80

# Start Nginx
CMD ["nginx", "-g", "daemon off;"]