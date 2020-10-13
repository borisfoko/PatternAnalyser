using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PAGui.Visualizer
{
    /// <summary>
    /// Holds a DrawingVisual displaying a graph.
    /// </summary>
    /// <remarks>
    /// The graph can be scaled through manipulating the zoom property (which invalidates measure)
    /// Tooltips over Nodes are possible (through observing to tooltips events)
    /// Click events on Nodes are possible
    /// </remarks>
    class GraphElement : FrameworkElement
    {
        private DrawingVisual _graph = new DrawingVisual();
        private DrawingVisual _sourceImage = new DrawingVisual();

        public GraphElement()
        {
            this.MouseLeftButtonDown += MouseLeftButtonDownHandler;
            ClipToBounds = true;

            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
        }

        public double Zoom
        {
            set
            {
                _zoom = value;
                InvalidateMeasure();
            }

            get { return _zoom; }
        }
        private double _zoom = 1;

        public DrawingVisual Graph
        {
            set
            {
                if (_graph != value)
                {
                    RemoveVisualChild(_graph);
                    _graph = value;

                    if (value != null)
                        AddVisualChild(_graph);

                    InvalidateMeasure();
                }
            }

            get { return _graph; }
        }

        public DrawingVisual SourceImage
        {
            get { return _sourceImage; }
            set
            {
                if (_sourceImage != value)
                {
                    RemoveVisualChild(_sourceImage);
                    _sourceImage = value;

                    if (value != null)
                        AddVisualChild(_sourceImage);

                    InvalidateMeasure();
                }
            }
        }

        // Capture the mouse event and hit test the coordinate point value against
        // the child visual objects.
        void MouseLeftButtonDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToolTipController.Hide();

            // Retreive the coordinates of the mouse button event.
            Point pt = e.GetPosition(this);
            DrawingVisual hit = VisualTreeHelper.HitTest(this, pt).VisualHit as DrawingVisual;
            if (hit != null)
            {
                string tag = hit.ReadLocalValue(FrameworkElement.TagProperty) as string;
                if (tag != null)
                {
                    foreach (DrawingVisual v in _graph.Children)
                    {
                        v.BitmapEffect = null;
                    }

                    OuterGlowBitmapEffect glow = new OuterGlowBitmapEffect();
                    glow.GlowColor = Colors.Blue;
                    glow.GlowSize = 1;
                    glow.Opacity = 0.8;
                    glow.Freeze();
                    hit.BitmapEffect = glow;
                }
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            ToolTipController.Move(null, null);
        }

        public ToolTipContentProviderDelegate ToolTipContentProvider;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point pt = e.GetPosition(this);

            HitTestResult result = VisualTreeHelper.HitTest(this, pt);
            if (result == null)
            {
                ToolTipController.Move(null, null);
            }
            else
            {
                DrawingVisual hit = result.VisualHit as DrawingVisual;
                object tag = (hit != null) ? hit.ReadLocalValue(FrameworkElement.TagProperty) : null;
                ToolTipController.Move(ToolTipContentProvider, tag);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        }

        // Provide a required override for the VisualChildCount property.
        protected override int VisualChildrenCount
        {
            get 
            { 
                return 2; 
            }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            if (index == 0 && _graph != null && _graph.Children.Count > 0)
                return _graph;
            else
                return _sourceImage;
        }

        internal void ZoomTo(Size size)
        {
            Size gs = GraphSize;
            double scaleY = size.Height / gs.Height;
            double scaleX = size.Width / gs.Width;
            Zoom = Math.Min(1, Math.Min(scaleX, scaleY));
        }

        private Size GraphSize
        {
            get
            {
                Rect bounds = (_graph.ContentBounds != Rect.Empty ? _graph.ContentBounds : _sourceImage.ContentBounds);
                bounds.Union(_graph.DescendantBounds != Rect.Empty ? _graph.DescendantBounds : _sourceImage.DescendantBounds);
                return new Size((bounds.Width + 2 * paddingX) * 64, (bounds.Height + 2 * paddingY) * 64);
            }
        }

        const double paddingX = 1; // availableSize.Width / _zoom / 64;
        const double paddingY = 1; // availableSize.Height / _zoom / 64;

        protected override Size MeasureOverride(Size availableSize)
        {
            Rect bounds = (_graph.ContentBounds != Rect.Empty ? _graph.ContentBounds : _sourceImage.ContentBounds);
            bounds.Union(_graph.DescendantBounds != Rect.Empty ? _graph.DescendantBounds : _sourceImage.DescendantBounds);
            if (bounds.IsEmpty) return new Size(8, 8); // if the graph is empty

            Matrix m = new Matrix();
            m.Translate(-bounds.Left + paddingX, -bounds.Top + paddingY);
            m.Scale(_zoom * 64, _zoom * 64);
            _graph.Transform = new MatrixTransform(m);
            _sourceImage.Transform = new MatrixTransform(m);

            return new Size((bounds.Width + 2 * paddingY) * _zoom * 64, (bounds.Height + 2 * paddingY) * _zoom * 64);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }
    }
}
