using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace MidpointDisplacement
{
    public partial class DisplayForm : Form
    {
        public int TotalPoints { get; private set; }
        public double MinHeight, MaxHeight;
        private double WeightMinimum { get; set; }
        private int XVariance { get; set; }
        private int YVariance { get; set; }
        private Random _random = new Random();

        public DisplayForm(int points = 6, int xVariance = 10, int yVariance = 50)
        {
            TotalPoints = points;
            WeightMinimum = Math.Pow(.75, points - 1);
            XVariance = xVariance;
            YVariance = yVariance;
            MinHeight = 0.6*Height;
            MaxHeight = 0.9*Height;
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            var generatedLines = GeneratePointsItteratively();
            generatedLines = ConcatArrays(ConcatArrays(new[] {new PointF(0, Height/2)}, generatedLines),
                new[] {new PointF(Width, Height/2)});
            g.DrawLines(new Pen(Color.Black), generatedLines);
        }

        private PointF[] GeneratePointsRecursively(PointF midPoint, double weight)
        {
            weight *= .75;
            weight *= .75;
            var lowerPoint = new PointF((float) (midPoint.X - midPoint.X/2 * weight), _random.Next((int) (midPoint.Y - YVariance), (int) (midPoint.Y + YVariance)));
            var upperPoint = new PointF((float) (midPoint.X + midPoint.X/2 * weight), _random.Next((int) (midPoint.Y - YVariance), (int)(midPoint.Y + YVariance))); ;

            if (weight <= WeightMinimum)
                return new[] {lowerPoint, midPoint, upperPoint};

            var lowerPointSubset = GeneratePointsRecursively(lowerPoint, weight);
            var upperPointSubset = GeneratePointsRecursively(upperPoint, weight);

            return ConcatArrays(ConcatArrays(lowerPointSubset, new[] {midPoint}), upperPointSubset);
        }

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

                var newPoint = FindPointOnLine(points[i - 1], points[i], valuesOfX[i]);
                newPoint.Y += GenerateYVariance(weight);
                if(i >= valuesOfX.Length / 2)
                    points.Insert(i + 1, newPoint);
                else
                    points.Insert(i, newPoint);
            }

            points.Remove(midpoint);

            return points.ToArray();
        }

        private int GenerateXVariance(double weight)
        {
            return (int) (_random.Next(-XVariance, XVariance)*weight);
        }

        private int GenerateYVariance(double weight)
        {
            return (int) (_random.Next(-XVariance, XVariance)*weight);
        }

        private T[] ConcatArrays<T>(T[] array1, T[] array2)
        {
            int array1Length = array1.Length;
            Array.Resize(ref array1, array1Length + array2.Length);
            Array.Copy(array2, 0, array1, array1Length, array2.Length);

            return array1;
        }

        private PointF FindPointOnLine(PointF leftPoint, PointF rightPoint, float newX)
        {
            var angle = rightPoint.Y/leftPoint.X;
            var newY = Math.Atan(angle)*newX;
            return new PointF(newX, (float) newY);
        }
    }
}
