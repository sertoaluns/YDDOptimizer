using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Windows.Forms;
using CodeWalker.GameFiles;
using YddResize.Core;

namespace YddOptimizer.UI
{
    public enum RenderMode
    {
        Solid,
        Wireframe,
        SolidAndWireframe
    }

    public sealed class PreviewControl : Control
    {
        private class MeshGeometry
        {
            public Vector3[] Vertices;
            public int[] Indices;
        }

        private readonly Dictionary<LodLevel, List<MeshGeometry>> _lodMeshes = new Dictionary<LodLevel, List<MeshGeometry>>();
        private LodLevel _currentLod = LodLevel.High;
        private RenderMode _renderMode = RenderMode.SolidAndWireframe;

        private float _yaw = 35f;
        private float _pitch = 20f;
        private float _zoom = 2.5f;
        private Vector3 _pan = Vector3.Zero;
        private Point _lastMouse;
        private bool _isOrbiting;
        private bool _isPanning;

        private Vector3 _modelCenter = Vector3.Zero;
        private float _modelRadius = 1.0f;

        private int _totalTriangles;
        private int _totalVertices;

        public RenderMode Mode
        {
            get { return _renderMode; }
            set { _renderMode = value; Invalidate(); }
        }

        public LodLevel CurrentLod
        {
            get { return _currentLod; }
            set
            {
                _currentLod = value;
                UpdateStats();
                Invalidate();
            }
        }

        public PreviewControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.FromArgb(24, 26, 32);
            UpdateStats();
        }

        public void LoadFromYdd(byte[] data)
        {
            _lodMeshes.Clear();
            _lodMeshes[LodLevel.High] = new List<MeshGeometry>();
            _lodMeshes[LodLevel.Medium] = new List<MeshGeometry>();
            _lodMeshes[LodLevel.Low] = new List<MeshGeometry>();
            _lodMeshes[LodLevel.VeryLow] = new List<MeshGeometry>();

            if (data == null || data.Length == 0)
            {
                UpdateStats();
                Invalidate();
                return;
            }

            try
            {
                var ydd = YddResizer.LoadYdd(data);
                var drawables = ydd.Drawables;
                if (drawables == null && ydd.Dict != null)
                {
                    var arr = new Drawable[ydd.Dict.Values.Count];
                    ydd.Dict.Values.CopyTo(arr, 0);
                    drawables = arr;
                }
                if (drawables == null && ydd.DrawableDict != null && ydd.DrawableDict.Drawables != null)
                {
                    drawables = ydd.DrawableDict.Drawables.data_items;
                }

                if (drawables != null)
                {
                    foreach (var d in drawables)
                    {
                        if (d == null || d.DrawableModels == null) continue;
                        ExtractMeshes(d.DrawableModels.High, _lodMeshes[LodLevel.High]);
                        ExtractMeshes(d.DrawableModels.Med, _lodMeshes[LodLevel.Medium]);
                        ExtractMeshes(d.DrawableModels.Low, _lodMeshes[LodLevel.Low]);
                        ExtractMeshes(d.DrawableModels.VLow, _lodMeshes[LodLevel.VeryLow]);
                    }
                }

                CalculateBounds();
                ResetCamera();
                UpdateStats();
            }
            catch
            {
                _lodMeshes.Clear();
            }

            Invalidate();
        }

        private void ExtractMeshes(DrawableModel[] models, List<MeshGeometry> list)
        {
            if (models == null) return;
            foreach (var m in models)
            {
                if (m == null || m.Geometries == null) continue;
                foreach (var g in m.Geometries)
                {
                    if (g == null || g.VertexBuffer == null || g.VertexBuffer.Data1 == null || g.IndexBuffer == null)
                        continue;

                    var vb = g.VertexBuffer;
                    var ib = g.IndexBuffer;
                    if (vb.Data1.VertexBytes == null || ib.Indices == null || vb.VertexStride <= 0)
                        continue;

                    int stride = vb.VertexStride;
                    int vcount = Math.Min((int)vb.VertexCount, vb.Data1.VertexBytes.Length / stride);
                    var layout = VertexLayout.Parse(vb.Info.Flags, (ulong)vb.Info.Types, stride);
                    if (layout == null || !layout.CanReadPosition)
                        continue;

                    var geom = new MeshGeometry
                    {
                        Vertices = new Vector3[vcount],
                        Indices = new int[ib.Indices.Length]
                    };

                    byte[] bytes = vb.Data1.VertexBytes;
                    for (int v = 0; v < vcount; v++)
                    {
                        geom.Vertices[v] = layout.ReadPosition(bytes, v);
                    }
                    for (int i = 0; i < ib.Indices.Length; i++)
                    {
                        geom.Indices[i] = ib.Indices[i];
                    }
                    list.Add(geom);
                }
            }
        }

        private void CalculateBounds()
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            bool any = false;

            foreach (var pair in _lodMeshes)
            {
                foreach (var g in pair.Value)
                {
                    foreach (var v in g.Vertices)
                    {
                        min = Vector3.Min(min, v);
                        max = Vector3.Max(max, v);
                        any = true;
                    }
                }
            }

            if (!any)
            {
                _modelCenter = Vector3.Zero;
                _modelRadius = 1.0f;
                return;
            }

            _modelCenter = (min + max) * 0.5f;
            _modelRadius = (max - min).Length() * 0.5f;
            if (_modelRadius < 0.001f) _modelRadius = 1.0f;
        }

        public void ResetCamera()
        {
            _yaw = 35f;
            _pitch = 20f;
            _zoom = 2.5f;
            _pan = Vector3.Zero;
            Invalidate();
        }

        private void UpdateStats()
        {
            _totalTriangles = 0;
            _totalVertices = 0;

            if (_lodMeshes.TryGetValue(_currentLod, out var list) && list != null)
            {
                foreach (var g in list)
                {
                    _totalTriangles += g.Indices.Length / 3;
                    _totalVertices += g.Vertices.Length;
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _lastMouse = e.Location;
            if (e.Button == MouseButtons.Left) _isOrbiting = true;
            if (e.Button == MouseButtons.Right) _isPanning = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) _isOrbiting = false;
            if (e.Button == MouseButtons.Right) _isPanning = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int dx = e.X - _lastMouse.X;
            int dy = e.Y - _lastMouse.Y;
            _lastMouse = e.Location;

            if (_isOrbiting)
            {
                _yaw += dx * 0.6f;
                _pitch = Math.Max(-85f, Math.Min(85f, _pitch + dy * 0.6f));
                Invalidate();
            }
            else if (_isPanning)
            {
                float panSpeed = (_modelRadius / Math.Max(100f, Height)) * _zoom;
                _pan.X += dx * panSpeed;
                _pan.Y -= dy * panSpeed;
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
                _zoom = Math.Max(0.2f, _zoom * 0.85f);
            else if (e.Delta < 0)
                _zoom = Math.Min(20.0f, _zoom * 1.15f);

            Invalidate();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            ResetCamera();
        }

        private struct ProjectedFace
        {
            public PointF P0, P1, P2;
            public float Depth;
            public Color Color;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background gradient
            using (var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(20, 22, 28), Color.FromArgb(10, 11, 14), 90f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            if (!_lodMeshes.TryGetValue(_currentLod, out var meshes) || meshes == null || meshes.Count == 0)
            {
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var brush = new SolidBrush(Color.FromArgb(120, 130, 145)))
                {
                    g.DrawString("Model veya LOD geometrisi bulunamadı\n(Bir YDD dosyası açın)", Font, brush, ClientRectangle, sf);
                }
                return;
            }

            int w = Width;
            int h = Height;
            if (w < 10 || h < 10) return;

            float radYaw = _yaw * (float)Math.PI / 180f;
            float radPitch = _pitch * (float)Math.PI / 180f;

            Matrix4x4 rot = Matrix4x4.CreateRotationZ(-radYaw) * Matrix4x4.CreateRotationX(radPitch);
            Vector3 lightDir = Vector3.Normalize(new Vector3(0.5f, -0.6f, 0.8f));

            float dist = _modelRadius * _zoom;
            float aspect = (float)w / h;
            float fov = 45f * (float)Math.PI / 180f;
            float tanHalfFov = (float)Math.Tan(fov / 2f);

            var faceList = new List<ProjectedFace>();

            foreach (var mesh in meshes)
            {
                var vWorld = new Vector3[mesh.Vertices.Length];
                for (int i = 0; i < mesh.Vertices.Length; i++)
                {
                    Vector3 local = mesh.Vertices[i] - _modelCenter;
                    Vector3 rotated = Vector3.Transform(local, rot);
                    // Camera looks down -Z axis
                    vWorld[i] = new Vector3(rotated.X + _pan.X, rotated.Y + _pan.Y, rotated.Z - dist);
                }

                int numTris = mesh.Indices.Length / 3;
                for (int t = 0; t < numTris; t++)
                {
                    int i0 = mesh.Indices[t * 3];
                    int i1 = mesh.Indices[t * 3 + 1];
                    int i2 = mesh.Indices[t * 3 + 2];

                    if (i0 >= vWorld.Length || i1 >= vWorld.Length || i2 >= vWorld.Length) continue;

                    Vector3 p0 = vWorld[i0];
                    Vector3 p1 = vWorld[i1];
                    Vector3 p2 = vWorld[i2];

                    // Near plane clipping
                    if (p0.Z >= -0.05f || p1.Z >= -0.05f || p2.Z >= -0.05f) continue;

                    // Face normal
                    Vector3 e1 = p1 - p0;
                    Vector3 e2 = p2 - p0;
                    Vector3 norm = Vector3.Normalize(Vector3.Cross(e1, e2));

                    // Backface culling
                    if (Vector3.Dot(norm, p0) >= 0) continue;

                    // Diffuse lighting
                    float diffuse = Math.Max(0.15f, Vector3.Dot(norm, lightDir));
                    int colVal = (int)Math.Max(35, Math.Min(235, diffuse * 230f));
                    Color faceColor = Color.FromArgb(colVal, (int)(colVal * 0.95f), (int)(colVal * 0.92f));

                    // Project to screen
                    PointF s0 = ProjectPoint(p0, w, h, tanHalfFov, aspect);
                    PointF s1 = ProjectPoint(p1, w, h, tanHalfFov, aspect);
                    PointF s2 = ProjectPoint(p2, w, h, tanHalfFov, aspect);

                    float depth = (p0.Z + p1.Z + p2.Z) / 3f;

                    faceList.Add(new ProjectedFace
                    {
                        P0 = s0,
                        P1 = s1,
                        P2 = s2,
                        Depth = depth,
                        Color = faceColor
                    });
                }
            }

            // Painter's algorithm (sort back-to-front)
            faceList.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            using (var solidBrush = new SolidBrush(Color.White))
            using (var wirePen = new Pen(Color.FromArgb(70, 110, 160, 210), 1f))
            {
                foreach (var face in faceList)
                {
                    var pts = new[] { face.P0, face.P1, face.P2 };
                    if (_renderMode == RenderMode.Solid || _renderMode == RenderMode.SolidAndWireframe)
                    {
                        solidBrush.Color = face.Color;
                        g.FillPolygon(solidBrush, pts);
                    }
                    if (_renderMode == RenderMode.Wireframe || _renderMode == RenderMode.SolidAndWireframe)
                    {
                        g.DrawPolygon(wirePen, pts);
                    }
                }
            }

            // Draw HUD Info
            DrawHud(g);
        }

        private static PointF ProjectPoint(Vector3 p, int w, int h, float tanHalfFov, float aspect)
        {
            float z = -p.Z;
            float x = p.X / (z * tanHalfFov * aspect);
            float y = p.Y / (z * tanHalfFov);

            float sx = (x + 1f) * 0.5f * w;
            float sy = (1f - y) * 0.5f * h;
            return new PointF(sx, sy);
        }

        private void DrawHud(Graphics g)
        {
            string info = string.Format("LOD: {0} | Üçgen: {1:N0} | Vertex: {2:N0}",
                _currentLod, _totalTriangles, _totalVertices);

            string controls = "Sol tık: Döndür | Sağ tık: Kaydır | Tekerlek: Yakınlaş";

            using (var bgBrush = new SolidBrush(Color.FromArgb(170, 15, 17, 22)))
            using (var textBrush = new SolidBrush(Color.FromArgb(220, 225, 235)))
            using (var subBrush = new SolidBrush(Color.FromArgb(140, 150, 165)))
            using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8f))
            {
                g.FillRectangle(bgBrush, 8, 8, 300, 48);
                g.DrawString(info, font, textBrush, 12, 12);
                g.DrawString(controls, subFont, subBrush, 12, 32);
            }
        }
    }
}
