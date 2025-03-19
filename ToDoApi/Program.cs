
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ToDoApi;
var builder = WebApplication.CreateBuilder(args);
//הזרקת מסד נתונים
// הוסף את ה-mysql
builder.Services.AddDbContext<ToDoDbContext>
(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("tododb2"),
     new MySqlServerVersion(new Version(8, 0, 25))));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//בעיית הקורס
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
//מוסיף עוד אפשרויות לסווגר
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
//.קורס
app.UseCors("AllowAll"); // הוסף את השורה הזו

//middleware להדליק את הסווגר בסביבת הפיתוח

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//root שמים את הסווגר בניתוב של ה
if (builder.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}
//middleware מהבינה
// Add middleware to log incoming requests
/*
app.Use((context, next) =>
{
    Console.WriteLine($"Received request: {context.Request.Method} {context.Request.Path}");
    return next.Invoke();
});*/

app.MapPost("/addTask", async (Item item, ToDoDbContext dbContext) =>
{
    item.IsComplete=false;
    dbContext.Items.Add(item); // הוסף את הפריט למסד הנתונים
    await dbContext.SaveChangesAsync(); // שמור את השינויים
    return Results.Created($"/items/{item.Id}", item); // החזר את התגובה עם המיקום של הפריט שנשמר
});

app.MapDelete("/items/{id}", async (int id, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); // חפש את הפריט לפי מזהה
    if (item is null) return Results.NotFound(); // אם לא נמצא, החזר 404

    dbContext.Items.Remove(item); // מחק את הפריט
    await dbContext.SaveChangesAsync(); // שמור את השינויים
    return Results.NoContent(); // החזר 204
});
app.MapPut("/setCompleted/", async (int id, bool isComplete, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); // חפש את הפריט לפי מזהה
    if (item is null) return Results.NotFound(); // אם לא נמצא, החזר 404
     // עדכן את השדות הרצויים
    item.IsComplete=isComplete;
    await dbContext.SaveChangesAsync(); // שמור את השינויים
    return Results.NoContent(); // החזר 204
});
app.MapGet("/{id}", async (int id, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); // חפש את הפריט לפי מזהה
    if (item is null) return Results.NotFound(); // אם לא נמצא, החזר 404
    return Results.Ok(item); // החזר את הפריט
});

app.MapGet("/items", async (ToDoDbContext dbContext) =>
{
    var items = await dbContext.Items.ToListAsync(); // שלוף את כל הפריטים
    return Results.Ok(items); // החזר את הפריטים
});


app.Run();
