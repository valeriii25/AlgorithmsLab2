#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;


internal class Program
{
    public static void Main(string[] args)
    {
        /*var countOfRectangles = int.Parse(Console.ReadLine()!);
        var rectangles = new List<(Point2d, Point2d)>();
        for (int i = 0; i < countOfRectangles; i++)
        {
            var nums = Console.ReadLine().Split().Select(x => int.Parse(x)).ToArray();
            var first = new Point2d(nums[0], nums[1]);
            var second = new Point2d(nums[2], nums[3]);
            rectangles.Add((first, second));
        }
        var countOfPoints = int.Parse(Console.ReadLine()!);
        var points = new List<Point2d>();
        for (int i = 0; i < countOfPoints; i++)
        {
            var nums = Console.ReadLine().Split().Select(x => int.Parse(x)).ToArray();
            var point = new Point2d(nums[0], nums[1]);
            points.Add(point);
        }
        if (countOfRectangles == 0)
            for (int i = 0; i < countOfPoints; i++)
            {
                Console.Write(0 + " ");
            }

        var obj = new Algorithm3();
        obj.FillRoots(rectangles);
        foreach (var point in points)
        {
            Console.Write(obj.GetRectangleCount(point) + " ");
        }*/

        BenchmarkRunner.Run<Bench>();
        
    }
}

[PlainExporter]
[HtmlExporter]
[RPlotExporter]

public class Bench
{
    public int countOfPoints { get; set; } = 1000;
    [Params(2, 4, 8, 16, 32, 64, 128, 256, 512, 1024)]
    public int countOfRectangles { get; set;  } = 100;
    public Algorithm2 a = new();
    public Algorithm3 b = new();
    
    public List<Point2d> points =>
        Enumerable.Range(0, countOfPoints).Select(i =>
        {
            var x = BigInteger.ModPow(9613 * new BigInteger(i), 31, countOfPoints * 20);
            var y = BigInteger.ModPow(5227 * new BigInteger(i), 31, countOfPoints * 20);
            return new Point2d((int)x, (int)y);
        }).ToList();
    
    public List<(Point2d, Point2d)> rectangles =>
        Enumerable.Range(0, countOfRectangles)
            .Select(i => (
                    new Point2d(10 * i, 10 * i),
                    new Point2d(2 * countOfRectangles - i, 2 * countOfRectangles - i)
                )
            )
            .ToList();

    [Benchmark]
    public void Start1()
    {
        foreach (var point in points)
        {
            Algorithm1.GetRectangleCount(point, rectangles);
        }
    }
    

    [Benchmark]
    public void Prepare2()
    {
        var g = new Algorithm2();
        g.FillMatrix(rectangles);
    }
    
    [Benchmark]
    public void Start2()
    {
        foreach (var point in points)
        {
            a.GetRectangleCount(point);
        }
    }
    
    [GlobalSetup]
    public void Prep3()
    {
        b.FillRoots(rectangles);
        a.FillMatrix(rectangles);
    }

    [Benchmark]
    public void Prepare3()
    {
        var g = new Algorithm3();
        g.FillRoots(rectangles);
    }
    
    [Benchmark]
    public void Start3()
    {
        foreach (var point in points)
        {
            b.GetRectangleCount(point);
        }
    }
}

public class Point2d
{
    public readonly int X, Y;  
    public Point2d(int a, int b)
    {
        X = a;
        Y = b;
    }
}

public class Node
{
    public int value;
    public Node? left, right; 
    public int leftIndex;
    public int rightIndex;

    public Node(int val, Node? l, Node? r, int lIndex, int rIndex)
    {
        value = val;
        left = l;
        right = r;
        leftIndex = lIndex;
        rightIndex = rIndex;
    }

    public Node(Node a) : this(a.value, a.left, a.right, a.leftIndex, a.rightIndex)
    {
    }
}

public class Modification
{
    public readonly int Operation, X, YMinIndex, YMaxIndex; 
    
    public Modification(int op, int xRoot, int y1, int y2)
    {
        Operation = op;
        X = xRoot;
        YMinIndex = y1;
        YMaxIndex = y2;
    }
}

public class Algorithm3
{
    private List<Node> _roots = new();
    private List<Modification> _modifications = new();
    private List<int> _xArray = new(), _yArray = new();
    private Dictionary<int, int> _matrixXIndexes = new ();
    private Dictionary<int, int> _matrixYIndexes = new ();

    private void FillModifications(List<(Point2d, Point2d)> rectangles)
    {
        foreach (var rectangle in rectangles)
        {
            var beginRectangle = new Modification(1, rectangle.Item1.X, 
                _matrixYIndexes[rectangle.Item1.Y],_matrixYIndexes[rectangle.Item2.Y] - 1);
            var endRectangle = new Modification(-1, rectangle.Item2.X,
                _matrixYIndexes[rectangle.Item1.Y], _matrixYIndexes[rectangle.Item2.Y] - 1);
            _modifications.Add(beginRectangle);
            _modifications.Add(endRectangle);
        }
        _modifications = _modifications.OrderBy(elem => elem.X).ToList();
    }

    private void FillArrays(List<(Point2d, Point2d)> rectangles)
    {
        foreach (var rectangle in rectangles)
        {
            _xArray.Add(rectangle.Item1.X);
            _xArray.Add(rectangle.Item2.X);
            _yArray.Add(rectangle.Item1.Y);
            _yArray.Add(rectangle.Item2.Y);
        }
        _xArray = _xArray.Distinct().OrderBy(elem => elem).ToList();
        _yArray = _yArray.Distinct().OrderBy(elem => elem).ToList();
        int index = 0;
        foreach (var x in _xArray)
            _matrixXIndexes[x] = index++;
        index = 0;
        foreach (var y in _yArray)
            _matrixYIndexes[y] = index++;
    }

    private static Node MakeStartTree(int begin, int end)
    {
        if (begin >= end)
            return new Node(0, null, null, begin, end);
        int middle = (begin + end) / 2;
        Node left = MakeStartTree(begin, middle);
        Node right = MakeStartTree(middle + 1, end);
        return new Node(left.value + right.value, left, right, left.leftIndex, right.rightIndex);
    }

    private Node? NewTree(Node? node, Modification modification)
    {
        if (node is null) return node;
        var newNode = new Node(node);
        if (node.leftIndex >= modification.YMinIndex && node.rightIndex <= modification.YMaxIndex)
        {
            newNode.value += modification.Operation;
            return newNode;
        }
        if (node.rightIndex < modification.YMinIndex || node.leftIndex > modification.YMaxIndex)
            return node;
        newNode.left = NewTree(node.left, modification);
        newNode.right = NewTree(node.right, modification);
        return newNode;
    }

    public void FillRoots(List<(Point2d, Point2d)> rectangles)
    {
        FillArrays(rectangles);
        FillModifications(rectangles);
        var root = MakeStartTree(0, _yArray.Count - 1);
        Modification? prev = null;
        foreach (var modification in _modifications)
        {
            if (prev is not null && modification.X != prev.X)
                _roots.Add(root!);
            root = NewTree(root, modification);
            prev = modification;
        }
        _roots.Add(root!);
    }
    
    private static int BinarySearch(int coordinate, List<int> array)
    {
        var left = 0;
        var right = array.Count - 1;
        while (left <= right)
        {
            var middle = (left + right) / 2;
            if (array[middle] == coordinate)
                return middle;
            if (array[middle] < coordinate)
                left = middle + 1;
            if (array[middle] > coordinate)
                right = middle - 1;
        }
        return right;
    }

    private int Dfs(Node? root, int y)
    {
        if (root is null) return 0;
        var middle = (root.leftIndex + root.rightIndex) / 2;
        if (y <= middle) return Dfs(root.left, y) + root.value;
        return Dfs(root.right, y) + root.value;
    }
    
    public int GetRectangleCount(Point2d point)
    {
        //var indexInRoots = _matrixXIndexes.ContainsKey(point.X) ? _matrixXIndexes[point.X] : BinarySearch(point.X, _xArray);
        var indexInRoots = BinarySearch(point.X, _xArray);
        if (indexInRoots == -1) return 0;
        //var yIndex = _matrixYIndexes.ContainsKey(point.Y) ? _matrixYIndexes[point.Y] : BinarySearch(point.Y, _yArray);
        var yIndex = BinarySearch(point.Y, _yArray);
        if (yIndex == -1) return 0;
        return Dfs(_roots[indexInRoots], yIndex);
    }
}

public class Algorithm2
{
    private List<int> _xArray = new(), _yArray = new();
    private int[,] _matrix;
    private Dictionary<int, int> _matrixXIndexes = new();
    private Dictionary<int, int> _matrixYIndexes = new();
    
    private void FillArrays(List<(Point2d, Point2d)> rectangles)
    {
        foreach (var rectangle in rectangles)
        {
            _xArray.Add(rectangle.Item1.X);
            _xArray.Add(rectangle.Item2.X);
            _yArray.Add(rectangle.Item1.Y);
            _yArray.Add(rectangle.Item2.Y);
        }
        _xArray = _xArray.Distinct().OrderBy(elem => elem).ToList();
        _yArray = _yArray.Distinct().OrderBy(elem => elem).ToList();
        int index = 0;
        foreach (var x in _xArray)
            if (!_matrixXIndexes.ContainsKey(x))
                _matrixXIndexes.Add(x, index++);
        index = 0;
        foreach (var y in _yArray)
            if (!_matrixYIndexes.ContainsKey(y))
                _matrixYIndexes.Add(y, index++);
    }

    public void FillMatrix(List<(Point2d, Point2d)> rectangles)
    {
        FillArrays(rectangles);
        _matrix = new int[_yArray.Count,_xArray.Count];
        foreach (var rectangle in rectangles)
        {
            for (int i = _matrixYIndexes[rectangle.Item1.Y]; i < _matrixYIndexes[rectangle.Item2.Y]; i++) 
            for (int j = _matrixXIndexes[rectangle.Item1.X]; j < _matrixXIndexes[rectangle.Item2.X]; j++) 
                _matrix[i,j]++;
        }
    }

    private static int BinarySearch(int coordinate, List<int> array)
    {
        var left = 0;
        var right = array.Count - 1;
        while (left <= right)
        {
            var middle = (left + right) / 2;
            if (array[middle] == coordinate)
                return middle;
            if (array[middle] < coordinate)
                left = middle + 1;
            if (array[middle] > coordinate)
                right = middle - 1;
        }
        return right;
    }

    public int GetRectangleCount(Point2d point)
    {
        int xInMatrix, yInMatrix;
        xInMatrix = _matrixXIndexes.ContainsKey(point.X) ? _matrixXIndexes[point.X] : BinarySearch(point.X, _xArray);
        yInMatrix = _matrixYIndexes.ContainsKey(point.Y) ? _matrixYIndexes[point.Y] : BinarySearch(point.Y, _yArray);
        return _matrix[yInMatrix, xInMatrix];
    }
}

public class Algorithm1
{
    public static int GetRectangleCount(Point2d point, List<(Point2d, Point2d)> rectangles)
    {
        return rectangles.Count(rectangle => point.X >= rectangle.Item1.X && 
                                             point.X < rectangle.Item2.X && 
                                             point.Y >= rectangle.Item1.Y && 
                                             point.Y < rectangle.Item2.Y);
    }
}
