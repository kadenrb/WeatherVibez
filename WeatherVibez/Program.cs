using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WeatherVibez.Models;
using WeatherVibez.CustomClaims;
using WeatherVibez.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add DB Context
builder.Services.AddDbContext<UserDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure cookie authentication for MVC
builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/User/Login";
	options.AccessDeniedPath = "/User/AccessDenied";
});

// Add HttpClient for API calls
builder.Services.AddHttpClient();

// Set Stripe API key
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Configure CORS to allow requests from specified origins
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(builder =>
	{
		builder.WithOrigins("https://localhost:7203", "https://localhost:3000")  // Add any front-end URL here
			   .AllowAnyMethod()
			   .AllowAnyHeader()
			   .AllowCredentials();
	});
});

// Add services for controllers and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession();

// Add JWT token authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
		};
	});

// Add custom services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<BalanceService>();

// Add Authorization services
builder.Services.AddAuthorization();

// Add custom claims service for user roles
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, CustomClaims>();

// Add Identity services
builder.Services.AddIdentity<User, IdentityRole>()
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<UserDbContext>()
	.AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
}
else
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MVC route handling
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Ensure roles are created at startup
using (var scope = app.Services.CreateScope())
{
	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
	var roles = new[] { "Admin", "User", "Premium" };
	foreach (var role in roles)
	{
		var roleExist = await roleManager.RoleExistsAsync(role);
		if (!roleExist)
		{
			await roleManager.CreateAsync(new IdentityRole(role));
		}
	}
}

app.UseSession();

app.Run();

