using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Triangulation;

namespace LasView {
    using Color = System.Drawing.Color;

    public class MainForm : GameWindow {

        private GUILayer gui = null;
        private Geometry points = null;
        private Geometry normals = null;

        private Camera mainCam = null;
        
        private Vector3 cameraVel = new Vector3();
        private LASFile file = null;
        private bool firstLoad = true;
        private bool leftButton = false;
        private List<Triangle> tris = null;
        private Vertex[] verts = null;
        private long elapsed = 0;
        private bool wireframe = false;

        private const int NumPoints = 500000;

        public MainForm(int width, int height)
            : base(width, height, GraphicsMode.Default, "LASView")
        {
            this.WindowBorder = WindowBorder.Fixed;
            this.VSync = VSyncMode.Off;

            mainCam = new Camera(0.5f, 10000.0f, MathHelper.PiOver2, new Viewport(width, height));

            Thread thread = new Thread(() => {
                file = new LASFile(@"38075_00_23.las", NumPoints);
            });
            thread.IsBackground = true;
            thread.Start();

            Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(OnMouseButtonDown);
            Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(OnMouseButtonUp);
            Mouse.Move += new EventHandler<MouseMoveEventArgs>(OnMouseMove);
            Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(OnKeyDown);
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e) {
            if (e.Key == Key.W) {
                wireframe = !wireframe;

                GL.PolygonMode(MaterialFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
            }
        }

        private void OnMouseButtonUp(object sender, MouseButtonEventArgs e) {
            if (e.Button == MouseButton.Left) {
                leftButton = false;
            }
        }

        private void OnMouseButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.Button == MouseButton.Left) {
                leftButton = true;
            }
        }

        private void OnMouseMove(object sender, MouseMoveEventArgs e) {
            if (leftButton) {
                mainCam.Yaw += e.XDelta / (float)mainCam.View.Width * MathHelper.TwoPi;
                mainCam.Pitch += e.YDelta / (float)mainCam.View.Height * MathHelper.TwoPi;
            }
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Enable(EnableCap.Texture2D);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.Enable(EnableCap.Texture2D);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);

            GL.ClearColor(new Color4(0.2f, 0.2f, 0.4f, 1.0f));
            GL.Color4(Color4.White);

            gui = new GUILayer(mainCam.View);
            gui.AddLabel("label", "Loading...", 0, 0, Layer.Layer1, Color.White);
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);

            mainCam.View = new Viewport(this.ClientSize.Width, this.ClientSize.Height);

            GL.MatrixMode(MatrixMode.Projection);

            Matrix4 projection = mainCam.GetProjection();
            GL.LoadMatrix(ref projection);

            GL.MatrixMode(MatrixMode.Modelview);

            gui.ResizeView(mainCam.View);
        }

        protected override void OnUpdateFrame(FrameEventArgs e) {
            base.OnUpdateFrame(e);
            bool zInput = false, xInput = false;

            float accel = 150.0f;
            float maxVel = 700.0f;
            float decel = 2000.0f;

            if (Keyboard[Key.Up]) {
                cameraVel.Z += accel;
                zInput = true;
            }
            if (Keyboard[Key.Down]) {
                cameraVel.Z -= accel;
                zInput = true;
            }
            if (Keyboard[Key.Left]) {
                cameraVel.X -= accel;
                xInput = true;
            }
            if (Keyboard[Key.Right]) {
                cameraVel.X += accel;
                xInput = true;
            }

            if (Math.Abs(cameraVel.Z) > maxVel) {
                cameraVel.Z = Math.Sign(cameraVel.Z) * maxVel;
            }

            if (Math.Abs(cameraVel.X) > maxVel) {
                cameraVel.X = Math.Sign(cameraVel.X) * maxVel;
            }

            float decelMag = (float)(decel * e.Time);
            if (!zInput) {
                cameraVel.Z -= Math.Sign(cameraVel.Z) * decelMag;
                if (Math.Abs(cameraVel.Z) < decelMag) {
                    cameraVel.Z = 0.0f;
                }
            }

            if (!xInput) {
                cameraVel.X -= Math.Sign(cameraVel.X) * decelMag;
                if (Math.Abs(cameraVel.X) < decelMag) {
                    cameraVel.X = 0.0f;
                }
            }

            if (xInput || zInput) {
                mainCam.MoveForward((float)(cameraVel.Z * e.Time));
                mainCam.Strafe((float)(cameraVel.X * e.Time));
            }

            if (file != null && file.IsLoaded && firstLoad) {
                gui.UpdateObjectText("label", "Loaded LAS File" + Environment.NewLine + "Triangulating...");

                verts = new Vertex[file.StoredPoints];
                List<Point> points = new List<Point>(verts.Length);

                for (int i = 0; i < verts.Length; i++) {
                    LPoint point = file[(long)i];
                    points.Add(new Point(
                        (uint) i,
                        point.X,
                        point.Y));

                    verts[i] = new Vertex() {
                        X = point.X,
                        Y = point.Z,
                        Z = point.Y
                    };
                }

                Thread thread = new Thread(() => {
                    var tri = new Delaunay(points);
                    tri.PointSelected += (o, p) => gui.UpdateObjectText("label", "Loaded LAS File" + Environment.NewLine + "Triangulating point " + p.PointNum.ToString());
                    
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    tris = (List<Triangle>) tri.CalculateTriangles();
                    sw.Stop();
                    elapsed = sw.ElapsedMilliseconds;
                });
                thread.IsBackground = true;
                thread.Start();

                firstLoad = false;
            }

            if (tris != null) {
                uint[] indices = Triangulate.TriangleListToIndexArray(tris);
                GenerateNormals(verts, indices, true);

                VertexBuffer buffer = new VertexBuffer(verts, indices);
                this.points = new Geometry(BeginMode.Triangles, buffer);

                gui.UpdateObjectText("label",
                    "Loaded LAS File" + Environment.NewLine +
                    "Filename: " + file.Filename + Environment.NewLine +
                    "Signature: " + file.Signature + Environment.NewLine +
                    "Total Points: " + file.NumPoints.ToString() + Environment.NewLine +
                    "Triangles: " + (buffer.NumIndices / 3).ToString() + Environment.NewLine +
                    "Triangulation Time: " + elapsed.ToString() + "ms");

                tris = null;
                verts = null;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 transform = mainCam.GetModelView();
            GL.LoadMatrix(ref transform);

            GL.Light(LightName.Light0, LightParameter.Position, new Vector4(0, 0.3f, 0.4f, 0));

            if (points != null) {
                points.Draw();
            }

            if (normals != null) {
                normals.Draw();
            }

            if (wireframe) {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }
            
            gui.Draw();

            if (wireframe) {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }

            SwapBuffers();
        }


        private void GenerateNormals(Vertex[] verts, uint[] indices, bool interpolate) {
            for (int i = 2; i < indices.Length; i += 3) {
                uint index1 = indices[i - 2],
                     index2 = indices[i - 1],
                     index3 = indices[i];

                Vector3 normal = CalcNormal(verts[index1], verts[index2], verts[index3]);
                if (normal.Y < 0) {
                    normal *= -1.0f;
                }

                if (interpolate) {
                    verts[index1].Normal += normal;
                    verts[index2].Normal += normal;
                    verts[index3].Normal += normal;
                } else {
                    verts[index1].Normal = normal;
                    verts[index2].Normal = normal;
                    verts[index3].Normal = normal;
                }

            }

            for (int i = 0; i < verts.Length; i++) {
                verts[i].Normal = Vector3.NormalizeFast(verts[i].Normal);
            }
        }

        private Vector3 CalcNormal(Vertex v1, Vertex v2, Vertex v3) {
            Vector3 first = new Vector3(v2.X - v1.X, v2.Y - v1.Y, v2.Z - v1.Z);
            Vector3 second = new Vector3(v3.X - v1.X, v3.Y - v1.Y, v3.Z - v1.Z);
            return Vector3.NormalizeFast(Vector3.Cross(first, second));
        }
    }
}
