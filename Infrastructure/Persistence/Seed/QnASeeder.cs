using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seed
{
    public static class QnASeeder
    {
        private static readonly Random _random = new();

        // =============================
        // HÀM CHÍNH
        // =============================
        public static async Task SeedAsync(
            ApplicationDbContext ctx,
            UserManager<ApplicationUser> userManager)
        {
            // B0 — seed bảng TrustLevels trước (tránh lỗi FK)
            await EnsureTrustLevelsAsync(ctx);

            // B1 — seed Users + Members
            await EnsureDemoMembersAsync(ctx, userManager);

            // B2 — reset sạch dữ liệu Q&A cũ
            await ResetQnADataAsync(ctx);

            // B3 — seed Categories / Tags / Posts / Comments / Votes...
            await SeedQnADataAsync(ctx);
        }

        // =============================
        // SEED TRUST LEVELS
        // =============================
        private static async Task EnsureTrustLevelsAsync(ApplicationDbContext ctx)
        {
            if (await ctx.TrustLevels.AnyAsync())
                return;

            var levels = new List<TrustLevel>
            {
                new() { Name = "Newbie" },
                new() { Name = "Member" },
                new() { Name = "Trusted" },
                new() { Name = "Admin" }
            };

            await ctx.TrustLevels.AddRangeAsync(levels);
            await ctx.SaveChangesAsync();
        }

        // =============================
        // SEED USER / MEMBER
        // =============================
        private static async Task EnsureDemoMembersAsync(
            ApplicationDbContext ctx,
            UserManager<ApplicationUser> userManager)
        {
            if (await ctx.Members.AnyAsync())
                return;

            // Lấy TrustLevel đầu tiên
            var defaultTrustLevelId = await ctx.TrustLevels
                .OrderBy(t => t.Id)
                .Select(t => t.Id)
                .FirstAsync();

            var demoUsers = new[]
            {
                new { Email = "admin@example.com", DisplayName = "Admin",      IsAdmin = true  },
                new { Email = "user1@example.com", DisplayName = "User One",   IsAdmin = false },
                new { Email = "user2@example.com", DisplayName = "User Two",   IsAdmin = false },
                new { Email = "user3@example.com", DisplayName = "User Three", IsAdmin = false },
                new { Email = "user4@example.com", DisplayName = "User Four",  IsAdmin = false },
            };

            const string adminPassword = "Admin@123";
            const string userPassword = "User@123";

            foreach (var du in demoUsers)
            {
                // 1. tạo ApplicationUser
                var user = await userManager.FindByEmailAsync(du.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = du.Email,
                        Email = du.Email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(
                        user,
                        du.IsAdmin ? adminPassword : userPassword);

                    if (!result.Succeeded)
                    {
                        var err = string.Join("; ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Không tạo user demo {du.Email}: {err}");
                    }
                }

                // 2. tạo Member tương ứng
                if (!await ctx.Members.AnyAsync(m => m.UserId == user.Id))
                {
                    ctx.Members.Add(new Member
                    {
                        UserId = user.Id,
                        DisplayName = du.DisplayName,
                        Bio = du.IsAdmin
                            ? "Tài khoản admin demo."
                            : "Tài khoản user demo.",
                        IsAdministrator = du.IsAdmin,
                        IsModerator = false,
                        TrustLevelId = defaultTrustLevelId
                    });
                }
            }

            await ctx.SaveChangesAsync();
        }


        // =============================
        // RESET POST / COMMENT / VOTE...
        // =============================
        private static async Task ResetQnADataAsync(ApplicationDbContext ctx)
        {
            ctx.CommentVotes.RemoveRange(await ctx.CommentVotes.ToListAsync());
            ctx.CommentRevisions.RemoveRange(await ctx.CommentRevisions.ToListAsync());
            ctx.Comments.RemoveRange(await ctx.Comments.ToListAsync());
            ctx.PostVotes.RemoveRange(await ctx.PostVotes.ToListAsync());
            ctx.PostRevisions.RemoveRange(await ctx.PostRevisions.ToListAsync());
            ctx.PostTags.RemoveRange(await ctx.PostTags.ToListAsync());
            ctx.Notifications.RemoveRange(await ctx.Notifications.ToListAsync());
            ctx.Posts.RemoveRange(await ctx.Posts.ToListAsync());
            ctx.Tags.RemoveRange(await ctx.Tags.ToListAsync());
            ctx.Categories.RemoveRange(await ctx.Categories.ToListAsync());

            await ctx.SaveChangesAsync();
        }


        // =============================
        // SEED Q&A DATA
        // =============================
        private static async Task SeedQnADataAsync(ApplicationDbContext context)
        {
            var members = await context.Members.OrderBy(m => m.Id).Take(20).ToListAsync();
            if (!members.Any())
                throw new InvalidOperationException("Không có Member nào để seed Q&A.");

            // ========== Categories ==========
            if (!await context.Categories.AnyAsync())
            {
                await context.Categories.AddRangeAsync(new[]
                {
                    new Category { Name = "C#", Slug = "csharp", DisplayOrder = 1 },
                    new Category { Name = "ASP.NET Core", Slug = "asp-net-core", DisplayOrder = 2 },
                    new Category { Name = "Frontend", Slug = "frontend", DisplayOrder = 3 },
                    new Category { Name = "Database & SQL", Slug = "database-sql", DisplayOrder = 4 },
                    new Category { Name = "Docker & DevOps", Slug = "docker-devops", DisplayOrder = 5 },
                    new Category { Name = "Power BI & DAX", Slug = "power-bi-dax", DisplayOrder = 6 },
                    new Category { Name = "Software Design", Slug = "software-design", DisplayOrder = 7 },
                    new Category { Name = "Khác", Slug = "others", DisplayOrder = 8 },
                });

                await context.SaveChangesAsync();
            }

            var categories = await context.Categories.ToListAsync();

            // ========== Tags ==========
            if (!await context.Tags.AnyAsync())
            {
                await context.Tags.AddRangeAsync(new[]
                {
                    new Tag { Name = "c#", Slug = "csharp" },
                    new Tag { Name = ".net", Slug = "dotnet" },
                    new Tag { Name = "asp.net-core", Slug = "asp-net-core" },
                    new Tag { Name = "ef-core", Slug = "ef-core" },
                    new Tag { Name = "javascript", Slug = "javascript" },
                    new Tag { Name = "html", Slug = "html" },
                    new Tag { Name = "css", Slug = "css" },
                    new Tag { Name = "react", Slug = "react" },
                    new Tag { Name = "sql-server", Slug = "sql-server" },
                    new Tag { Name = "docker", Slug = "docker" },
                    new Tag { Name = "kubernetes", Slug = "kubernetes" },
                    new Tag { Name = "power-bi", Slug = "power-bi" },
                    new Tag { Name = "dax", Slug = "dax" },
                    new Tag { Name = "clean-architecture", Slug = "clean-architecture" },
                    new Tag { Name = "api", Slug = "api" },
                });

                await context.SaveChangesAsync();
            }

            var tags = await context.Tags.ToListAsync();

            // ========== Posts ==========
            if (!await context.Posts.AnyAsync())
            {
                var posts = new List<Post>();

                for (int i = 0; i < 30; i++)
                {
                    var author = RandomPick(members);
                    var category = RandomPick(categories);

                    var title = $"Câu hỏi mẫu số {i + 1}";
                    var body = $"Nội dung mẫu cho câu hỏi {i + 1}.\nTác giả: {author.DisplayName}";

                    posts.Add(new Post
                    {
                        Title = title,
                        Body = body,
                        AuthorId = author.Id,
                        CategoryId = category.Id
                    });
                }

                await context.Posts.AddRangeAsync(posts);
                await context.SaveChangesAsync();
            }

            var seededPosts = await context.Posts.ToListAsync();

            // ========== Tags for Post ==========
            if (!await context.PostTags.AnyAsync())
            {
                var pt = new List<PostTag>();

                foreach (var p in seededPosts)
                {
                    foreach (var t in tags.OrderBy(_ => Guid.NewGuid()).Take(3))
                    {
                        pt.Add(new PostTag { PostId = p.Id, TagId = t.Id });
                    }
                }

                await context.PostTags.AddRangeAsync(pt);
                await context.SaveChangesAsync();
            }

            // ========== Comments ==========
            if (!await context.Comments.AnyAsync())
            {
                var comments = new List<Comment>();
                foreach (var post in seededPosts)
                {
                    for (int i = 0; i < _random.Next(2, 5); i++)
                    {
                        comments.Add(new Comment
                        {
                            PostId = post.Id,
                            AuthorId = RandomPick(members).Id,
                            Body = $"Bình luận mẫu {i + 1} cho post {post.Id}"
                        });
                    }
                }

                await context.Comments.AddRangeAsync(comments);
                await context.SaveChangesAsync();
            }
        }

        // =============================
        // HELPERS
        // =============================
        private static T RandomPick<T>(IList<T> list)
            => list[_random.Next(list.Count)];
    }
}
