FROM nginx:alpine

# Xóa html default
RUN rm -rf /usr/share/nginx/html/*

# Copy toàn bộ FE (assets, page, partials...)
COPY . /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
