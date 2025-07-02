using NpvCalculator.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: Only allow Angular dev server
var allowedOrigins = new[] { "http://localhost:4200", "https://localhost:4200" };
builder.Services.AddNpvServices();
//Configurations for future improvements - caching etc
builder.Services.AddNpvServices(options =>
{
    options.MaxCashFlowPeriods = 50;
    options.EnableCaching = true;
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");

app.MapControllers();

app.Run();