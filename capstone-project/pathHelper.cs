using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public class GeoJsonFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; }

    [JsonPropertyName("geometry")]
    public Geometry Geometry { get; set; }
}

public class Geometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; }
}

public class FeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("features")]
    public List<GeoJsonFeature> Features { get; set; }
}

public class PathHelper
{
    public static void FindCommonPoints()
    {
        string filePath = "c:\\Users\\colen\\OneDrive\\Grad School\\CSCI 8910 - Capstone\\Repo\\capstone-project\\frontend\\src\\assets\\I680-curve.json";
        var averageFeatureCollection = GetAverageCoordinates(filePath);

        string outputFilePath = "c:\\Users\\colen\\OneDrive\\Grad School\\CSCI 8910 - Capstone\\Repo\\capstone-project\\frontend\\src\\assets\\I680-curve_smoothed.json";
        string jsonString = JsonSerializer.Serialize(averageFeatureCollection, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputFilePath, jsonString);

        Console.WriteLine($"Average coordinates written to {outputFilePath}");
    }

    public static FeatureCollection GetAverageCoordinates(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        var featureCollection = JsonSerializer.Deserialize<FeatureCollection>(jsonString);

        var motorwayJunctions = featureCollection.Features
            .Where(f => f.Properties.ContainsKey("highway") && f.Properties["highway"] == "motorway_junction" && f.Properties.ContainsKey("ref"))
            .GroupBy(f => f.Properties["ref"]);

        var averageFeatures = new List<GeoJsonFeature>();
        foreach (var group in motorwayJunctions)
        {
            var coordinates = group.Select(f => f.Geometry.Coordinates).ToList();
            var averageX = coordinates.Average(c => c[0]);
            var averageY = coordinates.Average(c => c[1]);
            var exitRef = group.First().Properties["ref"];
            averageFeatures.Add(new GeoJsonFeature
            {
                Type = "Feature",
                Properties = new Dictionary<string, string> { { "ref", exitRef }, { "highway", "motorway_junction" } },
                Geometry = new Geometry { Type = "Point", Coordinates = new List<double> { averageX, averageY } }
            });
        }

        return new FeatureCollection
        {
            Type = "FeatureCollection",
            Features = averageFeatures
        };
    }
}