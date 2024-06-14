using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Collections.Generic;

//using System.Text.Json;

public static class DatabaseHelper
{
    private const string ConnectionString = "Server=(localdb)\\local;Database=coordinates";

    public static void SavePoint(Point point)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            var command = new SqlCommand("INSERT INTO coordinates.dbo.Points (X, Y) VALUES (@X, @Y)", connection);
            //var command = new SqlCommand("SELECT * FROM sys. databases", connection);
            command.Parameters.AddWithValue("@X", point.X);
            command.Parameters.AddWithValue("@Y", point.Y);
            command.ExecuteNonQuery();
        }
    }

    public static void SavePolygon(Polygon polygon)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            var command = new SqlCommand("INSERT INTO Polygons (Points) VALUES (@Points)", connection);
            command.Parameters.AddWithValue("@Points", polygon.ToString());
            command.ExecuteNonQuery();
        }
    }
    public static List<Point> GetPoints()
    {
        var points = new List<Point>();

        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            var command = new SqlCommand("SELECT X, Y FROM Points", connection);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var x = reader.GetDouble(0);
                    var y = reader.GetDouble(1);
                    points.Add(new Point(x, y));
                }
            }
        }

        return points;
    }

    public static List<Polygon> GetPolygons()
    {
        var polygons = new List<Polygon>();

        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            var command = new SqlCommand("SELECT Points FROM Polygons", connection);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var pointsStr = reader.GetString(0);
                    var polygon = Polygon.Parse(pointsStr);
                    polygons.Add(polygon);
                }
            }
        }

        return polygons;
    }
}

[Serializable]
[SqlUserDefinedType(Format.UserDefined, IsByteOrdered = true, MaxByteSize = 8000)]
public struct Point : INullable, IBinarySerialize
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static Point Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new ArgumentException("Input string is null or whitespace.");
        }

        s = s.Trim('(', ')').Replace(" ", "");
        var parts = s.Split(',');

        if (parts.Length != 2)
        {
            throw new FormatException("Input string is not in the correct format.");
        }

        if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double y))
        {
            return new Point(x, y);
        }
        else
        {
            throw new FormatException("Input string contains invalid numeric values.");
        }
    }

    public double Distance(Point other)
    {
        return Math.Sqrt(Math.Pow(other.X - X, 2) + Math.Pow(other.Y - Y, 2));
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public bool IsNull => false;

    public void Read(BinaryReader r)
    {
        X = r.ReadDouble();
        Y = r.ReadDouble();
    }

    public void Write(BinaryWriter w)
    {
        w.Write(X);
        w.Write(Y);
    }
}

[Serializable]
[SqlUserDefinedType(Format.UserDefined, IsByteOrdered = true, MaxByteSize = 8000)]

public struct Polygon : INullable, IBinarySerialize
{
    private Point[] vertices;
    public Point[] Vertices => vertices;

    public Polygon(Point[] points)
    {
        vertices = points;
    }

    public static Polygon Parse(string s)
    {
        var parts = s.Trim('(', ')').Split(new string[] { "), (" }, StringSplitOptions.None);
        var points = parts.Select(Point.Parse).ToArray();
        return new Polygon(points);
    }

    public double Area()
    {
        if (vertices.Length < 3)
            return 0;

        double area = 0;
        for (int i = 0; i < vertices.Length - 1; i++)
        {
            area += vertices[i].X * vertices[i + 1].Y - vertices[i].Y * vertices[i + 1].X;
        }
        area += vertices[vertices.Length - 1].X * vertices[0].Y - vertices[vertices.Length - 1].Y * vertices[0].X;

        return Math.Abs(area / 2.0);
    }

    public bool Contains(Point p)
    {
        int n = vertices.Length;
        bool result = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if ((vertices[i].Y > p.Y) != (vertices[j].Y > p.Y) &&
                (p.X < (vertices[j].X - vertices[i].X) * (p.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X))
            {
                result = !result;
            }
        }
        return result;
    }

    public override string ToString()
    {
        var points = string.Join(", ", vertices.Select(v => v.ToString()));
        return $"({points})";
    }

    public bool IsNull => false;

    public void Read(BinaryReader r)
    {
        int length = r.ReadInt32();
        vertices = new Point[length];
        for (int i = 0; i < length; i++)
        {
            var point = new Point();
            point.Read(r);
            vertices[i] = point;
        }
    }

    public void Write(BinaryWriter w)
    {
        w.Write(vertices.Length);
        foreach (var point in vertices)
        {
            point.Write(w);
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Wybierz operację:");
            Console.WriteLine("1 - Oblicz odległość między dwoma punktami");
            Console.WriteLine("2 - Oblicz pole wielokąta");
            Console.WriteLine("3 - Sprawdź, czy punkt należy do obszaru");
            Console.WriteLine("4 - Zapisz punkt w bazie danych");
            Console.WriteLine("5 - Zapisz wielokąt w bazie danych");
            Console.WriteLine("6 - Wypisz punkty z bazy danych");
            Console.WriteLine("7 - Wypisz wielokąty z bazy danych");
            Console.WriteLine("8 - Sprawdź przynależność punktu do wielokąta z bazy danych");
            Console.WriteLine("9 - Zakończ program");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CalculateDistance();
                    break;
                case "2":
                    CalculateArea();
                    break;
                case "3":
                    CheckContains();
                    break;
                case "4":
                    SavePoint();
                    break;
                case "5":
                    SavePolygon();
                    break;
                case "6":
                    LoadPoints();
                    break;
                case "7":
                    LoadPolygons();
                    break;
                case "8":
                    CheckPointInPolygon();
                    break;
                case "9":
                    return;
                default:
                    Console.WriteLine("Nieznana opcja, spróbuj ponownie.");
                    break;
            }

            Console.WriteLine();
        }
    }

    static void CalculateDistance()
    {
        try
        {
            Console.WriteLine("Podaj pierwszy punkt w formacie (x,y):");
            var point1Str = Console.ReadLine();
            var point1 = Point.Parse(point1Str);

            Console.WriteLine("Podaj drugi punkt w formacie (x,y):");
            var point2Str = Console.ReadLine();
            var point2 = Point.Parse(point2Str);

            var distance = point1.Distance(point2);
            Console.WriteLine($"Odległość: {distance}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    static void CalculateArea()
    {
        try
        {
            Console.WriteLine("Podaj wierzchołki wielokąta w formacie (x1,y1), (x2,y2), ... :");
            var polygonStr = Console.ReadLine();
            var polygon = Polygon.Parse(polygonStr);

            var area = polygon.Area();
            Console.WriteLine($"Pole: {area}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    static void CheckContains()
    {
        try
        {
            Console.WriteLine("Podaj wierzchołki wielokąta w formacie (x1,y1), (x2,y2), ... :");
            var polygonStr = Console.ReadLine();
            var polygon = Polygon.Parse(polygonStr);

            Console.WriteLine("Podaj punkt w formacie (x,y):");
            var pointStr = Console.ReadLine();
            var point = Point.Parse(pointStr);

            var contains = polygon.Contains(point);
            Console.WriteLine($"Czy punkt jest w obszarze: {contains}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    static void SavePoint()
    {
        try
        {
            Console.WriteLine("Podaj punkt w formacie (x,y):");
            var pointStr = Console.ReadLine();
            var point = Point.Parse(pointStr);

            DatabaseHelper.SavePoint(point);
            Console.WriteLine("Punkt został zapisany w bazie danych.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    static void SavePolygon()
    {
        try
        {
            Console.WriteLine("Podaj wierzchołki wielokąta w formacie (x1,y1), (x2,y2), ... :");
            var polygonStr = Console.ReadLine();
            var polygon = Polygon.Parse(polygonStr);

            DatabaseHelper.SavePolygon(polygon);
            Console.WriteLine("Wielokąt został zapisany w bazie danych.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }
    static void LoadPoints()
    {
        var points = DatabaseHelper.GetPoints();
        Console.WriteLine("Punkty w bazie danych:");
        foreach (var point in points)
        {
            Console.WriteLine(point);
        }
    }

    static void LoadPolygons()
    {
        var polygons = DatabaseHelper.GetPolygons();
        Console.WriteLine("Wielokąty w bazie danych:");
        foreach (var polygon in polygons)
        {
            Console.WriteLine(polygon);
        }
    }

    static void CheckPointInPolygon()
    {
        var points = DatabaseHelper.GetPoints();
        var polygons = DatabaseHelper.GetPolygons();

        Console.WriteLine("Podaj współrzędne punktu do sprawdzenia (x, y):");
        var input = Console.ReadLine();
        var parts = input.Split(',');
        var check = false;

        if (parts.Length == 2 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            var point = new Point(x, y);

            foreach (var polygon in polygons)
            {
                if (polygon.Contains(point))
                {
                    Console.WriteLine($"Punkt {point} znajduje się w wielokącie {polygon}.");
                    check = true;
                }
            }
            if (!check)
            {
                Console.WriteLine($"Punkt {point} nie znajduje się w żadnym wielokącie.");
            }
            return;
        }
        else
        {
            Console.WriteLine("Nieprawidłowy format.");
        }
    }
}
