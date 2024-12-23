using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpModels
{
    public class DepthMapViewer : GameWindow
    {
        private double rotationX = 0.0;
        private double rotationY = 0.0;
        private double rotationZ = 0.0;
        private double scaleValue = 0.04;

        private float frameTime = 0.0f;
        private int fps = 0;

        public double[,] map;

        public DepthMapViewer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, MapWorker dm)
                : base(gameWindowSettings, nativeWindowSettings)
        {
            Console.WriteLine(GL.GetString(StringName.Version));
            Console.WriteLine(GL.GetString(StringName.Vendor));
            Console.WriteLine(GL.GetString(StringName.Renderer));
            Console.WriteLine(GL.GetString(StringName.ShadingLanguageVersion));

            VSync = VSyncMode.On;

            this.map = dm.depthMap;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(173 / 255.0f, 216 / 255.0f, 230 / 255.0f, 255 / 255.0f);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.Normalize);
           
            float[] pos = new float[4] {0, 0, 0.9f, 0};
            GL.LightModel(LightModelParameter.LightModelTwoSide, 0);
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Light(LightName.Light0, LightParameter.Position, pos);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            int width = this.Size[0];
            int height = this.Size[1];

            Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45), width / height, 1.0f, 100.0f);
            Matrix4 view = Matrix4.LookAt(
                new Vector3(7.5f, 7.5f, 12.5f),
                new Vector3(2.5f, 2.5f, -5.0f),
                new Vector3(0.0f, 1.0f, 0.0f));

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perspective);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref view);

            base.OnResize(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            // Show FPS and Frame Time
            //frameTime += (float)args.Time;
            //fps++;

            //if (frameTime >= 1.0f)
            //{
            //    Title = $"Depth Map Visualization (FPS: {fps}, Frame Time: {frameTime})";
            //    frameTime = 0.0f;
            //    fps = 0;
            //}

            // Process rotation and navigation
            // EXTREMELY UNPRODUCTIVE SOLUTION
            var key = KeyboardState;
            double rotationFactor = 0.5;
            if (key.IsKeyDown(Keys.LeftShift)) rotationFactor *= 2;
            if (key.IsKeyDown(Keys.LeftControl)) rotationFactor /= 2;
            
            if (key.IsKeyDown(Keys.Escape))
            {
                Console.WriteLine(Keys.Escape.ToString());
                Close();
            }

            #region WASDQE rotation
            if (key.IsKeyDown(Keys.W))
            {
                rotationZ += rotationZ < 360.0 ? rotationFactor : -360.0;
            }

            if (key.IsKeyDown(Keys.S))
            {
                rotationZ -= rotationZ < 360.0 ? rotationFactor : -360.0;
            }

            if (key.IsKeyDown(Keys.A))
            {
                rotationX -= rotationFactor;
                if (rotationX > 360) rotationX -= 360.0;

            }

            if (key.IsKeyDown(Keys.D))
            {
                rotationX += rotationFactor;
                if (rotationX < 0) rotationX += 360.0;

            }

            if (key.IsKeyDown(Keys.Q))
            {
                rotationY -= rotationFactor;
                if (rotationY > 360) rotationY -= 360.0;

            }

            if (key.IsKeyDown(Keys.E))
            {
                rotationY += rotationFactor;
                if (rotationY < 0) rotationY += 360.0;

            }
            #endregion

            #region OP Scaling
            if (key.IsKeyDown(Keys.O))
            {
                scaleValue += 0.0005;
            }

            if (key.IsKeyDown(Keys.P))
            {
                scaleValue -= 0.0005;
            }
            #endregion

            base.OnUpdateFrame(args);
        }


        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.LoadIdentity();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse,
                new float[] { 0.6f, 0.6f, 0.6f });
            
            GL.Translate(-8.0, -9.0, -38.0);
            GL.Scale(scaleValue, scaleValue, scaleValue);
            GL.Rotate(rotationX, 1.0, 0.0, 0.0);
            GL.Rotate(rotationY, 0.0, 1.0, 0.0);
            GL.Rotate(rotationZ, 0.0, 0.0, 1.0);

            GL.Begin(PrimitiveType.Triangles);
            for (int i = 0; i < map.GetLength(0) - 2; i++)
            {
                for (int j = 0; j < map.GetLength(1) - 1; j++)
                {
                    if (map[i, j] != 0 && map[i + 1, j] != 0 && map[i + 1, j + 1] != 0 && map[i, j + 1] != 0)
                    {
                        // Нормали строястя на основе общей для обеих полигонов точки
                        // Два треугольника образуют параллелограм 
                        
                        // Первый полигон
                        Vector3 v1 = new Vector3(i - (i + 1), j - j, (float)(map[i, j] - map[i + 1, j]));
                        Vector3 v2 = new Vector3(i - (i + 1), (j + 1) - j, (float)(map[i, j + 1] - map[i + 1, j]));
                        Vector3 normal = Vector3.Cross(v2, v1);

                        GL.Normal3(normal.X, normal.Y, normal.Z);
                        GL.Vertex3(i, j, map[i, j]);
                        GL.Vertex3(i + 1, j, map[i + 1, j]);
                        GL.Vertex3(i, j + 1, map[i, j + 1]);

                        // Второй полигон
                        v1 = new Vector3(i + 1 - (i + 1), j + 1 - j, (float)(map[i + 1, j + 1] - map[i + 1, j]));
                        v2 = new Vector3(i - (i + 1), (j + 1) - j, (float)(map[i, j + 1] - map[i + 1, j]));
                        normal = Vector3.Cross(v1, v2);

                        //GL.Normal3(normal.X, normal.Y, normal.Z);
                        GL.Vertex3(i + 1, j + 1, map[i + 1, j + 1]);
                        GL.Vertex3(i + 1, j, map[i + 1, j]);
                        GL.Vertex3(i, j + 1, map[i, j + 1]);
                    }
                }
            }
            GL.End();

            SwapBuffers();
            base.OnRenderFrame(args);
        }
    }
}
