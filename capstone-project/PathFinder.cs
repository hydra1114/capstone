using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PathHelper; // Assuming the namespace of GeoJsonFeature is PathHelper
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer.Validate;

namespace PathFinder
{
    public class PathFinder
    {
        private static string root = "C:\\Users\\colen\\OneDrive\\Grad School\\CSCI 8910 - Capstone\\capstone\\capstone-project\\frontend\\src\\assets\\";
        public static List<Point> FindPath(Point start, Point end, double distance)
        {
            //var thing = DivideAndConquer(start, end, distance);
            var thing = GenerateGraph(start, end, distance, 11);
            thing.Insert(0, start);
            thing.Add(end);
            
            using HttpClient client = new HttpClient();
            Parallel.ForEach(thing, point =>
            {
                point.Elevation = ElevationService.GetElevationAsync(client, point.Y, point.X).Result;
            });

            //Task.WaitAll(thing); // Wait for the async task to complete
            // Serialize graph to GeoJSON format

            var path = FindAllPathsWithinDistance(
                BuildWeightedGraph(thing),
                0,
                thing.Count - 1,
                distance,
                0.005); // .05% deviation

            path.Sort((a, b) => a.ElevationChange.CompareTo(b.ElevationChange));
            var avgElevationChange = path.Average(p => p.ElevationChange);
            Console.WriteLine($"Num Paths: {path.Count} - Best Path: {path.First().TotalDistance} - Elevation Change: {path.First().ElevationChange} - Avg: {avgElevationChange}");
            var bestPath = new List<Point>();
            foreach(int i in path.First().Nodes)
            {
                bestPath.Add(thing[i]);
            }
            SaveGraphAsGeoJson(thing, Path.Join(root, "graph.json"));
            SaveGraphAsGeoJson(bestPath, Path.Join(root, "path.json"));
            return bestPath;
        }

        public static void SaveGraphAsGeoJson(List<Point> graph, string filePath)
        {
            Console.WriteLine($"Num Points: {graph.Count}");
            var geoJsonFeatures = graph.Select(point => new GeoJsonFeature
            {
                Type = "Feature",
                Properties = new Dictionary<string, string>(),
                Geometry = new PathHelper.Geometry
                {
                    Type = "Point",
                    Coordinates = new List<double> { point.X, point.Y }
                }
            }).ToList();

            var featureCollection = new FeatureCollection
            {
                Type = "FeatureCollection",
                Features = geoJsonFeatures
            };

            string geoJsonString = JsonSerializer.Serialize(featureCollection, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, geoJsonString);
        }

        private static List<Point> GenerateGraph(Point start, Point end, double distance, int numPoints)
        {
            var graph = new List<Point>();
            int numPointsEachSide = numPoints / 2;
            Console.WriteLine($"Start: {start.X}, {start.Y} - End: {end.X}, {end.Y} - Distance: {distance} - Actual Distance: {CalculateDistance(start, end)}");
            if (distance < 1)
            {
                return graph;
            }

            var middlePoint = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            double hypotenuseLength = distance / 2;
            var perpSlope = CalculatePerpendicularSlope(start, end);
                Console.WriteLine($"New Mid: {middlePoint.X}, {middlePoint.Y} - New Hyp: {hypotenuseLength} - Dist to Mid: {CalculateDistance(start, middlePoint)}");

            graph.AddRange(GetPointsAroundMiddle(start, middlePoint, hypotenuseLength, perpSlope, numPointsEachSide));
            double distToMid;
            Point newMiddlePoint;
            double newHypLength;
            for (int i = 1; i < numPointsEachSide; i++)
            {
                newMiddlePoint = GreatCircleInterpolate(start.Y, start.X, middlePoint.Y, middlePoint.X, (double)i / numPointsEachSide);
                newHypLength = hypotenuseLength * i / numPointsEachSide;
                //Console.WriteLine($"New Mid: {newMiddlePoint.X}, {newMiddlePoint.Y} - New Hyp: {newHypLength} - Dist to Mid: {CalculateDistance(start, newMiddlePoint)} - Ratio: {i / numPointsEachSide}");
                graph.AddRange(GetPointsAroundMiddle(start, newMiddlePoint, newHypLength, perpSlope, i));
            }
            //other side
            for (int i = 1; i < numPointsEachSide; i++)
            {
                newMiddlePoint = GreatCircleInterpolate(end.Y, end.X, middlePoint.Y, middlePoint.X, (double)i / numPointsEachSide);
                newHypLength = hypotenuseLength * i / numPointsEachSide;
                //Console.WriteLine($"New Mid: {newMiddlePoint.X}, {newMiddlePoint.Y} - New Hyp: {newHypLength} - Dist to Mid: {CalculateDistance(start, newMiddlePoint)} - Ratio: {i / numPointsEachSide}");
                graph.AddRange(GetPointsAroundMiddle(end, newMiddlePoint, newHypLength, perpSlope, i));
            }
          
            return graph;
        }

        private static IEnumerable<Point> GetPointsAroundMiddle(Point start,Point middlePoint, double hypotenuseLength, double perpSlope, int numPointsEachSide)
        {
            var graph = new List<Point>();
            for (int i = 0; i < numPointsEachSide; i++) 
            {
                // Calculate the offset distance from the middle point along the perpendicular slope
                double distanceFromMid = Math.Sqrt(hypotenuseLength * hypotenuseLength - Math.Pow(CalculateDistance(start, middlePoint), 2));
                
                // Calculate the new point along the line defined by middlePoint and perpSlope
                double bearing = Math.Atan(perpSlope) * (180.0 / Math.PI); // degrees
                double ratio = 1.0 / numPointsEachSide * (i + 1.0);
                var newPoint = DestinationPoint(middlePoint.Y, middlePoint.X, distanceFromMid * ratio, bearing);
                graph.Add(newPoint);
                //Console.WriteLine($"New Point: {newPoint.X}, {newPoint.Y} - Distance: {CalculateDistance(start, newPoint)} - Ratio: {ratio}");
            }

            //opposite direction
            for (int i = 0; i < numPointsEachSide; i++) 
            {
                // Calculate the offset distance from the middle point along the perpendicular slope
                double distanceFromMid = Math.Sqrt(hypotenuseLength * hypotenuseLength - Math.Pow(CalculateDistance(start, middlePoint), 2));
                
                // Calculate the new point along the line defined by middlePoint and perpSlope
                double bearing = (Math.Atan(perpSlope) * (180.0 / Math.PI) + 180) % 360 ; // degrees
                double ratio = 1.0 / numPointsEachSide * (i + 1.0);
                var newPoint = DestinationPoint(middlePoint.Y, middlePoint.X, distanceFromMid * ratio, bearing);
                graph.Add(newPoint);
                //Console.WriteLine($"New Point: {newPoint.X}, {newPoint.Y} - Distance: {CalculateDistance(start, newPoint)} - Ratio: {ratio} - opp");
            }
            
            graph.Add(middlePoint);
            return graph;
        }

        public static Point DestinationPoint(double lat, double lon, double distanceMiles, double bearingDegrees)
        {
            const double R = 3958.8; // Radius of Earth in miles

            var radianDistance = distanceMiles / R; // angular distance in radians
            var bearing = bearingDegrees * Math.PI / 180; // bearing in radians

            var latShifted = lat * Math.PI / 180;
            var lonShifted = lon * Math.PI / 180;

            var newLat = Math.Asin(Math.Sin(latShifted) * Math.Cos(radianDistance) +
                            Math.Cos(latShifted) * Math.Sin(radianDistance) * Math.Cos(bearing));

            var newLon = lonShifted + Math.Atan2(Math.Sin(bearing) * Math.Sin(radianDistance) * Math.Cos(latShifted),
                                    Math.Cos(radianDistance) - Math.Sin(latShifted) * Math.Sin(newLat));

            return new Point(newLon * 180 / Math.PI, newLat * 180 / Math.PI);
        }
        private static double CalculatePerpendicularSlope(Point start, Point end)
        {
            // Calculate the slope of the line from start to end
            double dx = start.X - end.X;
            double dy = start.Y - end.Y;
            if (dx == 0)
            {
                // If the line is vertical, the perpendicular slope is 0 (horizontal line)
                return 0;
            }

            if (dy == 0)
            {
                // If the line is horizontal, the perpendicular slope is undefined (vertical line)
                throw new InvalidOperationException("Perpendicular slope is undefined for a horizontal line.");
            }

            // The perpendicular slope is the negative reciprocal of the original slope
            return -dy / dx;
        }

        private static double CalculateDistance(Point point1, Point point2)
        {
            const double EarthRadiusMiles = 3963; // Earth's radius in miles

            double ToRadians(double angle) => Math.PI * angle / 180.0;

            double lat1 = ToRadians(point1.Y);
            double lon1 = ToRadians(point1.X);
            double lat2 = ToRadians(point2.Y);
            double lon2 = ToRadians(point2.X);

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusMiles * c;
        }

        public static Point GreatCircleInterpolate(double lat1, double lon1, double lat2, double lon2, double fraction)
        {
            // Convert to radians
            lat1 = DegreeToRadian(lat1);
            lon1 = DegreeToRadian(lon1);
            lat2 = DegreeToRadian(lat2);
            lon2 = DegreeToRadian(lon2);

            // Convert lat/lon to Cartesian coordinates
            double x1 = Math.Cos(lat1) * Math.Cos(lon1);
            double y1 = Math.Cos(lat1) * Math.Sin(lon1);
            double z1 = Math.Sin(lat1);

            double x2 = Math.Cos(lat2) * Math.Cos(lon2);
            double y2 = Math.Cos(lat2) * Math.Sin(lon2);
            double z2 = Math.Sin(lat2);

            // Angle between points (in radians)
            double dot = x1 * x2 + y1 * y2 + z1 * z2;
            double omega = Math.Acos(Math.Clamp(dot, -1.0, 1.0)); // clamp to avoid floating point errors

            if (Math.Abs(omega) < 1e-10)
            {
                // Points are too close, just return the first one
                return new Point (RadianToDegree(lon1), RadianToDegree(lat1));
            }

            // Slerp
            double sinOmega = Math.Sin(omega);
            double t1 = Math.Sin((1 - fraction) * omega) / sinOmega;
            double t2 = Math.Sin(fraction * omega) / sinOmega;

            double x = t1 * x1 + t2 * x2;
            double y = t1 * y1 + t2 * y2;
            double z = t1 * z1 + t2 * z2;

            // Convert back to lat/lon
            double lat = Math.Atan2(z, Math.Sqrt(x * x + y * y));
            double lon = Math.Atan2(y, x);

            return new Point (RadianToDegree(lon), RadianToDegree(lat));
        }

        private static double DegreeToRadian(double degree) => degree * Math.PI / 180.0;
        private static double RadianToDegree(double radian) => radian * 180.0 / Math.PI;


        public static List<Edge> BuildWeightedGraph(List<Point> points)
        {
            var edges = new List<Edge>();

            for (int i = 0; i < points.Count; i++)
            {
                //Console.WriteLine($"Point: {points[i].X}, {points[i].Y} - Elevation: {points[i].Elevation}");
                for (int j = i + 1; j < points.Count; j++)
                {
                    double distance = CalculateDistance(points[i], points[j]);
                    edges.Add(new Edge { From = i, To = j, Weight = distance, ElevationChange = Math.Abs(points[i].Elevation - points[j].Elevation) });
                    edges.Add(new Edge { From = j, To = i, Weight = distance, ElevationChange = Math.Abs(points[i].Elevation - points[j].Elevation) }); // If undirected graph
                }
            }
            return edges;
        }
        public class PathResult
        {
            public List<int> Nodes { get; set; } = new List<int>();
            public double TotalDistance { get; set; }
            public double ElevationChange { get; set; } = 0;
        }

        public static List<PathResult> FindAllPathsWithinDistance(
            List<Edge> edges,
            int start,
            int end,
            double targetDistance,
            double allowedDeviation = 0.1) // 10%
        {
            var graph = BuildAdjacencyList(edges);

            var results = new List<PathResult>();
            double minDistance = targetDistance * (1.0 - allowedDeviation);
            double maxDistance = targetDistance * (1.0 + allowedDeviation);

            Console.WriteLine($"Target Distance: {targetDistance} - Min: {minDistance} - Max: {maxDistance}");

            // Iterative DFS using a stack
            var stack = new Stack<(int Node, double DistanceSoFar, double ElevationChangeSoFar, List<int> Path)>();
            stack.Push((start, 0, 0, new List<int> { start }));

            while (stack.Count > 0)
            {
                var (current, distanceSoFar, elevationChangeSoFar, currentPath) = stack.Pop();

                if (current == end)
                {
                    if (distanceSoFar >= minDistance && distanceSoFar <= maxDistance)
                    {
                        if (results.Count < 15)
                        {
                            Console.WriteLine($"Path: {string.Join(" -> ", currentPath)} - Distance: {distanceSoFar} - Elevation Change: {elevationChangeSoFar}");
                        }
                        results.Add(new PathResult
                        {
                            Nodes = new List<int>(currentPath),
                            TotalDistance = distanceSoFar,
                            ElevationChange = elevationChangeSoFar
                        });

                        if (results.Count > 2000)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var edge in graph[current])
                    {
                        if (!currentPath.Contains(edge.To) && distanceSoFar + edge.Weight <= maxDistance)
                        {
                            var newPath = new List<int>(currentPath) { edge.To };
                            stack.Push((edge.To, distanceSoFar + edge.Weight, elevationChangeSoFar + edge.ElevationChange, newPath));
                        }
                    }
                }
            }

            return results;
        }

        private static Dictionary<int, List<Edge>> BuildAdjacencyList(List<Edge> edges)
        {
            var graph = new Dictionary<int, List<Edge>>();
            foreach (var edge in edges)
            {
                if (!graph.ContainsKey(edge.From))
                    graph[edge.From] = new List<Edge>();
                graph[edge.From].Add(edge);
            }
            return graph;
        }

        public static void FindPointsAlongPath(List<GeoJsonFeature> points, List<Point> path)
        {
            points.Sort((a, b) => a.Properties["ref"].CompareTo(b.Properties["ref"]));
            List<double> distances = new List<double>();
            List<Point> newPoints = new List<Point>();
            var first = int.Parse(points[0].Properties["ref"]);
            for (int i = 1; i < path.Count; i++)
            {
                var segmentLength = CalculateDistance(path[i - 1], path[i]);
                distances.Add(segmentLength);
            }

            for (int i = 1; i < points.Count; i++)
            {
                var nextpointMileMarker = int.Parse(points[i].Properties["ref"]);
                newPoints.Add(FindCoordinateAtDistance(path, distances, Math.Abs(nextpointMileMarker - first)));
            }
            newPoints.Insert(0, path.First());
            newPoints.Add(path.Last());
            
            for (int i = 0; i < points.Count; i++)
            {
                points[i].Geometry.Coordinates = new List<double> { newPoints[i].X, newPoints[i].Y };
            }

            var featureCollection = new FeatureCollection
            {
                Type = "FeatureCollection",
                Features = points
            };
            var filePath = Path.Join(root, "new-points.json");
            string geoJsonString = JsonSerializer.Serialize(featureCollection, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, geoJsonString);

            return;
        }

        public static Point InterpolateBetween(
            Point p1,
            Point p2,
            double fraction)
        {
            double lat = p1.Y + (p2.Y - p1.Y) * fraction;
            double lon = p1.X + (p2.X - p1.X) * fraction;
            return new Point(lon, lat);
        }

        public static Point FindCoordinateAtDistance(
        List<Point> pathPoints,
        List<double> segmentDistances, // precomputed distances between each pair of points
        double targetDistance)
        {
            double accumulated = 0.0;

            for (int i = 0; i < segmentDistances.Count; i++)
            {
                double segmentLength = segmentDistances[i];

                if (accumulated + segmentLength >= targetDistance)
                {
                    double remaining = targetDistance - accumulated;
                    double fraction = remaining / segmentLength;
                    return InterpolateBetween(pathPoints[i], pathPoints[i + 1], fraction);
                }

                accumulated += segmentLength;
            }

            // If target distance is beyond total path length, return last point
            return pathPoints.Last();
        }


    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Elevation { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    public class Edge
    {
        public int From { get; set; }
        public int To { get; set; }
        public double Weight { get; set; }
        public double ElevationChange { get; set; } 
    }
}