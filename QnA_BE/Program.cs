using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using QnA_BE.Swagger;

var builder = WebApplication.CreateBuilder(args);

// ====== (1) CẤU HÌNH URL KHI CHẠY DEV (tuỳ chọn) ======
// Nếu muốn cố định cổng dev: http://localhost:7006
// builder.WebHost.UseUrls("http://localhost:7006");

// ====== (2) DỊCH VỤ ======
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt(); // extension của bạn

// CORS: đọc từ appsettings "Cors:AllowedOrigins"; fallback một số origin hay dùng
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[]
    {
        "http://localhost:5173",   // Vite
        "http://127.0.0.1:5173",
        "http://localhost:5500",   // Live Server
        "http://localhost:3000",   // FE chạy trong Docker
        "http://localhost"         // fallback chung
    };


builder.Services.AddCors(opt =>
{
    opt.AddPolicy("DevCors", p => p
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
});

// (Tuỳ) log request/response đơn giản khi debug
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                      HttpLoggingFields.ResponsePropertiesAndHeaders;
});

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ====== (3) MIDDLEWARE ======
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QnA API v1");
        c.DisplayRequestDuration();
    });

    // Dev thường dùng HTTP để tránh lỗi cert => KHÔNG redirect HTTPS trong dev
}
else
{
    // Prod/Stage mới bật redirect HTTPS (nếu bạn có chứng chỉ thật)
    app.UseHttpsRedirection();
}

app.UseHttpLogging();

// CORS phải đặt TRƯỚC Auth
app.UseCors("DevCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.Services.EnsureSeededAsync();

app.Run();
