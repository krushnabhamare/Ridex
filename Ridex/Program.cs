using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Ridex.Data;
using Ridex.Hubs;
using Ridex.Models;
using Ridex.Services;

var builder = WebApplication.CreateBuilder(args);


// Database Connection


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("condb")));



// MVC


builder.Services.AddControllersWithViews();



// SignalR


builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();


// Background Services


builder.Services.AddHostedService<RideAutoCancelService>();



// Identity Configuration


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();



// Cookie Configuration


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";

    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    options.SlidingExpiration = true;
});



// Custom Services


builder.Services.AddScoped<EmailService>();


var app = builder.Build();



// Error Handling


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error");



// Middleware


app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();



// Routing


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



// SignalR Hub Mapping


app.MapHub<RideHub>("/rideHub");


app.Run();