using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
// Import the classes from pathHelper.cs
using PathHelper;

public class ElevationService
{
    public static async Task AddElevationDataAsync()
    {
        string inputFilePath = "c:\\Users\\colen\\OneDrive\\Grad School\\CSCI 8910 - Capstone\\capstone\\capstone-project\\frontend\\src\\assets\\I29-North_smoothed.json";
        string outputFilePath = "c:\\Users\\colen\\OneDrive\\Grad School\\CSCI 8910 - Capstone\\capstone\\capstone-project\\frontend\\src\\assets\\I29-North-with-elevation.json";

        string jsonString = File.ReadAllText(inputFilePath);
        var featureCollection = JsonSerializer.Deserialize<FeatureCollection>(jsonString);

        using HttpClient client = new HttpClient();
        foreach (var feature in featureCollection.Features)
        {
            if (feature.Geometry.Type == "Point" && feature.Geometry.Coordinates.Count == 2)
            {
                double latitude = feature.Geometry.Coordinates[1];
                double longitude = feature.Geometry.Coordinates[0];

                double elevation = await GetElevationAsync(client, latitude, longitude);
                feature.Properties["elevation"] = elevation.ToString();
            }
        }

        string outputJson = JsonSerializer.Serialize(featureCollection, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputFilePath, outputJson);

        Console.WriteLine($"Elevation data written to {outputFilePath}");
    }

    public static async Task<double> GetElevationAsync(HttpClient client, double latitude, double longitude)
    {
        string apiUrl = $"https://epqs.nationalmap.gov/v1/json?x={longitude}&y={latitude}&units=Feet&output=json";
        var response = await client.GetAsync(apiUrl);
        
        if (response.StatusCode == HttpStatusCode.MovedPermanently ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.RedirectMethod)
        {
            var redirectUrl = response.Headers?.Location?.ToString();
            response = await client.GetAsync(redirectUrl);
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {jsonResponse}");
        var elevationResponse = JsonSerializer.Deserialize<ElevationResponse>(jsonResponse);
        return elevationResponse?.value ?? 0;
    }
}

public class ElevationResponse
{
    public Location location { get; set; }
    public int locationId { get; set; }
    public double value { get; set; }
    public int rasterId { get; set; }
    public int resolution { get; set; }
}

public class Location
{
    public double x { get; set; }
    public double y { get; set; }
    public SpatialReference spatialReference { get; set; }
}

public class SpatialReference
{
    public int wkid { get; set; }
    public int latestWkid { get; set; }
}
