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

        /// <summary>
        /// Hàm chính: tạo user thường + member, xoá data Q&A cũ, seed lại data đẹp
        /// Gọi từ DependencyInjection.EnsureSeededAsync
        /// </summary>
        public static async Task SeedAsync(
            ApplicationDbContext ctx,
            UserManager<ApplicationUser> userManager)
        {
            // B1: đảm bảo có vài user thường + Member tương ứng
            await EnsureDemoMembersAsync(ctx, userManager);

            // B2: xoá sạch dữ liệu Q&A cũ (không đụng tới Member / AspNetUsers)
            await ResetQnADataAsync(ctx);

            // B3: seed lại Categories, Tags, Posts, Comments, Votes
            await SeedQnADataAsync(ctx);
        }

        /// <summary>
        /// Nếu bảng Members đang rỗng thì tạo 5 user thường + 5 member tương ứng
        /// </summary>
        private static async Task EnsureDemoMembersAsync(
    ApplicationDbContext ctx,
    UserManager<ApplicationUser> userManager)
        {
            // Nếu đã có Member thì thôi, không tạo nữa
            if (await ctx.Members.AnyAsync())
                return;

            // ✔ Thêm 1 admin + vài user thường
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
                // 1. Tạo ApplicationUser
                var user = await userManager.FindByEmailAsync(du.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = du.Email,
                        Email = du.Email,
                        EmailConfirmed = true
                    };

                    var password = du.IsAdmin ? adminPassword : userPassword;

                    var result = await userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Không tạo được demo user {du.Email}: {errors}");
                    }
                }

                // 2. Tạo Member tương ứng
                if (!await ctx.Members.AnyAsync(m => m.UserId == user.Id))
                {
                    ctx.Members.Add(new Member
                    {
                        UserId = user.Id,
                        DisplayName = du.DisplayName,
                        Bio = du.IsAdmin
                                            ? "Tài khoản admin demo cho hệ thống Q&A."
                                            : "Tài khoản user demo để test Q&A.",
                        IsAdministrator = du.IsAdmin,
                        IsModerator = false,
                        TrustLevelId = 1 // bạn chỉnh lại cho khớp dữ liệu TrustLevel
                    });
                }
            }

            await ctx.SaveChangesAsync();
        }


        /// <summary>
        /// Xoá sạch dữ liệu Q&A cũ nhưng KHÔNG xoá Member / AspNetUsers
        /// </summary>
        private static async Task ResetQnADataAsync(ApplicationDbContext ctx)
        {
            // Xoá theo thứ tự tránh lỗi FK

            var commentVotes = await ctx.CommentVotes.ToListAsync();
            ctx.CommentVotes.RemoveRange(commentVotes);

            var commentRevisions = await ctx.CommentRevisions.ToListAsync();
            ctx.CommentRevisions.RemoveRange(commentRevisions);

            var comments = await ctx.Comments.ToListAsync();
            ctx.Comments.RemoveRange(comments);

            var postVotes = await ctx.PostVotes.ToListAsync();
            ctx.PostVotes.RemoveRange(postVotes);

            var postRevisions = await ctx.PostRevisions.ToListAsync();
            ctx.PostRevisions.RemoveRange(postRevisions);

            var postTags = await ctx.PostTags.ToListAsync();
            ctx.PostTags.RemoveRange(postTags);

            var notifications = await ctx.Notifications.ToListAsync();
            ctx.Notifications.RemoveRange(notifications);

            var posts = await ctx.Posts.ToListAsync();
            ctx.Posts.RemoveRange(posts);

            var tags = await ctx.Tags.ToListAsync();
            ctx.Tags.RemoveRange(tags);

            var categories = await ctx.Categories.ToListAsync();
            ctx.Categories.RemoveRange(categories);

            // Nếu bạn có PostAttachments DbSet thì mở thêm đoạn này:
            // var attachments = await ctx.PostAttachments.ToListAsync();
            // ctx.PostAttachments.RemoveRange(attachments);

            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Seed Categories, Tags, Posts, PostTags, Comments, Votes
        /// </summary>
        private static async Task SeedQnADataAsync(ApplicationDbContext context)
        {
            var members = await context.Members
                .OrderBy(m => m.Id)
                .Take(20)
                .ToListAsync();

            if (!members.Any())
                throw new InvalidOperationException("Không có Member nào sau khi seed demo users.");

            // ========== Categories ==========
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new() { Name = "C#",                  Slug = "csharp",             DisplayOrder = 1 },
                    new() { Name = "ASP.NET Core",       Slug = "asp-net-core",       DisplayOrder = 2 },
                    new() { Name = "Frontend",           Slug = "frontend",           DisplayOrder = 3 },
                    new() { Name = "Database & SQL",     Slug = "database-sql",       DisplayOrder = 4 },
                    new() { Name = "Docker & DevOps",    Slug = "docker-devops",      DisplayOrder = 5 },
                    new() { Name = "Power BI & DAX",     Slug = "power-bi-dax",       DisplayOrder = 6 },
                    new() { Name = "Software Design",    Slug = "software-design",    DisplayOrder = 7 },
                    new() { Name = "Khác",               Slug = "others",             DisplayOrder = 8 },
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            var allCategories = await context.Categories.ToListAsync();

            // ========== Tags ==========
            if (!await context.Tags.AnyAsync())
            {
                var tags = new List<Tag>
                {
                    new() { Name = "c#",              Slug = "csharp" },
                    new() { Name = ".net",           Slug = "dotnet" },
                    new() { Name = "asp.net-core",   Slug = "asp-net-core" },
                    new() { Name = "ef-core",        Slug = "ef-core" },
                    new() { Name = "javascript",     Slug = "javascript" },
                    new() { Name = "html",           Slug = "html" },
                    new() { Name = "css",            Slug = "css" },
                    new() { Name = "react",          Slug = "react" },
                    new() { Name = "sql-server",     Slug = "sql-server" },
                    new() { Name = "docker",         Slug = "docker" },
                    new() { Name = "kubernetes",     Slug = "kubernetes" },
                    new() { Name = "power-bi",       Slug = "power-bi" },
                    new() { Name = "dax",            Slug = "dax" },
                    new() { Name = "clean-architecture", Slug = "clean-architecture" },
                    new() { Name = "api",            Slug = "api" },
                };

                await context.Tags.AddRangeAsync(tags);
                await context.SaveChangesAsync();
            }

            var allTags = await context.Tags.ToListAsync();

            // ========== Posts ==========
            if (!await context.Posts.AnyAsync())
            {
                var posts = new List<Post>();
                var postTags = new List<PostTag>();

                int numberOfPosts = 40;

                for (int i = 0; i < numberOfPosts; i++)
                {
                    var author = RandomPick(members);
                    var category = RandomPick(allCategories);

                    var title = i switch
                    {
                        < 10 => $"[C#] Hỏi về async/await và deadlock ({i + 1})",
                        < 20 => $"[EF Core] Khác biệt AsNoTracking và Tracking ({i + 1})",
                        < 30 => $"[SQL] Tối ưu query join nhiều bảng ({i + 1})",
                        _ => $"[Frontend] Tổ chức lại CSS/JS cho dự án Q&A ({i + 1})"
                    };

                    var body = GeneratePostBodySample(title, author.DisplayName);

                    var post = new Post
                    {
                        Title = title,
                        Body = body,
                        AuthorId = author.Id,
                        CategoryId = category.Id
                    };

                    posts.Add(post);
                }

                await context.Posts.AddRangeAsync(posts);
                await context.SaveChangesAsync();

                var seededPosts = await context.Posts.ToListAsync();

                foreach (var post in seededPosts)
                {
                    var numTags = _random.Next(2, 5);
                    var tagSample = allTags.OrderBy(_ => Guid.NewGuid())
                                           .Take(numTags)
                                           .ToList();

                    foreach (var tag in tagSample)
                    {
                        postTags.Add(new PostTag
                        {
                            PostId = post.Id,
                            TagId = tag.Id
                        });
                    }
                }

                await context.PostTags.AddRangeAsync(postTags);
                await context.SaveChangesAsync();
            }

            var allPostsForComment = await context.Posts.ToListAsync();

            // ========== Comments ==========
            if (!await context.Comments.AnyAsync())
            {
                var comments = new List<Comment>();

                foreach (var post in allPostsForComment)
                {
                    var numComments = _random.Next(2, 6);
                    for (int i = 0; i < numComments; i++)
                    {
                        var author = RandomPick(members);
                        var content = GenerateCommentBodySample(i);

                        comments.Add(new Comment
                        {
                            PostId = post.Id,
                            AuthorId = author.Id,
                            Body = content
                        });
                    }
                }

                await context.Comments.AddRangeAsync(comments);
                await context.SaveChangesAsync();
            }

            var allComments = await context.Comments.ToListAsync();

            // ========== Votes ==========
            if (!await context.PostVotes.AnyAsync() && !await context.CommentVotes.AnyAsync())
            {
                var postVotes = new List<PostVote>();
                var commentVotes = new List<CommentVote>();

                // vote cho post
                foreach (var post in allPostsForComment)
                {
                    // loại bỏ tác giả khỏi danh sách voter
                    var possibleVoters = members
                        .Where(m => m.Id != post.AuthorId)
                        .OrderBy(_ => Guid.NewGuid()) // random order
                        .ToList();

                    if (!possibleVoters.Any())
                        continue;

                    // số lượng vote: 3–10 nhưng không vượt quá số voter
                    var numVotes = _random.Next(3, 10);
                    numVotes = Math.Min(numVotes, possibleVoters.Count);

                    for (int i = 0; i < numVotes; i++)
                    {
                        var voter = possibleVoters[i]; // lấy lần lượt, không trùng

                        int value = _random.NextDouble() < 0.85 ? 1 : -1;

                        postVotes.Add(new PostVote
                        {
                            PostId = post.Id,
                            MemberId = voter.Id,
                            Value = value
                        });
                    }
                }

                // vote cho comment
                foreach (var c in allComments)
                {
                    var possibleVoters = members
                        .Where(m => m.Id != c.AuthorId)
                        .OrderBy(_ => Guid.NewGuid())
                        .ToList();

                    if (!possibleVoters.Any())
                        continue;

                    var numVotes = _random.Next(0, 5);
                    numVotes = Math.Min(numVotes, possibleVoters.Count);

                    for (int i = 0; i < numVotes; i++)
                    {
                        var voter = possibleVoters[i];

                        int value = _random.NextDouble() < 0.9 ? 1 : -1;

                        commentVotes.Add(new CommentVote
                        {
                            CommentId = c.Id,
                            MemberId = voter.Id,
                            Value = value
                        });
                    }
                }


                await context.PostVotes.AddRangeAsync(postVotes);
                await context.CommentVotes.AddRangeAsync(commentVotes);
                await context.SaveChangesAsync();

                // ========== Post Revisions ==========
                if (!await context.PostRevisions.AnyAsync())
                {
                    var revisions = new List<PostRevision>();
                    foreach (var post in allPostsForComment)
                    {
                        // random 0–2 revisions cho mỗi post
                        int numRevs = _random.Next(1, 3);

                        for (int i = 0; i < numRevs; i++)
                        {
                            var editor = RandomPick(members);

                            revisions.Add(new PostRevision
                            {
                                PostId = post.Id,
                                EditorId = editor.Id,
                                BeforeTitle = post.Title,
                                AfterTitle = post.Title + $" (chỉnh sửa {i + 1})",
                                BeforeBody = post.Body,
                                AfterBody = post.Body + $"\n\n_Update lần {i + 1}: thêm nội dung mẫu._",
                                Summary = "Demo chỉnh sửa post"
                            });
                        }
                    }

                    await context.PostRevisions.AddRangeAsync(revisions);
                    await context.SaveChangesAsync();
                }

                // ========== Comment Revisions ==========
                if (!await context.CommentRevisions.AnyAsync())
                {
                    var commentRevs = new List<CommentRevision>();

                    foreach (var c in allComments)
                    {
                        int numRevs = _random.Next(0, 2); // nhiều comment không chỉnh sửa → tự nhiên hơn

                        for (int i = 0; i < numRevs; i++)
                        {
                            var editor = RandomPick(members);

                            commentRevs.Add(new CommentRevision
                            {
                                CommentId = c.Id,
                                EditorId = editor.Id,
                                BeforeBody = c.Body,
                                AfterBody = c.Body + $" (chỉnh sửa lần {i + 1})",
                                Summary = "Demo chỉnh sửa comment"
                            });
                        }
                    }

                    await context.CommentRevisions.AddRangeAsync(commentRevs);
                    await context.SaveChangesAsync();
                }

            }
        }

        // ========== Helpers ==========

        private static T RandomPick<T>(IList<T> list)
        {
            return list[_random.Next(list.Count)];
        }

        private static string GeneratePostBodySample(string title, string authorName)
        {
            return
$@"**Câu hỏi:** {title}

Em đang làm dự án Q&A giống StackOverflow, dùng kiến trúc Clean Architecture (.NET).

Hiện tại em gặp vấn đề:
- Chưa rõ cách seed data đẹp để demo
- Chưa rõ cách tổ chức FE/BE cho dễ maintain
- Chưa rõ cách đóng gói Docker (API + SQL Server)

Mong mọi người gợi ý hướng tiếp cận, best practice và các keyword nên học thêm.

**Stack hiện tại:**
- Backend: .NET, EF Core, SQL Server
- Frontend: HTML/CSS/JS, Bootstrap
- Mục tiêu: demo đồ án Q&A nội bộ

_(Tạo bởi: {authorName})_";
        }

        private static string GenerateCommentBodySample(int index)
        {
            return index switch
            {
                0 => "Bạn có thể dùng EF Core + migration để quản lý schema, seed data ở Infrastructure.",
                1 => "Mình nghĩ bạn nên tách rõ layer: Domain / Application / Infrastructure / API / FE.",
                2 => "Khi demo, nên đóng gói bằng Docker Compose để bạn bè/giảng viên chạy cho dễ.",
                3 => "Nhớ phân trang (paging), sort, filter cho danh sách câu hỏi để UX tốt hơn.",
                _ => "Ý tưởng hệ thống Q&A nội bộ kết hợp AI trả lời theo tài liệu công ty là khá hay đấy."
            };
        }
    }
}
