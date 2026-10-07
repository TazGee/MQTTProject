using MQTTRestApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MqttService>();

builder.Services.AddControllers();

var app = builder.Build();

var mqttService = app.Services.GetRequiredService<MqttService>();
await mqttService.ConnectAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
