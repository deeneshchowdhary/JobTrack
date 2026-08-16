using JobTrack.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<JobTrackDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(origin =>
                origin.StartsWith("http://localhost:"));
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AngularApp");
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow
}));

app.Run();