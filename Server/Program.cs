using System.Text;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.API.Data;
using Server.UI;

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

#region Authentication and Authorization
string secretKey = builder.Configuration["secret_key.txt"] ?? throw new InvalidOperationException("Secret key is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["issuer.txt"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["audience.txt"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuerSigningKey = true
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("accessToken", out string? token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });
#endregion

#region WebbApplication 

WebApplication app = builder.Build();

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