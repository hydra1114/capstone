using System.Text.Json;
using PathHelper;

var builder = WebApplication.CreateBuilder(args);

var root = "C:\\Users\\colen\\OneDrive\\Grad School\\CSCI 8910 - Capstone\\capstone\\capstone-project\\frontend\\src\\assets";
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

//PathHelper.PathHelper.FindCommonPoints();
//ElevationService.AddElevationDataAsync().Wait();
var pathToFind = JsonSerializer.Deserialize<FeatureCollection>(File.ReadAllText(Path.Join(root, "I29-North-with-elevation.json")));
var firstPoint = pathToFind.Features[0].Geometry.Coordinates;
var lastPoint = pathToFind.Features[pathToFind.Features.Count - 1].Geometry.Coordinates;
var worked = int.TryParse(pathToFind.Features[0].Properties["ref"], out var firstMileMarker);
worked = int.TryParse(pathToFind.Features[pathToFind.Features.Count - 1].Properties["ref"], out var lastMileMarker) && worked;
Console.WriteLine($"Distance: {Math.Abs(firstMileMarker - lastMileMarker)}");
List<PathFinder.Point> points = new List<PathFinder.Point>();
if (worked) {
    points = PathFinder.PathFinder.FindPath(new PathFinder.Point (firstPoint[0], firstPoint[1]), new PathFinder.Point (lastPoint[0], lastPoint[1]), Math.Abs(firstMileMarker - lastMileMarker));
    Console.WriteLine($"Length of Path: {points.Count} Features: {pathToFind.Features.Count}");
    PathFinder.PathFinder.FindPointsAlongPath(pathToFind.Features, points);
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
