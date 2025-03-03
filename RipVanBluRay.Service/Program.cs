using RipVanBluRay;
using RipVanBluRay.Hubs;
using RipVanBluRay.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new SharedState());

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024 * 4; // 4MB
});
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapControllers();
app.MapHub<RipHub>("/hubs/rip");

app.Run();