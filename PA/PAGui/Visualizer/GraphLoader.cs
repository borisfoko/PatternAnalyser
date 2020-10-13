using PADataProcessing.VoronoiToolBox;
using PAGui.DataLoader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PAGui.Visualizer
{
    /// <summary>
    /// Creates a VisualDrawing from the -plain output of the dot engine
    /// </summary>
    internal class GraphLoader
    {
        static Pen outline;
        static double DefaultRadius = 0.05;
        private DrawingVisual graph;
        private double BBMinX;
        private double BBMinY;
        private double BBMaxX;
        private double BBMaxY;

        static GraphLoader()
        {
            outline = new Pen(Brushes.Blue, 0.02);
            outline.Freeze();
        }

        public GraphLoader(IEnumerable<Node> Nodes, IEnumerable<Edge> Edges, double [] BoundingBox = null, BitmapFrame Frame = null, double PenSize = 0)
        {
            outline = new Pen(Brushes.Blue, 0.02 + (Frame != null ? PenSize : 0));
            outline.Freeze();
            graph = new DrawingVisual();
            
            LoadGraph(Nodes, Edges, Frame, (Frame != null ? PenSize : 0), BoundingBox);
        }

        public DrawingVisual Graph
        {
            get { return graph; }
        }

        public int NodeCount
        {
            get { return node_count; }
        }
        int node_count;

        public int EdgeCount
        {
            get { return edge_count; }
        }
        int edge_count;

        private void LoadGraph(IEnumerable<Node> Nodes, IEnumerable<Edge> Edges, BitmapFrame Frame = null, double PenSize = 0, double [] BoundingBox = null)
        {
            if (Frame != null)
            {
                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(Frame, new Rect(0, 0, Frame.Width, Frame.Height));
                }

                graph.Children.Add(drawingVisual);
            }

            if (BoundingBox == null)
            {
                BBMinX = -1;
                BBMinY = -1;
                BBMaxX = -1;
                BBMaxY = -1;
            }
            else if (BoundingBox.Length == 4)
            {
                BBMinX = BoundingBox[0];
                BBMinY = BoundingBox[1];
                BBMaxX = BoundingBox[2];
                BBMaxY = BoundingBox[3];
                DrawingVisual visual = new DrawingVisual();
                DrawingContext dc = visual.RenderOpen();
                Pen pen = new Pen(Brushes.Maroon, 0.2 + PenSize);
                Brush fill = Brushes.Transparent;
                Rect BoundingBoxRect = new Rect(new Point(BBMinX, BBMinY), new Point(BBMaxX, BBMaxY));
                dc.DrawRectangle(fill, pen, BoundingBoxRect);
                dc.Close();
                visual.SetValue(FrameworkElement.TagProperty, $"BBox_{01}");
                graph.Children.Add(visual);
            }

            for (int i = 0; i < Nodes.Count(); i++)
            {
                LoadNode(Nodes.ElementAt(i), i);
            }

            if (Frame == null)
            {
                PenSize = 0;
            }

            for (int i = 0; i < Edges.Count(); i++)
            {
                LoadEdge(Edges.ElementAt(i), i, PenSize);
            }
        }

        private void LoadNode(Node NodePoint, int NodeId)
        {
            Brush fill = Brushes.Transparent;

            if (Math.Abs(NodePoint.Orientation) >= 0.1)
                fill = Brushes.Blue;

            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();

            dc.DrawEllipse(fill, outline, new Point(NodePoint.X, NodePoint.Y), DefaultRadius, DefaultRadius);

            dc.Close();

            visual.SetValue(FrameworkElement.TagProperty, $"Node_{NodeId}");

            graph.Children.Add(visual);

            ++node_count;
        }

        private void LoadEdge(Edge edge, int EdgeId, double PenSize = 0)
        {
            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();

            Pen pen = new Pen(Brushes.Red, 0.05 + PenSize);
            Edge edgeCopy = ToBoundingBoxPoint(edge);
            if (IsInBoundingBox(new Point(edgeCopy.A.X, edgeCopy.A.Y)) == 0 && IsInBoundingBox(new Point(edgeCopy.B.X, edgeCopy.B.Y)) == 0)
                dc.DrawLine(pen, new Point(edgeCopy.A.X, edgeCopy.A.Y), new Point(edgeCopy.B.X, edgeCopy.B.Y));

            dc.Close();
            visual.SetValue(FrameworkElement.TagProperty, $"Edge_{EdgeId}");
            
            graph.Children.Add(visual);
            ++edge_count;
        }

        private Edge ToBoundingBoxPoint(Edge Source)
        {
            Point APrim = new Point
            (
                Source.A.X == double.PositiveInfinity ? double.MaxValue : (Source.A.X == double.NegativeInfinity ? double.MinValue : Source.A.X),
                Source.A.Y == double.PositiveInfinity ? double.MaxValue : (Source.A.Y == double.NegativeInfinity ? double.MinValue : Source.A.Y)
            );
            Point BPrim = new Point
            (
                Source.B.X == double.PositiveInfinity ? double.MaxValue : (Source.B.X == double.NegativeInfinity ? double.MinValue : Source.B.X),
                Source.B.Y == double.PositiveInfinity ? double.MaxValue : (Source.B.Y == double.NegativeInfinity ? double.MinValue : Source.B.Y)
            );
            Edge Result = new Edge 
            { 
                A = new Node(APrim.X, APrim.Y),
                B = new Node(BPrim.X, BPrim.Y)
            };

            if (BBMinX != -1 && BBMinY != -1 && BBMaxX != -1 && BBMaxY != -1 && Source.A != Source.B)
            {
                int AInBox = IsInBoundingBox(APrim);
                int BInBox = IsInBoundingBox(BPrim);


                if (AInBox > 0 && BInBox == 0)
                {
                    Point tmp = GetProjection(APrim, BPrim, AInBox);
                    Result.A = new Node(tmp.X, tmp.Y);
                }
                else if (BInBox > 0 && AInBox == 0)
                {
                    Point tmp = GetProjection(APrim, BPrim, BInBox);
                    Result.B = new Node(tmp.X, tmp.Y);
                }
            }

            return Result;
        }

        private Point GetProjection(Point p1, Point p2, int BoxSide)
        {
            Point Intersection = new Point();
            Point close_p1 = new Point();
            Point close_p2 = new Point();
            Point Center = new Point(BBMaxX / 2, BBMaxY / 2);
            Point p3 = new Point(), p4 = new Point();
            bool line_intersect = false;
            bool segment_intersect = false;
            switch (BoxSide)
            {
                case 1:
                    p3 = new Point(BBMinX, BBMinY);
                    p4 = new Point(BBMinX, BBMaxY);
                    FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    break;
                case 2:
                    p3 = new Point(BBMinX, BBMinY);
                    p4 = new Point(BBMinX, BBMaxY);
                    FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    if (!segment_intersect)
                    {
                        p3 = new Point(BBMinX, BBMinY);
                        p4 = new Point(BBMaxX, BBMinY);
                        FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    }
                    break;
                case 3:
                    p3 = new Point(BBMinX, BBMinY);
                    p4 = new Point(BBMaxX, BBMinY);
                    FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    break;
                case 4:
                    p3 = new Point(BBMinX, BBMinY);
                    p4 = new Point(BBMaxX, BBMinY);
                    FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    if (!segment_intersect)
                    {
                        p3 = new Point(BBMaxX, BBMinY);
                        p4 = new Point(BBMaxX, BBMaxY);
                        FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    }
                    break;
                case 5:
                    p3 = new Point(BBMaxX, BBMinY);
                    p4 = new Point(BBMaxX, BBMaxY);
                    FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    break;
                case 6:
                    p3 = new Point(BBMaxX, BBMinY);
                    p4 = new Point(BBMaxX, BBMaxY);
                    FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    if (!segment_intersect)
                    {
                        p3 = new Point(BBMaxX, BBMaxY);
                        p4 = new Point(BBMinX, BBMaxY);
                        FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    }
                    break;
                case 7:
                    p3 = new Point(BBMaxX, BBMaxY);
                    p4 = new Point(BBMinX, BBMaxY);
                    FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    break;
                case 8:
                    p3 = new Point(BBMaxX, BBMaxY);
                    p4 = new Point(BBMinX, BBMaxY);
                    FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    if (!segment_intersect)
                    {
                        p3 = new Point(BBMinX, BBMinY);
                        p4 = new Point(BBMinX, BBMaxY);
                        FindIntersection(p1, p2, p3, p4, out line_intersect, out segment_intersect, out Intersection, out close_p1, out close_p2);
                    }
                    break;
            }

            return Intersection;
        }

        // Find the point of intersection between
        // the lines p1 --> p2 and p3 --> p4.
        private void FindIntersection(
            Point p1, Point p2, Point p3, Point p4,
            out bool lines_intersect, out bool segments_intersect,
            out Point intersection,
            out Point close_p1, out Point close_p2)
        {
            // Get the segments' parameters.
            double dx12 = p2.X - p1.X;
            double dy12 = p2.Y - p1.Y;
            double dx34 = p4.X - p3.X;
            double dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            double denominator = (dy12 * dx34 - dx12 * dy34);

            double t1 =
                ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                    / denominator;
            if (double.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new Point(double.NaN, double.NaN);
                close_p1 = new Point(double.NaN, double.NaN);
                close_p2 = new Point(double.NaN, double.NaN);
                return;
            }
            lines_intersect = true;

            double t2 =
                ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                    / -denominator;

            // Find the point of intersection.
            intersection = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect =
                ((t1 >= 0) && (t1 <= 1) &&
                 (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);
            close_p2 = new Point(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }

        private int IsInBoundingBox(Point Source)
        {
            if (Source == null)
                return -1;

            if (Source.X < BBMinX && (BBMinY <= Source.Y && Source.Y <= BBMaxY))
                return 1;
            if (Source.X < BBMinX && Source.Y < BBMinY)
                return 2;
            if ((BBMinX <= Source.X && Source.X <= BBMaxX) && Source.Y < BBMinY)
                return 3;
            if (BBMaxX < Source.X && Source.Y < BBMinY)
                return 4;
            if (BBMaxX < Source.X && (BBMinY <= Source.Y && Source.Y <= BBMaxY))
                return 5;
            if (BBMaxX < Source.X && BBMaxY < Source.Y)
                return 6;
            if ((BBMinX <= Source.X && Source.X <= BBMaxX) && BBMaxY < Source.Y)
                return 7;
            if (Source.X < BBMinX && BBMaxY < Source.Y)
                return 8;
            return 0;
        }
    }
}
