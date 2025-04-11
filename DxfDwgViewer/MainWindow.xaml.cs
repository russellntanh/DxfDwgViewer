using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DxfDwgViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CadDocument cadDocument = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(DrawingCanvas);
            MousePositionText.Text = $"X: {p.X}, Y: {p.Y}";
        }

        private void ImportDXF_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "DXF Files (*.dxf)|*.dxf|DWG Files (*.dwg)|*.dwg|All Files (*.*)|*.*" };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();
                cadDocument = null;

                if (fileExtension == ".dxf")
                {
                    using (DxfReader dxfReader = new DxfReader(filePath))
                    {
                        cadDocument = dxfReader.Read();
                        if (cadDocument == null)
                        {
                            MessageBox.Show("Can't read this DXF file.");
                            return;
                        }
                    }
                }
                else if (fileExtension == ".dwg")
                {
                    using (DwgReader dwgReader = new DwgReader(filePath))
                    {
                        cadDocument = dwgReader.Read();
                        if (cadDocument == null)
                        {
                            MessageBox.Show("Can't read this DWG file.");
                            return;
                        }
                    }
                }

                DrawingCanvas.Children.Clear();

                // check size of drawing
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                foreach (Entity entity in cadDocument.Entities)
                {
                    if (entity is ACadSharp.Entities.Line line)
                    {
                        minX = Math.Min(minX, Math.Min(line.StartPoint.X, line.EndPoint.X));
                        minY = Math.Min(minY, Math.Min(line.StartPoint.Y, line.EndPoint.Y));
                        maxX = Math.Max(maxX, Math.Max(line.StartPoint.X, line.EndPoint.X));
                        maxY = Math.Max(maxY, Math.Max(line.StartPoint.Y, line.EndPoint.Y));
                    }
                    else if (entity is Circle circle)
                    {
                        minX = Math.Min(minX, circle.Center.X - circle.Radius);
                        minY = Math.Min(minY, circle.Center.Y - circle.Radius);
                        maxX = Math.Max(maxX, circle.Center.X + circle.Radius);
                        maxY = Math.Max(maxY, circle.Center.Y + circle.Radius);
                    }
                    else if (entity is LwPolyline lwPolyline)
                    {
                        foreach (var vertex in lwPolyline.Vertices)
                        {
                            minX = Math.Min(minX, vertex.Location.X);
                            minY = Math.Min(minY, vertex.Location.Y);
                            maxX = Math.Max(maxY, vertex.Location.X);
                            maxY = Math.Max(maxY, vertex.Location.Y);
                        }
                    }
                }

                // Tính scale và offset
                double drawingWidth = maxX - minX;
                double drawingHeight = maxY - minY;
                double canvasWidth = DrawingCanvas.ActualWidth;
                double canvasHeight = DrawingCanvas.ActualHeight;
                double scale = Math.Min(canvasWidth / drawingWidth, canvasHeight / drawingHeight) * 0.5; // 90% để chừa lề
                double offsetX = minX * scale; // Dịch để tránh tọa độ âm, thêm lề 10
                double offsetY = maxY * scale;  // Dịch Y để căn chỉnh từ trên xuống

                // draw each entity
                foreach (Entity entity in cadDocument.Entities)
                {
                    DrawEntity(entity, scale, offsetX, offsetY);
                }
            }
        }

        private void DrawEntity(Entity entity, double scale, double offsetX, double offsetY)
        {
            switch (entity)
            {
                case ACadSharp.Entities.Arc entityArc:
                    DrawArc(entityArc, scale, offsetX, offsetY);
                    break;
                case ACadSharp.Entities.Circle entityCircle:
                    DrawCircle(entityCircle, scale, offsetX, offsetY);
                    break;
                case ACadSharp.Entities.Line entityLine:
                    DrawLine(entityLine, scale, offsetX, offsetY);
                    break;
                case ACadSharp.Entities.LwPolyline entityLwPolyline:
                    DrawPolyline(entityLwPolyline, scale, offsetX, offsetY);
                    break;
                default:
                    break;
            }
        }

        private void DrawArc(ACadSharp.Entities.Arc entityArc, double scale, double offsetX, double offsetY)
        {
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Red
            };
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure
            {
                StartPoint = new System.Windows.Point((entityArc.Center.X - entityArc.Radius) * scale + offsetX,
                                                    -(entityArc.Center.Y - entityArc.Radius) * scale + offsetY)
            };
            ArcSegment segment = new ArcSegment
            {
                Point = new System.Windows.Point((entityArc.Center.X + entityArc.Radius) * scale + offsetX,
                                                -((entityArc.Center.Y + entityArc.Radius) * scale) + offsetY),
                Size = new System.Windows.Size(entityArc.Radius * 2 * scale,
                                entityArc.Radius * 2 * scale),
                SweepDirection = SweepDirection.Clockwise
            };
            figure.Segments.Add(segment);
            geometry.Figures.Add(figure);
            path.Data = geometry;
            DrawingCanvas.Children.Add(path);
        }

        private void DrawPolyline(ACadSharp.Entities.LwPolyline entityPolyline, double scale, double offsetX, double offsetY)
        {
            Polygon polygon = new Polygon
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
            };
            PointCollection points = new PointCollection();
            foreach (var vertex in entityPolyline.Vertices)
            {
                System.Windows.Point point = new System.Windows.Point
                {
                    X = vertex.Location.X * scale + offsetX,
                    Y = -vertex.Location.Y * scale + offsetY
                };
                points.Add(point);
            }
            polygon.Points = points;
            DrawingCanvas.Children.Add(polygon);
        }

        private void DrawLine(ACadSharp.Entities.Line entityLine, double scale, double offsetX, double offsetY)
        {
            System.Windows.Shapes.Line wpfLine = new System.Windows.Shapes.Line
            {
                X1 = (entityLine.StartPoint.X * scale) + offsetX,
                Y1 = (-entityLine.StartPoint.Y * scale) + offsetY,
                X2 = (entityLine.EndPoint.X * scale) + offsetX,
                Y2 = (-entityLine.EndPoint.Y * scale) + offsetY,
                StrokeThickness = 1,
                Stroke = Brushes.Red,
            };
            DrawingCanvas.Children.Add(wpfLine);
        }

        private void DrawCircle(Circle entityCircle, double scale, double offsetX, double offsetY)
        {
            System.Windows.Shapes.Ellipse wpfCircle = new System.Windows.Shapes.Ellipse
            {
                Width = entityCircle.Radius * 2 * scale,
                Height = entityCircle.Radius * 2 * scale,
                Stroke = Brushes.Red,
                StrokeThickness = 1
            };
            Canvas.SetLeft(wpfCircle, entityCircle.Center.X * scale - entityCircle.Radius * scale + offsetX);
            Canvas.SetTop(wpfCircle, -(entityCircle.Center.Y * scale + entityCircle.Radius * scale) + offsetY);
            DrawingCanvas.Children.Add(wpfCircle);
        }
    }
}