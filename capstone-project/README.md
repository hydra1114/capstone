# Capstone Project Backend

This folder contains the backend code for the Capstone Project. The main entry point is `Program.cs`, which serves as a minimal web API and performs pathfinding operations using helper classes.

## Overview of `Program.cs`

The `Program.cs` file is responsible for:

1. **Setting up a Web API**:
   - It includes an endpoint `/weatherforecast` that generates random weather forecasts for demonstration purposes.
   - Swagger is configured for API documentation and testing.

2. **Pathfinding Operations**:
   - It reads a GeoJSON file (`I29-North-with-elevation.json` or `I680-curve_smoothed_sample.json`) containing geographical features.
   - It calculates the distance between the first and last points in the file based on mile markers.
   - It uses the `PathFinder` helper class to find a path between the first and last points and identifies points along the path.

3. **Console Output**:
   - Outputs the calculated distance and the length of the path to the console.

## Helper Classes

### `PathHelper`
- Provides utility methods for working with paths and geographical data.
- Example methods:
  - `AddElevationAsync`: Makes an API call to EPQS and adds elevation data to paths.

### `PathFinder`
- Contains methods for pathfinding and working with geographical points.
- Example methods:
  - `FindPath`: Finds a path between two points based on the given distance.
  - `FindPointsAlongPath`: Places points along a given path.

## How to Run the Program

### Prerequisites

1. Install the [.NET SDK](https://dotnet.microsoft.com/download).
2. Ensure the required GeoJSON file (`I29-North-with-elevation.json`) is located in the `frontend/src/assets` directory.

### Steps to Run

1. Clone the repository:
   ```bash
   git clone <repository-url>

2. Navigate to the project directory:
    `cd capstone-project`

3. Build the project:
    `dotnet build`

4. Run the console application:
    `dotnet run`

### Console Output
When the program runs, it will output the following information to the console:

- The distance between the first and last points in the GeoJSON file.
- The length of the path found and the number of features in the file.

### Modifying the Input File
To use a different GeoJSON file for pathfinding:

1. Replace the file name in `Program.cs`

    `var pathToFind = JsonSerializer.Deserialize<FeatureCollection>(File.ReadAllText(Path.Join(root, "your-new-file.json")));`

2. Ensure the new file is in `frontend/src/assets`.

3. Rebuild and rerun the application using the commands above.