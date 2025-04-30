using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.VisualBasic;
using ToDoApi;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("tododbWEB");
var connectionString2 = builder.Configuration.GetConnectionString("tododb2");
builder.Services.AddDbContext<ToDoDbContext>(
    options =>
    options.UseMySql(
    connectionString
    , new MySqlServerVersion(new Version(8, 0, 25))));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ToDo API",
        Description = "An ASP.NET Core Web API for managing ToDo items",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Example Contact",
            Url = new Uri("https://example.com/contact")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri("https://example.com/license")
        }
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.Use((context, next) =>
{
    Console.WriteLine($"Received request: {context.Request.Method} {context.Request.Path}");
    return next.Invoke();
});

app.MapPost("/addTask", async (Item item, ToDoDbContext dbContext) =>
{
    item.IsComplete = false;
    dbContext.Items.Add(item);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/items/{item.Id}", item);
});

app.MapDelete("/items/{id}", async (int id, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); 
    if (item is null) return Results.NotFound(); 

    dbContext.Items.Remove(item); 
    await dbContext.SaveChangesAsync();
    return Results.NoContent(); 
});

app.MapPut("/setCompleted/{id}", async (int id, bool isComplete, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); 
    if (item is null) return Results.NotFound(); 

    item.IsComplete = isComplete; 
    await dbContext.SaveChangesAsync(); 
    return Results.NoContent(); 
});

app.MapGet("/items", async (ToDoDbContext dbContext) =>
{
    var items = await dbContext.Items.ToListAsync(); 
    return Results.Ok(items); 
});

app.MapGet("/items/{id}", async (int id, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); 
    if (item is null) return Results.NotFound(); 
    return Results.Ok(item); 
});
var port = Environment.GetEnvironmentVariable("PORT") ?? "5001"; 
app.Run($"http://0.0.0.0:{port}");
