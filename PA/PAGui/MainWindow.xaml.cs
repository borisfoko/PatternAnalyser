using Microsoft.Win32;
using PADataProcessing.VoronoiToolBox;
using PAGui.DataLoader;
using PAGui.Visualizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Image = System.Drawing.Image;

namespace PAGui
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Dictionary<string, string>> _dataList;
        private BitmapFrame _frame;
        private VoronoiGraph _voronoiGraph;
        private VoronoiGraph _voronoiGraphCopy;
        private Dictionary<string, object> _tips;
        private List<Node> _nodes;
        private List<Node> _vVNodes;
        private List<Edge> _edges;
        private static RoutedCommand _voronoiDiagrammCommand;
        private static RoutedCommand _delaunayDiagrammCommand;
        private static RoutedCommand _btnErosionCommand;
        private static RoutedCommand _btnDilatationCommand;
        private static RoutedCommand _btnOpeningCommand;
        private static RoutedCommand _btnClosingCommand;
        private static RoutedCommand _btnASFCommand;
        private static RoutedCommand _btnGReconstructionCommand;
        private static RoutedCommand _btnDistanceFunctionCommand;
        private static RoutedCommand _btnSkeletonCommand;
        private static RoutedCommand _btnOpennessCommand;
        private static RoutedCommand _btnClosenessCommand;
        private string _fileName;
        private byte[] _imagePixels;
        private int _grayScaleValue;
        private bool _scaled;
        private double[] _boundingBox;
        private int _erosionDilatationValue;
        private int _openingClosingValue;

        public MainWindow()
        {
            _boundingBox = new double[] { -1, -1, -1, -1 };
            _grayScaleValue = 175;
            _scaled = false;
            _voronoiDiagrammCommand = new RoutedCommand("VoronoiDiagrammCommand", typeof(MainWindow));
            _delaunayDiagrammCommand = new RoutedCommand("DelaunayDiagrammCommand", typeof(MainWindow));
            _btnErosionCommand = new RoutedCommand("BtnErosionCommand", typeof(MainWindow));
            _btnDilatationCommand = new RoutedCommand("BtnDilatationCommand", typeof(MainWindow));
            _btnOpeningCommand = new RoutedCommand("BtnOpeningCommand", typeof(MainWindow));
            _btnClosingCommand = new RoutedCommand("BtnClosingCommand", typeof(MainWindow));
            _btnASFCommand = new RoutedCommand("BtnASFCommand", typeof(MainWindow));
            _btnGReconstructionCommand = new RoutedCommand("BtnGReconstructionCommand", typeof(MainWindow));
            _btnDistanceFunctionCommand = new RoutedCommand("BtnDistanceFunctionCommand", typeof(MainWindow));
            _btnSkeletonCommand = new RoutedCommand("BtnSkeletonCommand", typeof(MainWindow));
            _btnOpennessCommand = new RoutedCommand("BtnOpennessCommand", typeof(MainWindow));
            _btnClosenessCommand = new RoutedCommand("BtnClosenessCommand", typeof(MainWindow));
            _erosionDilatationValue = 0;
            _openingClosingValue = 0;
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenExecuted));
            InputBindings.Add(new InputBinding(ApplicationCommands.Open, new KeyGesture(Key.O, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, PrintExecuted));
            InputBindings.Add(new InputBinding(ApplicationCommands.Print, new KeyGesture(Key.P, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseExecuted));
            InputBindings.Add(new InputBinding(ApplicationCommands.Close, new KeyGesture(Key.F4, ModifierKeys.Alt)));
            _tips = new Dictionary<string, object>();
            _dataList = new List<Dictionary<string, string>>();
            _nodes = new List<Node>();
            _vVNodes = new List<Node>();
            _edges = new List<Edge>();
            MyDotViewer.ShowNodeTip += MyDotViewer_ShowToolTip;
        }

        public static RoutedCommand VoronoiDiagrammCommand
        {
            get { return _voronoiDiagrammCommand; }
        }

        public static RoutedCommand DelaunayDiagrammCommand
        {
            get { return _delaunayDiagrammCommand; }
        }

        public static RoutedCommand BtnErosionCommand
        {
            get { return _btnErosionCommand; }
        }

        public static RoutedCommand BtnDilatationCommand
        {
            get { return _btnDilatationCommand; }
        }

        public static RoutedCommand BtnOpeningCommand
        {
            get { return _btnOpeningCommand; }
        }

        public static RoutedCommand BtnClosingCommand
        {
            get { return _btnClosingCommand; }
        }

        public static RoutedCommand BtnASFCommand
        {
            get { return _btnASFCommand; }
        }

        public static RoutedCommand BtnGReconstructionCommand
        {
            get { return _btnGReconstructionCommand; }
        }

        public static RoutedCommand BtnDistanceFunctionCommand
        {
            get { return _btnDistanceFunctionCommand; }
        }

        public static RoutedCommand BtnSkeletonCommand
        {
            get { return _btnSkeletonCommand; }
        }

        public static RoutedCommand BtnOpennessCommand
        {
            get { return _btnOpennessCommand; }
        }

        public static RoutedCommand BtnClosenessCommand
        {
            get { return _btnClosenessCommand; }
        }

        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = "Csv & TIF Dateien (*.csv;*.tif;*.jfif)|*.csv;*.tif;*.jfif|Alle Dateien (*.*)|*.*";
            if (dlg.ShowDialog() == true)
            {
                string Extension = System.IO.Path.GetExtension(dlg.FileName);
                switch (Extension.ToLower())
                {
                    case ".csv":
                        _scaled = false;
                        LoadPointClass(dlg.FileName);
                        break;
                    case ".tif":
                        _scaled = true;
                        LoadTifImages(dlg.FileName);
                        break;
                    default:
                        if (Extension.ToLower() == ".jfif" || Extension.ToLower() == ".jpg")
                        {
                            _scaled = true;
                            LoadTifImages(dlg.FileName);
                        }
                        else
                        {
                            Trace.WriteLine("Falscher Format");
                        }
                        break;
                }
            }
        }

        private void MyDotViewer_ShowToolTip(object sender, NodeTipEventArgs e)
        {
            e.Handled = true;
            string key = e.Tag as string;
            if (key != null)
            {
                _tips.TryGetValue(key, out e.Content);
            }
        }

        private void PrintExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                MyDotViewer.Print(pd);
            }
        }

        private void LoadPointClass(string file)
        {
            _dataList = DataManager.LoadPointCloudFromCsv(file);
            if (_dataList != null)
            {
                _tips.Clear();
                _nodes.Clear();
                _vVNodes.Clear();
                _edges.Clear();
                _erosionDilatationValue = 0;
                tbErosionDilatation.Text = $"{_erosionDilatationValue}";
                _openingClosingValue = 0;
                tbOpeningClosing.Text = $"{_openingClosingValue}";
                _fileName = System.IO.Path.GetFileName(file);
                Title = $"Pattern Analyser - Voronoi Diagramm ({_fileName}) - Status: wird berechnet...";
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _voronoiGraph = Fortune.ComputeVoronoiGraph(_dataList.ToIEnumerableVectorList());
                _voronoiGraphCopy = _voronoiGraph;
                stopWatch.Stop();
                _nodes = _dataList.ToIEnumerableNodeList(ref _boundingBox).ToList();
                _vVNodes = _nodes;
                _scaled = true;
                _edges = _voronoiGraph.Edges.MapEdgeHashSetToEdgeList(ActualHeight, true, _scaled).ToList();
                this.MyDotViewer.LoadPlain(_nodes, _edges, _fileName, _boundingBox);
                _tips.Clear();
                _tips = _dataList.GetOneKeyValues();
                Title = $"Pattern Analyser - Voronoi Diagramm ({_fileName}) - Dauer: {stopWatch.ElapsedMilliseconds.ToString()} ms";
                _frame = null;
            }
        }

        private void LoadTifImages(string file)
        {
            _fileName = System.IO.Path.GetFileName(file);
            _frame = BitmapDecoder.Create(new Uri(file), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames.First();
            int pixelSize = _frame.Format.BitsPerPixel / 8;
            int stride = _frame.PixelWidth * pixelSize;
            int size = _frame.PixelHeight * stride;
            _imagePixels = new byte[size];
            _frame.CopyPixels(_imagePixels, stride, 0);
            _tips.Clear();
            _nodes.Clear();
            _vVNodes.Clear();
            _edges.Clear();
            _erosionDilatationValue = 0;
            tbErosionDilatation.Text = $"{_erosionDilatationValue}";
            _openingClosingValue = 0;
            tbOpeningClosing.Text = $"{_openingClosingValue}";
            int NodeCounter = 0;
            // Take every four pixel of the image
            for (int x = 0; x < _frame.PixelWidth; x = x + 12)
            {
                for (int y = 0; y < _frame.PixelHeight; y = y + 12)
                {
                    int pixelIndex = y * stride + pixelSize * x;
                    int pixelGrayScale = (int)((_imagePixels[pixelIndex] * .21) + (_imagePixels[pixelIndex + 1] * .71) + (_imagePixels[pixelIndex + 2] * .071));
                    
                    if (pixelGrayScale <= _grayScaleValue)
                    {
                        _nodes.Add(new Node("", x, y, 1));
                        _tips.Add($"Node_{NodeCounter}", NodeCounter);
                        NodeCounter++;
                    }
                    else if (pixelGrayScale > _grayScaleValue && pixelGrayScale < 220)
                    {
                        _nodes.Add(new Node("", x, y, 0));
                        _tips.Add($"Node_{NodeCounter}", NodeCounter);
                        NodeCounter++;
                    }
                }
            }
            _vVNodes = _nodes;
            _boundingBox = new double[]
            {
                -50, -20, _frame.PixelWidth + 50, _frame.PixelHeight + 20
            };
            Title = $"Pattern Analyser - Voronoi Diagramm ({_fileName}) - Status: wird berechnet...";
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            _voronoiGraph = Fortune.ComputeVoronoiGraph(_nodes.MapNodeToVector());
            _voronoiGraphCopy = _voronoiGraph;
            stopWatch.Stop();
            _edges = _voronoiGraph.Edges.MapEdgeHashSetToEdgeList(ActualHeight, true, _scaled).ToList();
            this.MyDotViewer.LoadPlain(_nodes, _edges, _fileName, _boundingBox, _frame, 1);

            Title = $"Pattern Analyser - Voronoi Diagramm ({_fileName}) - Dauer: {stopWatch.ElapsedMilliseconds.ToString()} ms";
            //this.MyDotViewer.LoadSourceImage(_frame, System.IO.Path.GetFileName(file));
        }

        private void VoronoiDiagramCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void VoronoiDiagramCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                _erosionDilatationValue = 0;
                tbErosionDilatation.Text = $"{_erosionDilatationValue}";
                _openingClosingValue = 0;
                tbOpeningClosing.Text = $"{_openingClosingValue}";
                _edges = _voronoiGraph.Edges.MapEdgeHashSetToEdgeList(ActualHeight, true, _scaled).ToList(); ;
                this.MyDotViewer.LoadPlain(_nodes, _edges, _fileName, _boundingBox, _frame, 0.3);
                _tips.Clear();
                _tips = _dataList.GetOneKeyValues();
                Title = $"Pattern Analyser - Voronoi Diagramm ({_fileName})";
            }
        }

        private void DelaunayDiagramCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void DelaunayDiagramCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                _erosionDilatationValue = 0;
                tbErosionDilatation.Text = $"{_erosionDilatationValue}";
                _openingClosingValue = 0;
                tbOpeningClosing.Text = $"{_openingClosingValue}";
                _edges = _voronoiGraph.Edges.MapEdgeHashSetToEdgeList(ActualHeight, false, _scaled).ToList(); ;
                this.MyDotViewer.LoadPlain(_nodes, _edges, _fileName, _boundingBox, _frame, 0.3);
                _tips.Clear();
                _tips = _dataList.GetOneKeyValues();
                Title = $"Pattern Analyser - Delaunay Triangulation ({_fileName})";
            }
        }

        private void BtnErosionCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null && _erosionDilatationValue > -20)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnErosionCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _erosionDilatationValue--;
            tbErosionDilatation.Text = $"{_erosionDilatationValue}";
            //_voronoiGraph = _voronoiGraphCopy;
            _voronoiGraph = Fortune.ErosionVG(_voronoiGraphCopy, _voronoiGraph, ref _vVNodes);
            if (_voronoiGraph != null)
            {
                _edges = _voronoiGraph.Edges.MapEdgeHashSetToEdgeList(ActualHeight, false, _scaled).ToList(); ;
                this.MyDotViewer.LoadPlain(_vVNodes, _edges, _fileName, _boundingBox, _frame, (_frame != null? 1: 0.3));
                _tips.Clear();
                _tips = _dataList.GetOneKeyValues();
                Title = $"Pattern Analyser - Erosion ({_erosionDilatationValue}) - Delaunay Triangulation ({_fileName})";
            }
        }

        private void BtnDilatationCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null && _erosionDilatationValue < 20)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnDilatationCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_voronoiGraphCopy.Edges.Count != _voronoiGraph.Edges.Count)
            {
                _erosionDilatationValue++;
                tbErosionDilatation.Text = $"{_erosionDilatationValue}";
                _voronoiGraph = Fortune.DilationVG(_voronoiGraphCopy, _voronoiGraph, ref _vVNodes);
                if (_voronoiGraph != null)
                {
                    _edges = _voronoiGraph.Edges.MapEdgeHashSetToEdgeList(ActualHeight, false, _scaled).ToList(); ;
                    this.MyDotViewer.LoadPlain(_vVNodes, _edges, _fileName, _boundingBox, _frame, (_frame != null ? 1 : 0.3));
                    _tips.Clear();
                    _tips = _dataList.GetOneKeyValues();
                    Title = $"Pattern Analyser - Dilatation ({_erosionDilatationValue}) - Delaunay Triangulation ({_fileName})";
                }
            }
        }

        private void BtnOpeningCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null && _openingClosingValue > -20)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnOpeningCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _openingClosingValue--;
            tbOpeningClosing.Text = $"{_openingClosingValue}";
            _voronoiGraph = Fortune.OpeningVG(_voronoiGraphCopy, _voronoiGraph, ref _vVNodes);
            if (_voronoiGraph != null)
            {
                _edges = _voronoiGraph.Edges.MapEdgeHashSetToEdgeList(ActualHeight, false, _scaled).ToList(); ;
                this.MyDotViewer.LoadPlain(_vVNodes, _edges, _fileName, _boundingBox, _frame, (_frame != null ? 1 : 0.3));
                _tips.Clear();
                _tips = _dataList.GetOneKeyValues();
                Title = $"Pattern Analyser - Opening ({_openingClosingValue}) - Delaunay Triangulation ({_fileName})";
            }
        }

        private void BtnClosingCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null && _openingClosingValue < 20)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnClosingCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _openingClosingValue++;
            tbOpeningClosing.Text = $"{_openingClosingValue}";
            _voronoiGraph = Fortune.ClosingVG(_voronoiGraphCopy, _voronoiGraph, ref _vVNodes);
            if (_voronoiGraph != null)
            {
                _edges = _voronoiGraph.Edges.MapEdgeHashSetToEdgeList(ActualHeight, false, _scaled).ToList(); ;
                this.MyDotViewer.LoadPlain(_vVNodes, _edges, _fileName, _boundingBox, _frame, (_frame != null ? 1 : 0.3));
                _tips.Clear();
                _tips = _dataList.GetOneKeyValues();
                Title = $"Pattern Analyser - Closing ({_openingClosingValue}) - Delaunay Triangulation ({_fileName})";
            }
        }

        private void BtnASFCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnASFCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void BtnGReconstructionCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnGReconstructionCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void BtnDistanceFunctionCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnDistanceFunctionCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void BtnSkeletonCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnSkeletonCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void BtnOpennessCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnOpennessCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void BtnClosenessCommandHandler_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_voronoiGraph != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BtnClosenessCommandHandler_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void CloseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // Display message box
            MessageBoxResult result = MessageBox.Show("Sind Sie sich sicher, dass Sie die Anwendung beenden möchten?", "Meldung - Anwendung beenden", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    PAMainWindow.Close();
                    break;
                case MessageBoxResult.No:
                    // User pressed No button
                    // ... Do nothing
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }

        private void PAMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Display message box
            MessageBoxResult result = MessageBox.Show("Sind Sie sich sicher, dass Sie die Anwendung beenden möchten?", "Meldung - Anwendung beenden", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    e.Cancel = false;
                    break;
                case MessageBoxResult.No:
                    e.Cancel = true;
                    break;
                default:
                    e.Cancel = true;
                    break;
            }
        }
    }
}
