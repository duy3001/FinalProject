using Application.Common.Interfaces;
using Domain.Common.Authorization;
using Infrastructure.Auth;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Seed;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext (SQL Server)
            var cs = configuration.GetConnectionString("DefaultConnection")!;
            Console.WriteLine(">>> Connection string = " + cs);
            services.AddDbContext<ApplicationDbContext>(opt =>
            {
                opt.UseSqlServer(cs, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            });
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

            // Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // MediatR (Application assembly)
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
                Assembly.Load("Application")));

            // JWT
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            var jwt = configuration.GetSection("Jwt").Get<JwtSettings>()!;
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opt =>
            {
                opt.RequireHttpsMetadata = false;
                opt.SaveToken = true;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
                };
            });
            // Authorization dùng permission claim
            services.AddAuthorization(options =>
            {
                // ví dụ: user thường hỏi/đáp/comment/vote/search
                options.AddPolicy("CanAskQuestion", p => p.RequireClaim("permission", Permissions.QnA.AskQuestion));
                options.AddPolicy("CanAnswer", p => p.RequireClaim("permission", Permissions.QnA.Answer));
                options.AddPolicy("CanComment", p => p.RequireClaim("permission", Permissions.QnA.Comment));
                options.AddPolicy("CanVote", p => p.RequireClaim("permission", Permissions.QnA.Vote));
                options.AddPolicy("CanSearch", p => p.RequireClaim("permission", Permissions.QnA.Search));
                options.AddPolicy("CanDelete", p => p.RequireClaim("permission", Permissions.QnA.Delete));

                // các policy admin – sau này làm tính năng admin thì gắn
                options.AddPolicy("CanViewStats", p => p.RequireClaim("permission", Permissions.Admin.ViewStats));
                options.AddPolicy("CanUploadPolicyDocs", p => p.RequireClaim("permission", Permissions.Admin.UploadPolicyDocs));
                options.AddPolicy("CanManagePolicyDataset", p => p.RequireClaim("permission", Permissions.Admin.ManagePolicyDataset));
            });

            // Interceptors
            services.AddScoped<AuditingInterceptor>();
            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<TenantInterceptor>();
            services.AddScoped<IIdentityService, IdentityService>();


            // Helpers
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ITenantProvider, TenantProvider>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IFileStorage, LocalFileStorage>();

            return services;
        }

        public static async Task EnsureSeededAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var ctx = sp.GetRequiredService<ApplicationDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();

            // apply migration
            await ctx.Database.MigrateAsync();

            // 1. seed Role + Permission + admin user demo
            await IdentitySeeder.SeedAsync(userManager, roleManager);

            // 2. seed Members + dữ liệu Q&A (posts, comments, votes…)
            // (QnASeeder của mình dùng userManager để tạo Member thường)
            await QnASeeder.SeedAsync(ctx, userManager);
        }
    }
}
