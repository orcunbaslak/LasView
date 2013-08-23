using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using Triangulation;
using TPoint = Triangulation.Point;

namespace TriangulateTest {
    public class TestFramework : Form {
        private Button goButton;
        private PictureBox canvas;
        private Button clearButton;
        private List<TPoint> points = new List<TPoint>();
        private List<Triangle> tris = null;
        private TPoint selectedPoint = null;
        private ListBox logBox;
        private SplitContainer splitContainer1;
        private Edge selectedEdge = null;
        private CheckBox showCircleBox;
        private CheckBox clipEdgesBox;
        private int curX = 0;

        public TestFramework() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            this.goButton = new System.Windows.Forms.Button();
            this.canvas = new System.Windows.Forms.PictureBox();
            this.clearButton = new System.Windows.Forms.Button();
            this.logBox = new System.Windows.Forms.ListBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.showCircleBox = new System.Windows.Forms.CheckBox();
            this.clipEdgesBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // goButton
            // 
            this.goButton.Location = new System.Drawing.Point(12, 12);
            this.goButton.Name = "goButton";
            this.goButton.Size = new System.Drawing.Size(85, 28);
            this.goButton.TabIndex = 0;
            this.goButton.Text = "Go";
            this.goButton.UseVisualStyleBackColor = true;
            this.goButton.Click += new System.EventHandler(this.OnGoClick);
            // 
            // canvas
            // 
            this.canvas.BackColor = System.Drawing.Color.White;
            this.canvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas.Location = new System.Drawing.Point(0, 0);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(529, 530);
            this.canvas.TabIndex = 1;
            this.canvas.TabStop = false;
            this.canvas.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
            this.canvas.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(103, 12);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(85, 28);
            this.clearButton.TabIndex = 2;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.OnClearClick);
            // 
            // logBox
            // 
            this.logBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logBox.FormattingEnabled = true;
            this.logBox.IntegralHeight = false;
            this.logBox.Location = new System.Drawing.Point(0, 0);
            this.logBox.Name = "logBox";
            this.logBox.Size = new System.Drawing.Size(204, 530);
            this.logBox.TabIndex = 3;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 46);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.logBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.canvas);
            this.splitContainer1.Size = new System.Drawing.Size(737, 530);
            this.splitContainer1.SplitterDistance = 204;
            this.splitContainer1.TabIndex = 4;
            // 
            // showCircleBox
            // 
            this.showCircleBox.AutoSize = true;
            this.showCircleBox.Location = new System.Drawing.Point(220, 19);
            this.showCircleBox.Name = "showCircleBox";
            this.showCircleBox.Size = new System.Drawing.Size(118, 17);
            this.showCircleBox.TabIndex = 5;
            this.showCircleBox.Text = "Show Circumcircles";
            this.showCircleBox.UseVisualStyleBackColor = true;
            this.showCircleBox.CheckedChanged += new System.EventHandler(this.OnCircleShowChange);
            // 
            // clipEdgesBox
            // 
            this.clipEdgesBox.AutoSize = true;
            this.clipEdgesBox.Location = new System.Drawing.Point(344, 19);
            this.clipEdgesBox.Name = "clipEdgesBox";
            this.clipEdgesBox.Size = new System.Drawing.Size(127, 17);
            this.clipEdgesBox.TabIndex = 6;
            this.clipEdgesBox.Text = "Clip edges to polygon";
            this.clipEdgesBox.UseVisualStyleBackColor = true;
            this.clipEdgesBox.CheckedChanged += new System.EventHandler(this.OnClipEdgesChanged);
            // 
            // TestFramework
            // 
            this.ClientSize = new System.Drawing.Size(761, 588);
            this.Controls.Add(this.clipEdgesBox);
            this.Controls.Add(this.showCircleBox);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.goButton);
            this.Name = "TestFramework";
            this.ShowIcon = false;
            this.Text = "Triangulation Test";
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void OnMouseClick(object sender, MouseEventArgs e) {
            points.Add(new TPoint((uint) points.Count, e.X, e.Y));
            canvas.Invalidate();
        }

        private void OnPaint(object sender, PaintEventArgs e) {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            TPoint lastPoint = null;
            foreach (TPoint point in points) {
                if (point == selectedPoint) {
                    e.Graphics.FillRectangle(Brushes.Red, point.X - 2, point.Y - 2, 5, 5);
                } else {
                    e.Graphics.DrawRectangle(Pens.Black, point.X - 1, point.Y - 1, 2, 2);
                }
                if (clipEdgesBox.Checked && lastPoint != null) {
                    e.Graphics.DrawLine(Pens.Black, lastPoint.X, lastPoint.Y, point.X, point.Y);
                }
                e.Graphics.DrawString(point.Index.ToString(), SystemFonts.DefaultFont, Brushes.Black, new PointF(point.X, point.Y));
                lastPoint = point;
            }

            TPoint first = points.FirstOrDefault();
            if (clipEdgesBox.Checked && first != null && lastPoint != null) {
                e.Graphics.DrawLine(Pens.Black, lastPoint.X, lastPoint.Y, first.X, first.Y);
            }

            if (tris != null) {
                foreach (Triangle t in tris) {
                    DrawEdge(e.Graphics, t.Edge1);
                    DrawEdge(e.Graphics, t.Edge2);
                    DrawEdge(e.Graphics, t.Edge3);
                    
                    if (showCircleBox.Checked) {
                        int radius2 = (int) t.Circumcircle.Radius * 2;
                        Rectangle rect = new Rectangle(
                            (int) (t.Circumcircle.Center.X - t.Circumcircle.Radius), 
                            (int) (t.Circumcircle.Center.Y - t.Circumcircle.Radius),
                            radius2, 
                            radius2);
                        e.Graphics.DrawEllipse(Pens.Tomato, rect);
                    }
                }
            }

            if (!goButton.Enabled) {
                e.Graphics.DrawLine(Pens.LightBlue, curX, 0, curX, canvas.Height);
            }
        }

        private void DrawEdge(Graphics g, Edge e) {
            if (selectedEdge == e) {
                g.DrawLine(Pens.Lime, e.First.X, e.First.Y, e.Second.X, e.Second.Y);
            } else {
                g.DrawLine(Pens.Blue, e.First.X, e.First.Y, e.Second.X, e.Second.Y);
            }
        }

        private void OnGoClick(object sender, EventArgs e) {
            logBox.Items.Clear();
            goButton.Enabled = false;
            clearButton.Enabled = false;

            var triangulator = new Delaunay(points);
            triangulator.PointSelected += (o, pe) => {
                selectedPoint = pe.SelectedPoint;
                BeginInvoke(new Action(() => {
                    curX = (int) pe.SelectedPoint.X;
                    logBox.Items.Insert(0, String.Format(
                        "Point {0} selected ({1:#},{2:#})",
                        pe.SelectedPoint.Index,
                        pe.SelectedPoint.X,
                        pe.SelectedPoint.Y
                        ));
                    canvas.Invalidate();
                }));
                Thread.Sleep(100);
            };

            EventHandler<TrianglesModifiedArgs> triEvent = (o, te) => {
                tris = te.NewTriangles.ToList();
                BeginInvoke(new Action(() => {
                    logBox.Items.Insert(0, String.Format(
                        "Triangles {0}: New count: {1}",
                        te.EventType,
                        te.NewTriangles.Count()
                        ));
                    canvas.Invalidate();
                }));
                Thread.Sleep(100);
            };

            triangulator.CreatedTriangles += triEvent;
            triangulator.RemovedTriangles += triEvent;

            EventHandler<EdgeArgs> edgeEvent = (o, ee) => {
                selectedEdge = ee.Edge;
                BeginInvoke(new Action(() => {
                    logBox.Items.Insert(0, String.Format(
                        "Edge {0} between {1} and {2}",
                        ee.EventType,
                        ee.Edge.First.Index,
                        ee.Edge.Second.Index
                        ));
                    canvas.Invalidate();
                }));
                Thread.Sleep(50);
            };

            triangulator.EdgeAdded += edgeEvent;
            triangulator.EdgeRemoved += edgeEvent;

            ThreadPool.QueueUserWorkItem(o => {
                tris = triangulator.CalculateTriangles().ToList();
                BeginInvoke(new Action(() => {
                    selectedEdge = null;
                    selectedPoint = null;

                    goButton.Enabled = true;
                    clearButton.Enabled = true;
                    canvas.Invalidate();
                }));
            });
        }

        private void OnClearClick(object sender, EventArgs e) {
            tris.Clear();
            points.Clear();

            canvas.Invalidate();
        }

        private void OnCircleShowChange(object sender, EventArgs e) {
            canvas.Invalidate();
        }

        private void OnClipEdgesChanged(object sender, EventArgs e) {
            canvas.Invalidate();
        }

    }

    public static class Program {
        public static void Main(string[] args) {
            Application.Run(new TestFramework());
        }
    }
}
