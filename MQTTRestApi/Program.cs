using Microsoft.EntityFrameworkCore;
using MQTTRestApi.Services;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddSingleton<IRedisService, RedisService>();

builder.Services.AddControllers();

var cs = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = ServerVersion.AutoDetect(cs);

builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(cs, serverVersion));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        builder.Configuration["Redis:ConnectionString"]!
    )
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

var mqttService = app.Services.GetRequiredService<IMqttService>();
await mqttService.ConnectAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
