using SwiftShip.Web.Data;
using SwiftShip.Web.Filters;

var builder = WebApplication.CreateBuilder(args);

// The connection string lives in appsettings.json (and, on a real machine, in user secrets).
// It is never written in the code.
Db.Configure(builder.Configuration.GetConnectionString("SwiftShip"));

builder.Services.AddControllersWithViews(options =>
{
    // every POST is checked against the anti-forgery token, so a page cannot be submitted
    // from another site
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Users are kept in the session after they log in.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".SwiftShip.Session";
});

builder.Services.AddHttpContextAccessor();

// One service object per request: it opens the connection only when it needs it.
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ShipmentService>();
builder.Services.AddScoped<CourierService>();
builder.Services.AddScoped<BranchService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
