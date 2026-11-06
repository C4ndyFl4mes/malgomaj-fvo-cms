using System.Text;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.API.Data;
using Server.API.Exceptions;
using Server.API.Routes.Internal.Page.Editor;
using Server.API.Routes.Internal.Page.List;
using Server.API.Routes.Internal.Page.Save;
using Server.UI;
using Server.UI.States;
using Server.API.Entities;
using Microsoft.AspNetCore.Identity;
using Server.API.Routes.Internal.Page.Publish;
using Server.API.Routes.Internal.Page.Redact;
using Server.API.Routes.Internal.Page.Remove;
using Server.API.Routes.Internal.Authentication.SignUp;
using Server.API.Routes.Internal.Authentication.SignIn;
using Server.API.Routes.Internal.Authentication;
using Server.API.Validators;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Server.Services;
using Microsoft.AspNetCore.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#region Load secrets from either /run/secrets or .secrets in the project root.
if (Directory.Exists("/run/secrets"))
{
    builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
}
else
{
    string contentRoot = builder.Environment.ContentRootPath;
    if (Directory.Exists(Path.GetFullPath(Path.Combine(contentRoot, "..", ".secrets"))))
    {
        builder.Configuration.AddKeyPerFile(Path.GetFullPath(Path.Combine(contentRoot, "..", ".secrets")), optional: true);
        Console.WriteLine("Loaded secrets from the .secrets directory.");
    }
    else
    {
        throw new InvalidOperationException("Secrets directory not found. Please ensure that either /run/secrets or .secrets in project root exists.");
    }
}

// Load the database connection string from secrets and set it in the configuration.
string? dbConnectionString = builder.Configuration["db_connection_string.txt"];
if (!string.IsNullOrEmpty(dbConnectionString))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = dbConnectionString;
    Console.WriteLine("Database connection string loaded from secrets.");
}
#endregion

// register the AppDbContect with the connection string from configuration.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFastEndpoints().SwaggerDocument();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<ComponentExceptionHandler>();

// States:
builder.Services.AddScoped<NavigationState>();

// Data services:
builder.Services.AddScoped<SignInData>();
builder.Services.AddScoped<SignUpData>();
builder.Services.AddScoped<EditorGetData>();
builder.Services.AddScoped<PageSaveData>();
builder.Services.AddScoped<PageListGetData>();
builder.Services.AddScoped<PagePublishData>();
builder.Services.AddScoped<PageRedactData>();
builder.Services.AddScoped<PageRemoveData>();

// Validator services:
builder.Services.AddScoped<IValidator<UserModel>, UserModelValidator>();

#region Authentication and Authorization
builder.Services.AddScoped<UserSessionValidator>();
builder.Services.AddScoped<AuthenticationStateProvider, CmsAuthenticationStateProvider>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-Cms.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.LoginPath = "/admin";
        options.AccessDeniedPath = "/access-denied";

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("InternalUsers", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrator", "Editor");
    })
    .AddPolicy("AdministratorsOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrator");
    });

#endregion

#region WebbApplication 

WebApplication app = builder.Build();

using var scope = app.Services.CreateScope();
AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.MigrateAsync();

if (!await dbContext.Roles.AnyAsync())
{
    RoleEntity adminRole = new()
    {
        Id = Guid.NewGuid(),
        Name = "Administrator",
        Description = "Har fullständig åtkomst till alla funktioner och inställningar."
    };
    RoleEntity editorRole = new()
    {
        Id = Guid.NewGuid(),
        Name = "Editor",
        Description = "Kan redigera innehåll och hantera vissa inställningar."
    };
    dbContext.Roles.AddRange(adminRole, editorRole);
    await dbContext.SaveChangesAsync();
}

if (!await dbContext.Users.AnyAsync())
{
    string adminEmail = builder.Configuration["admin_email.txt"] ??
        throw new InvalidOperationException("Admin email is not configured.");
    string adminPassword = builder.Configuration["admin_password.txt"] ??
        throw new InvalidOperationException("Admin password is not configured.");
    string roleId = (await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator"))?.Id.ToString() ??
        throw new InvalidOperationException("Admin role is not configured in the database.");

    UserEntity adminUser = new()
    {
        Id = Guid.NewGuid(),
        Email = adminEmail,
        PasswordHash = new PasswordHasher<UserEntity>().HashPassword(null!, adminPassword),
        RoleId = Guid.Parse(roleId),
        Role = null! // Will automatically be set by EF Core due to the RoleId FK.
    };

    dbContext.Users.Add(adminUser);
    await dbContext.SaveChangesAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    ui =>
    {
        ui.UseStatusCodePagesWithReExecute(
            "/admin/not-found",
            createScopeForStatusCodePages: true
        );
    }
);

app.UseMiddleware<GlobalExceptionHandler>();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints().UseSwaggerGen();

app.Run();

#endregion
