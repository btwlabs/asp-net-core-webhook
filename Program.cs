var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Configure app settings and environment variables
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

try
{
    var app = builder.Build();
    if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); } 
// Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseDefaultFiles(); // Enable serving up default files

    app.UseStaticFiles(); // Enable serving up static files

    app.UseRouting();

    app.UseAuthorization();

    app.UseHttpsRedirection();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // Write the exception to the console (you may want to use a more sophisticated logging solution for real-world projects).
    Console.WriteLine(ex.ToString());
    throw;
}
