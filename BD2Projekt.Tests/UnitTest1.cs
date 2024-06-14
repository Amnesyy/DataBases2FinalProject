using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class PointTests
{
    [Test]
    public void Point_Parse_ValidString_ReturnsPoint()
    {
        var pointStr = "(1.5, 2.5)";
        var point = Point.Parse(pointStr);

        ClassicAssert.AreEqual(1.5, point.X);
        ClassicAssert.AreEqual(2.5, point.Y);
    }

    [Test]
    public void Point_Distance_ReturnsCorrectDistance()
    {
        var point1 = new Point(1, 1);
        var point2 = new Point(4, 5);

        var distance = point1.Distance(point2);

        ClassicAssert.AreEqual(5, distance);
    }

    [Test]
    public void Point_ToString_ReturnsCorrectFormat()
    {
        var point = new Point(1.5, 2.5);
        var pointStr = point.ToString();

        ClassicAssert.AreEqual("(1.5, 2.5)", pointStr);
    }
}


[TestFixture]
public class PolygonTests
{
    [Test]
    public void Polygon_Parse_ValidString_ReturnsPolygon()
    {
        var polygonStr = "(1.5, 2.5), (3.5, 4.5), (5.5, 6.5)";
        var polygon = Polygon.Parse(polygonStr);

        ClassicAssert.AreEqual(3, polygon.Vertices.Length);
        ClassicAssert.AreEqual(1.5, polygon.Vertices[0].X);
        ClassicAssert.AreEqual(2.5, polygon.Vertices[0].Y);
    }

    [Test]
    public void Polygon_Area_ReturnsCorrectArea()
    {
        var points = new[] { new Point(0, 0), new Point(4, 0), new Point(4, 3) };
        var polygon = new Polygon(points);

        var area = polygon.Area();

        ClassicAssert.AreEqual(6, area);
    }

    [Test]
    public void Polygon_Contains_ReturnsCorrectResult()
    {
        var points = new[] { new Point(0, 0), new Point(4, 0), new Point(4, 3), new Point(0, 3) };
        var polygon = new Polygon(points);

        var insidePoint = new Point(2, 2);
        var outsidePoint = new Point(5, 5);

        ClassicAssert.IsTrue(polygon.Contains(insidePoint));
        ClassicAssert.IsFalse(polygon.Contains(outsidePoint));
    }
}


[TestFixture]
public class DatabaseHelperTests
{
    [Test]
    public void DatabaseHelper_SavePoint_SavesPoint()
    {
        var point = new Point(1.1, 2.2);
        DatabaseHelper.SavePoint(point);

        var points = DatabaseHelper.GetPoints();
        var savedPoint = points.LastOrDefault();

        ClassicAssert.AreEqual(point.X, savedPoint.X);
        ClassicAssert.AreEqual(point.Y, savedPoint.Y);
    }

    [Test]
    public void DatabaseHelper_SavePolygon_SavesPolygon()
    {
        var points = new[] { new Point(0, 0), new Point(1, 1), new Point(2, 0) };
        var polygon = new Polygon(points);
        DatabaseHelper.SavePolygon(polygon);

        var polygons = DatabaseHelper.GetPolygons();
        var savedPolygon = polygons.LastOrDefault();

        ClassicAssert.AreEqual(polygon.Vertices.Length, savedPolygon.Vertices.Length);
        for (int i = 0; i < polygon.Vertices.Length; i++)
        {
            ClassicAssert.AreEqual(polygon.Vertices[i].X, savedPolygon.Vertices[i].X);
            ClassicAssert.AreEqual(polygon.Vertices[i].Y, savedPolygon.Vertices[i].Y);
        }
    }
}
