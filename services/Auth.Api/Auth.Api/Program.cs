using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


// Connection with Postgres auth instance
builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));

// Identity User Configuration
builder.Services.AddIdentity<User, IdentityRole>(opt =>
{
    opt.Password.RequiredLength = 8;
    opt.Password.RequireNonAlphanumeric = true;
    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    opt.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()                    // be able to add roles
.AddRoleManager<RoleManager<IdentityRole>>() // be able to make use of RoleManager (creating... roles)
.AddEntityFrameworkStores<AuthDbContext>()    // providing our context to the identity system
.AddSignInManager<SignInManager<User>>()     // make use of sign in manager in order to sign in user
.AddUserManager<UserManager<User>>()         // make use of user manager in order to create user
.AddDefaultTokenProviders();                 // to be able to create tokens for email confirmation

// Jwt Bearer Configuration with Assymetric keys
var jwtSettings = builder.Configuration.GetSection("Jwt");
var rsaKeyProvider = new RsaKeyProvider(builder.Configuration); // temp instance just to build validation params
builder.Services.AddSingleton(rsaKeyProvider);
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new RsaSecurityKey(rsaKeyProvider.PublicKey),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
