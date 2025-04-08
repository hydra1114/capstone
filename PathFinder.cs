using System;
using System.Collections.Generic;

public class PathFinder
{
    public static List<List<double>> FindPath(List<double> start, List<double> end)
    {
        var path = new List<List<double>>();
        DivideAndConquer(start, end, path);
        return path;
    }

    private static void DivideAndConquer(List<double> start, List<double> end, List<List<double>> path)
    {
        double distance = CalculateDistance(start, end);
        if (distance < 1)
        {
            path.Add(start);
            path.Add(end);
            return;
        }

        var middlePoints = GenerateMiddlePoints(start, end);
        foreach (var middle in middlePoints)
        {
            DivideAndConquer(start, middle, path);
            DivideAndConquer(middle, end, path);
        }
    }

    private static double CalculateDistance(List<double> point1, List<double> point2)
    {
        double dx = point2[0] - point1[0];
        double dy = point2[1] - point1[1];
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static List<List<double>> GenerateMiddlePoints(List<double> start, List<double> end)
    {
        var middlePoints = new List<List<double>>();
        for (int i = 1; i <= 5; i++)
        {
            double ratio = i / 6.0;
            double x = start[0] + ratio * (end[0] - start[0]);
            double y = start[1] + ratio * (end[1] - start[1]);
            middlePoints.Add(new List<double> { x, y });
        }
        return middlePoints;
    }
}
