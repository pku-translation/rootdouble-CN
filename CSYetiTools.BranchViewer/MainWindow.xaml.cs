using AdonisUI;
using CSYetiTools.Base;
using CSYetiTools.Base.Branch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using Path = System.IO.Path;

namespace CSYetiTools.BranchViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly SortedDictionary<int, Graph> _graphTable = new();

    private readonly Dictionary<int, int> _indexTable = new();

    private string? _dataFolder;

    private bool _darkTheme;

    private bool _forbidListEvent;

    private readonly double[] _scaleLevels = { 0.125, 0.25, 0.5, 1, 2, 4, 8 };
    private readonly int _centerLevel = 3;

    private class NavigateTarget
    {
        public int Index { get; init; }
        public Point? ScrollOffset { get; set; }
        public int? ScaleLevel { get; set; }
    }

    public MainWindow(FilePath dataFolder)
    {
        _dataFolder = dataFolder;
        InitializeComponent();
        MainContent.Visibility = Visibility.Hidden;
        LoadingGrid.Visibility = Visibility.Visible;
        LoadingProgressBar.Value = 0;
    }

    public record GraphItem(string Name, Graph Graph);

    private enum SearchingColor { White, Black }

    private class SearchingNode
    {
        public bool IsEntry { get; init; }
        public Node Node { get; init; } = null!;
        public UIElement Element { get; init; } = null!;
        public Size Size { get; init; }
        public Point Position { get; set; }
        public int Level { get; set; }
        public SearchingColor Color { get; set; }
    }

    protected async override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (_dataFolder == null) {
            return;
        }
        if (!Directory.Exists(_dataFolder)) {
            MessageBox.Show(this, $"Folder '{Path.GetFullPath(_dataFolder)}' not exists", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }
        var entries = new List<(string, int)>();
        foreach (var file in Directory.EnumerateFiles(_dataFolder, "*.yaml")) {
            if (int.TryParse(Path.GetFileNameWithoutExtension(file), out var index)) {
                entries.Add((file, index));
            }
        }
        foreach (var i in ..entries.Count) {
            var (file, index) = entries[i];
            try {
                _graphTable[index] = await Task.Run(() => Graph.Load(file));
            }
            catch (Exception exc) {
                MessageBox.Show(this, $"Failed to load {Path.GetFullPath(file)}: \n" + exc, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            LoadingProgressBar.Value = (double)i / entries.Count * 100;
        }

        foreach (var (i, (key, graph)) in _graphTable.WithIndex()) {
            GraphList.Items.Add(new GraphItem($"[{key:0000}] {graph.SceneTitle}", graph));
            _indexTable.Add(graph.Index, i);
        }

        var query = from entry in _graphTable
                    let count = entry.Value.NodeTable.Values.Sum(node => node.Adjacents.Count)
                    orderby count descending
                    select new { Name = $"[{entry.Key:0000}] {entry.Value.SceneTitle}", Count = count };
        foreach (var (i, s) in query.TakeWhile(s => s.Count > 5).WithIndex()) {
            Debug.WriteLine($"#{i:000}: {s.Name} ({s.Count})");
        }

        LoadingGrid.Visibility = Visibility.Hidden;
        MainContent.Visibility = Visibility.Visible;
        _dataFolder = null;

        if (_graphTable.Count > 0) {
            NavigateTo(new NavigateTarget { Index = 0, ScrollOffset = new Point() }, false);
        }
    }

    private readonly List<NavigateTarget> _navigateQueue = new();
    private int _currentNavigate = -1;

    private void NavigateToScript(int script)
    {
        if (_indexTable.TryGetValue(script, out var index)) {
            NavigateTo(new NavigateTarget { Index = index }, false);
        }
        else {
            MessageBox.Show(this, $"Script {index:0000} data not found", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void NavigateTo(NavigateTarget target, bool fromList)
    {
        if (_currentNavigate >= 0 && _currentNavigate + 1 < _navigateQueue.Count) {
            _navigateQueue.RemoveRange(_currentNavigate + 1, _navigateQueue.Count - _currentNavigate - 1);
        }
        _navigateQueue.Add(target);
        ++_currentNavigate;
        if (!fromList) {
            _forbidListEvent = true;
            GraphList.SelectedIndex = _navigateQueue[_currentNavigate].Index;
            _forbidListEvent = false;
        }
        SetGraph();
    }

    private void NavigateBack()
    {
        if (_currentNavigate == 0) {
            return;
        }
        --_currentNavigate;
        _forbidListEvent = true;
        GraphList.SelectedIndex = _navigateQueue[_currentNavigate].Index;
        _forbidListEvent = false;
        SetGraph();
    }

    private void NavigateForward()
    {
        if (_currentNavigate + 1 >= _navigateQueue.Count) {
            return;
        }
        ++_currentNavigate;
        _forbidListEvent = true;
        GraphList.SelectedIndex = _navigateQueue[_currentNavigate].Index;
        _forbidListEvent = false;
        SetGraph();
    }

    private static Hyperlink CreateHyperlink(string content, Action onClick)
    {
        var hyperlink = new Hyperlink { Inlines = { content }, Tag = "Hyper" };
        hyperlink.Click += (_, _) => onClick();
        return hyperlink;
    }

    private UIElement CreateNode(Node node, ICollection<int> entryIndices)
    {
        const int textWidth = 500;
        const int linkWidth = 120;
        var border = new Border {
            Background = new SolidColorBrush(SystemColors.WindowFrameBrush.Color) { Opacity = 0.4 },
            BorderBrush = SystemColors.ActiveBorderBrush,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(5),
        };

        var stackPanel = new StackPanel {
            UseLayoutRounding = true,
            Width = node.Contents.Any(c => c is TextContent) ? textWidth : linkWidth
        };
        if (entryIndices.Count > 0) {
            stackPanel.Children.Add(new TextBlock {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Text = $"[entry {{{string.Join(", ", entryIndices)}}}] #{node.Offset}"
            });
        }
        else {
            stackPanel.Children.Add(new TextBlock {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Text = $"#{node.Offset}"
            });
        }
        foreach (var content in node.Contents) {
            stackPanel.Children.Add(new Separator());
            stackPanel.Children.Add(content switch {
                TextContent { Character: { } ch } text =>
                    new TextBlock {
                        Inlines = {
                                new Run("【" + ch + "】") {FontWeight = FontWeights.Bold}, new LineBreak(), text.Content
                        },
                        ToolTip = text.RawContent,
                        TextWrapping = TextWrapping.Wrap
                    },
                TextContent text =>
                    new TextBlock {
                        Text = text.Content,
                        ToolTip = text.RawContent,
                        TextWrapping = TextWrapping.Wrap,
                    },
                JumpContent jump =>
                    new TextBlock {
                        Inlines = {
                                CreateHyperlink($"<jump {jump.Script}[{jump.Entry}]>", () => NavigateToScript(jump.Script))
                        },
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                CallContent call =>
                    new TextBlock {
                        Inlines = {
                                CreateHyperlink($"<call {call.Script}[{call.Entry}]>", () => NavigateToScript(call.Script))
                        },
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                ReturnContent =>
                    new TextBlock {
                        Text = "<return>",
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        border.Child = stackPanel;
        border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        border.Arrange(new Rect(border.DesiredSize));
        return border;
    }

    private void SetGraph()
    {
        GraphCanvas.Children.Clear();
        var target = _navigateQueue[_currentNavigate];
        GraphList.ScrollIntoView(GraphList.SelectedItem);
        var item = (GraphItem)GraphList.SelectedItem;
        var graph = item.Graph;

        // BFS for node y-positioning
        var searchingTable = new Dictionary<int, SearchingNode>();
        foreach (var (offset, node) in graph.NodeTable) {
            var entryIndices = Enumerable.Range(0, graph.Entries.Count).Where(i => graph.Entries[i] == offset)
                .ToList();
            var nodeElement = CreateNode(node, entryIndices);
            var size = nodeElement.RenderSize;
            searchingTable.Add(offset,
                new SearchingNode {
                    IsEntry = entryIndices.Count > 0,
                    Node = node,
                    Element = nodeElement,
                    Size = size,
                    Level = 0,
                    Color = SearchingColor.White
                });
        }
        var queue = new Queue<SearchingNode>();
        var maxLevel = 0;
        foreach (var node in searchingTable.Values) {
            if (node.Color != SearchingColor.White) {
                continue;
            }
            node.Color = SearchingColor.Black;
            if (!node.IsEntry && node.Level == 0 && node.Node.Adjacents.Count > 0) {
                var minLevel = node.Node.Adjacents.Min(adj => searchingTable[adj].Level);
                if (minLevel > 0) {
                    node.Level = minLevel - 1;
                }
            }
            queue.Enqueue(node);
            while (queue.TryDequeue(out var current)) {
                foreach (var adj in current.Node.Adjacents) {
                    var adjNode = searchingTable[adj];
                    if (adjNode.Color != SearchingColor.White) {
                        continue;
                    }
                    adjNode.Level = current.Level + 1;
                    if (maxLevel < adjNode.Level) {
                        maxLevel = adjNode.Level;
                    }
                    adjNode.Color = SearchingColor.Black;
                    queue.Enqueue(adjNode);
                }
            }
        }

        var tower = Enumerable.Range(0, maxLevel + 1).Select(_ => new List<SearchingNode>()).ToList();
        var sizes = Enumerable.Repeat(new Size(), maxLevel + 1).ToList();
        foreach (var node in searchingTable.Values) {
            tower[node.Level].Add(node);
        }

        // position all levels
        var xSpace = 50.0;
        var ySpace = 50.0;
        for (var level = 0; level <= maxLevel; ++level) {
            var nodes = tower[level];
            var height = nodes.Max(n => n.Size.Height);
            var width = nodes.Sum(n => n.Size.Width + xSpace * 2);
            sizes[level] = new Size(width, height);
        }
        var canvasWidth = sizes.Max(s => s.Width);
        var canvasHeight = sizes.Sum(s => s.Height + 2 * ySpace);
        var y = ySpace;
        for (var level = 0; level <= maxLevel; ++level) {
            var nodes = tower[level];
            var x = (canvasWidth - sizes[level].Width) / 2 + xSpace;
            var height = sizes[level].Height;
            foreach (var node in nodes) {
                node.Position = new Point(x, y + (height - node.Size.Height) / 2);
                Canvas.SetLeft(node.Element, node.Position.X);
                Canvas.SetTop(node.Element, node.Position.Y);
                x += node.Size.Width + xSpace * 2;
            }
            y += sizes[level].Height + ySpace * 2;
        }

        // add arrows
        foreach (var node in searchingTable.Values) {
            foreach (var adj in node.Node.Adjacents) {
                var adjNode = searchingTable[adj];
                var a = new Point(node.Position.X + node.Size.Width / 2, node.Position.Y + node.Size.Height);
                var b = new Point(adjNode.Position.X + adjNode.Size.Width / 2, adjNode.Position.Y);
                var ratio = a.Y < b.Y ? 2 : 4;
                var pa = new Point(a.X, a.Y + ratio * ySpace);
                var pb = new Point(b.X, b.Y - ratio * ySpace);
                var path = new System.Windows.Shapes.Path {
                    IsHitTestVisible = false,
                    StrokeThickness = 2.5,
                    Opacity = 0.8
                };
                var segments = new List<PathSegment>();
                if (a.Y > b.Y && Math.Abs(a.X - b.X) <= 5) {
                    var dx = a.X > canvasWidth / 2 ? -30.0 : 30.0;
                    segments.Add(new BezierSegment(new Point(pa.X + dx, pa.Y), new Point(pb.X + dx, pb.Y), b,
                        true));
                }
                else {
                    segments.Add(new BezierSegment(pa, pb, b, true));
                }
                path.Data = new PathGeometry(new[] { new PathFigure(a, segments, false) });
                path.SetResourceReference(Shape.StrokeProperty, AdonisUI.Brushes.AccentBrush);
                GraphCanvas.Children.Add(path);
            }
        }

        // add nodes
        foreach (var node in searchingTable.Values) {
            GraphCanvas.Children.Add(node.Element);
        }

        // scroll and scale
        GraphCanvas.Width = canvasWidth;
        GraphCanvas.Height = canvasHeight;

        if (target.ScaleLevel is not { } l) {
            l = _centerLevel;
            target.ScaleLevel = _centerLevel;
        }
        var scale = _scaleLevels[l];
        GraphScale.ScaleX = scale;
        GraphScale.ScaleY = scale;
        if (target.ScrollOffset is not { } o) {
            o = new Point((canvasWidth - GraphScroll.ViewportWidth) / 2, 0);
            target.ScrollOffset = o;
        }
        GraphScroll.ScrollToHorizontalOffset(o.X);
        GraphScroll.ScrollToVerticalOffset(o.Y);

        // misc
        SceneTitleText.Text = graph.SceneTitle;
        BackButton.IsEnabled = _currentNavigate > 0;
        ForwardButton.IsEnabled = _currentNavigate + 1 < _navigateQueue.Count;

        GC.Collect();
    }

    private void GraphList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_forbidListEvent) {
            NavigateTo(new NavigateTarget { Index = GraphList.SelectedIndex }, true);
        }
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _darkTheme = !_darkTheme;
        ResourceLocator.SetColorScheme(Application.Current.Resources,
            _darkTheme ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);
        SetGraph();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        NavigateBack();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        NavigateForward();
    }

    #region Scroll

    private Point _scrollMousePoint;
    private double _hOffset;
    private double _vOffset;

    private void GraphScroll_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Run { Parent: Hyperlink }) {
            return;
        }
        if (FindVisualParent<ScrollBar>((DependencyObject)e.OriginalSource) != null) {
            return;
        }
        _scrollMousePoint = e.GetPosition(GraphScroll);
        _hOffset = GraphScroll.HorizontalOffset;
        _vOffset = GraphScroll.VerticalOffset;
        GraphScroll.CaptureMouse();
    }

    private void GraphScroll_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!GraphScroll.IsMouseCaptured) {
            return;
        }
        var hOff = _hOffset + (_scrollMousePoint.X - e.GetPosition(GraphScroll).X);
        var vOff = _vOffset + (_scrollMousePoint.Y - e.GetPosition(GraphScroll).Y);
        GraphScroll.ScrollToHorizontalOffset(hOff);
        GraphScroll.ScrollToVerticalOffset(vOff);
    }

    private void GraphScroll_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        GraphScroll.ReleaseMouseCapture();
    }

    private static TParentItem? FindVisualParent<TParentItem>(DependencyObject obj)
        where TParentItem : DependencyObject
    {
        DependencyObject? parent = obj;
        do {
            if (parent is not Visual) {
                return null;
            }
            parent = VisualTreeHelper.GetParent(parent);
        } while (parent != null && parent is not TParentItem);
        return (TParentItem?)parent;
    }

    #endregion

    private void GraphScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_currentNavigate >= 0 && _currentNavigate < _navigateQueue.Count) {
            _navigateQueue[_currentNavigate].ScrollOffset = new Point(e.HorizontalOffset, e.VerticalOffset);
        }
    }

    private void GraphScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) {
            return;
        }

        if (_currentNavigate < 0 || _currentNavigate >= _navigateQueue.Count) {
            return;
        }

        e.Handled = true;

        var target = _navigateQueue[_currentNavigate];
        if (target.ScaleLevel is { } l) {
            var l1 = e.Delta > 0 ? Math.Min(l + 1, _scaleLevels.Length - 1) : Math.Max(l - 1, 0);
            if (l == l1) {
                return;
            }
            var position = e.GetPosition(GraphCanvas);
            var position2 = e.GetPosition(GraphScroll);
            var scale = _scaleLevels[l1];
            GraphScale.ScaleX = scale;
            GraphScale.ScaleY = scale;
            GraphScroll.ScrollToHorizontalOffset(position.X * scale - position2.X);
            GraphScroll.ScrollToVerticalOffset(position.Y * scale - position2.Y);
            target.ScaleLevel = l1;
        }
        else {
            MessageBox.Show(this, "Scale level not initialized", "Warning", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            target.ScaleLevel = _centerLevel;
        }
    }
}
