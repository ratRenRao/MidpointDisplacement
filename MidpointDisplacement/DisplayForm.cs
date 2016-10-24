using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace MidpointDisplacement
{
    public partial class DisplayForm : Form
    {
        public int TotalPoints { get; private set; }
        public float MinHeight, MaxHeight, MaxWidth, MinWidth;
        private float WeightMinimum { get; set; }
        private int XVariance { get; set; }
        private int YVariance { get; set; }
        private readonly Random _random = new Random();
        private static readonly AutoResetEvent Wait = new AutoResetEvent(true); 
        private int _generatedPoints = 0;
        private readonly object _countLock = new object();

        public DisplayForm(int points = 100, int xVariance = 20, int yVariance = 20)
        {
            TotalPoints = points;
            WeightMinimum = (float) Math.Pow(.75, points - 1);
            XVariance = xVariance;
            YVariance = yVariance;
            MinHeight = (float) (0.6*Height);
            MaxHeight = (float) (0.9*Height);
            MinWidth = (float) (0.6*Width);
            MaxWidth = (float) (0.9*Width);

            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            //var generatedLines = GeneratePointsItteratively();
            var leftPoint = new PointF(0, GetWeightedVariance(0, (float) (Height*.3) * 2)); 
            var rightPoint = new PointF(Width, GetWeightedVariance(0, (float) (Height*.3) * 2));

            var generatedLines = GeneratePointsRecursively(leftPoint, rightPoint);

            // Add end points
            generatedLines = ConcatArrays(new[] {leftPoint, rightPoint}, generatedLines);

            Array.Sort(generatedLines, (x, y) => x.X.CompareTo(y.X));
            for(var i = 0; i < generatedLines.Length; i++)
            {
                generatedLines[i].Y = Height - generatedLines[i].Y;
            }

            g.DrawLines(new Pen(Color.Black), generatedLines);
            foreach(var point in generatedLines)
                g.DrawRectangle(new Pen(Color.Red), point.X, point.Y, 3f, 3f);
        }

        private PointF[] GeneratePointsRecursively(PointF leftBoundary, PointF rightBoundary)
        {
            var newX = GetWeightedVariance(rightBoundary.X, leftBoundary.X);

            var midPoint = new PointF(newX, leftBoundary.X == 0 && rightBoundary.X == Width
                ? (float) (Height*.85)
                : GetWeightedVariance(rightBoundary.Y, leftBoundary.Y));

            lock (_countLock)
                _generatedPoints++;

            if (_generatedPoints > TotalPoints)
                return new[] {midPoint};

            PointF[] upperPointSubset, lowerPointSubset;
            if (_random.Next(1) == 0)
            { 
                lowerPointSubset = GeneratePointsRecursively(leftBoundary, midPoint);
                upperPointSubset = GeneratePointsRecursively(midPoint, rightBoundary);
            }
            else
            {
                upperPointSubset = GeneratePointsRecursively(midPoint, rightBoundary);
                lowerPointSubset = GeneratePointsRecursively(leftBoundary, midPoint);
            }
            return ConcatArrays(ConcatArrays(lowerPointSubset, new[] {midPoint}), upperPointSubset);
        }

        
        private T[] ConcatArrays<T>(T[] array1, T[] array2)
        {
            int array1Length = array1.Length;
            Array.Resize(ref array1, array1Length + array2.Length);
            Array.Copy(array2, 0, array1, array1Length, array2.Length);

            return array1;
        }

        private float FindY(PointF leftPoint, PointF rightPoint)
        {
            var newY = Math.Abs((rightPoint.Y + leftPoint.Y)/2);
            return newY;
        }

        private float GetWeightedVariance(float p1, float p2)
        {
            var min = p1 > p2 ? p2 : p1;
            var max = p1 < p2 ? p2 : p1;
            var hat = GenerateHat(min, max).ToList();
            Wait.WaitOne();
            return hat[_random.Next(hat.Count)]*(_random.Next(1) == 0 ? 1 : -1);
        }

        private static IEnumerable<float> GenerateHat(float index, float end)
        {
            int count = 0;
            int k = 0;
            int mid = (int) (Math.Floor(end + index)/2);

            while (index <= mid)
            {
                for (int i = 0; i <= count; i++)
                {
                    yield return index;
                }
                count += k;
                k++;
                index += 2;
            };

            Wait.Set();
        }

        private int GenerateXVariance(float leftX, float rightX, float newX)
        {
            //return (int) (_random.Next(-XVariance, XVariance)*weight);
            var baseChange = rightX - (leftX + rightX) / 2;
            var weightedMax = (Math.Abs(rightX - leftX) / MaxWidth) * 100;

            return 0;
        }

        private int GenerateYVariance(float y)
        {
            //return (int) (_random.Next(-XVariance, XVariance)*weight);
            return 0;
        }


        /*
        private PointF[] GeneratePointsItteratively()
        {
            double weight = 1;
            var valuesOfX = new float[TotalPoints + 2];
            valuesOfX[0] = 0;
            valuesOfX[valuesOfX.Length - 1] = Width;

            for (int i = 1; i <= TotalPoints; i++)
            {
                weight = 1-(i-1)*(1/TotalPoints);
                do
                {
                    valuesOfX[i] = i*(Width/TotalPoints) + GenerateXVariance(weight);
                }while (valuesOfX[i] < 0 || valuesOfX[i] > Width);
            }

            weight = 1;
            var midpoint = new PointF((float) Width/2, Height - _random.Next((int) MinHeight, (int) MaxHeight));
            var points = new List<PointF>()
            {
                new PointF(0, (float) (Height * .7) + _random.Next(-YVariance, YVariance)),
                midpoint,
                new PointF(Width, (float) (Height * .7) + _random.Next(-YVariance, YVariance))
            };

            for (int i = 1; i < valuesOfX.Length - 1; i++)
            {
                if (TotalPoints%2 == 1 && i == valuesOfX.Length/2 + 1)
                    continue;
                //weight = 1 - (i - 1) * (1 / TotalPoints);

                var newPoint = new PointF(newX, FindY(points[i - 1], points[i], valuesOfX[i]));
                newPoint.Y += GenerateYVariance(weight);
                if(i >= valuesOfX.Length / 2)
                    points.Insert(i + 1, newPoint);
                else
                    points.Insert(i, newPoint);
            }

            points.Remove(midpoint);

            return points.ToArray();
        }
        */
    }
}
