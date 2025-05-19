using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//var connectionString = "Server=DESKTOP-7NQF8CQ\\SQLEXPRESS;Database=EFPractice01;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddDbContext<EFPractice01.Data.CourseContext>(options => options.UseSqlServer(connectionString));
var app = builder.Build();

//app.MapGet("/", () => "Hello World!");
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
