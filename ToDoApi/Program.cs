using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.VisualBasic;
using ToDoApi;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("tododbWEB");
var connectionString2 = builder.Configuration.GetConnectionString("tododb2");
// var connectionString3 = "server=localhost;database=tododb2;user=root;password=Ee1357913579##;"; // חיבור לדאטה בייס
// Console.WriteLine($"Connection String: {connectionString}");
// הזרקת מסד נתונים
builder.Services.AddDbContext<ToDoDbContext>(
    options =>
    options.UseMySql(
    connectionString // חיבור לדאטה בייס
        // "server=b1yqwa5ijxosrh3ahwmi-mysql.services.clever-cloud.com;database=b1yqwa5ijxosrh3ahwmi;user=uznpkq0mzzvq1ayh;password=eKVNRzQ4LsKvwKn1ibK5;"
    , new MySqlServerVersion(new Version(8, 0, 25)))); // אתה יכול לשנות את הגרסה כאן


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//תפיסת שגיאות - הדפסה ודיבאג
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// בעיית הקורס
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

// מגדיר את Swagger
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

// מאפשר CORS
app.UseCors("AllowAll");

// middleware להדליק את הסווגר בסביבת הפיתוח
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty; // שמים את הסווגר בניתוב של ה-root
    });
}

// middleware ל-log של בקשות נכנסות
app.Use((context, next) =>
{
    Console.WriteLine($"Received request: {context.Request.Method} {context.Request.Path}");
    return next.Invoke();
});

// שינוי: הוספת id לנתיב של עדכון משימות
app.MapPost("/addTask", async (Item item, ToDoDbContext dbContext) =>
{
    item.IsComplete = false;
    dbContext.Items.Add(item); // הוסף את הפריט למסד הנתונים
    await dbContext.SaveChangesAsync(); // שמור את השינויים
    return Results.Created($"/items/{item.Id}", item); // החזר את התגובה עם המיקום של הפריט שנשמר
});

// שינוי: תיקון נתיב מחיקה
app.MapDelete("/items/{id}", async (int id, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); // חפש את הפריט לפי מזהה
    if (item is null) return Results.NotFound(); // אם לא נמצא, החזר 404

    dbContext.Items.Remove(item); // מחק את הפריט
    await dbContext.SaveChangesAsync(); // שמור את השינויים
    return Results.NoContent(); // החזר 204
});

// שינוי: הוספת id לנתיב של עדכון משימות
app.MapPut("/setCompleted/{id}", async (int id, bool isComplete, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); // חפש את הפריט לפי מזהה
    if (item is null) return Results.NotFound(); // אם לא נמצא, החזר 404

    item.IsComplete = isComplete; // עדכן את השדות הרצויים
    await dbContext.SaveChangesAsync(); // שמור את השינויים
    return Results.NoContent(); // החזר 204
});

// שליפת כל הפריטים
app.MapGet("/items", async (ToDoDbContext dbContext) =>
{
    var items = await dbContext.Items.ToListAsync(); // שלוף את כל הפריטים
    return Results.Ok(items); // החזר את הפריטים
});

// שינוי: תיקון נתיב של חיפוש פריט לפי מזהה
app.MapGet("/items/{id}", async (int id, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id); // חפש את הפריט לפי מזהה
    if (item is null) return Results.NotFound(); // אם לא נמצא, החזר 404
    return Results.Ok(item); // החזר את הפריט
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "5001"; // ברירת מחדל לפורט 5001
app.Run($"http://0.0.0.0:{port}");
