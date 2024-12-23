using System;
using System.Numerics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpModels
{
    class LightModelViewer : GameWindow
    {
        public int Width;
        public int Height;

        public float frameTime = 0.0f;
        public int fps = 0;

        public int mode = 0;
        public double[,] map;

        private float lightPositionX = 500.0f;
        private float lightPositionY = 300.0f;
        private float lightPositionZ = -1.0f;

        private int objectPositionOffsetX = 0;
        private int objectPositionOffsetY = 0;

        private double scale = 0.12;
        private double rotationX = 0.0;
        private double rotationY = 0.0;

        // Коэффициент диффузного отражения
        private readonly double kd = 0.5;
        // рассеянная составляющая освещенности в точке
        private readonly double id = 1;
        // Коэффициент блеска (свойство материала)
        private readonly double alpha = 100;
        // Неровность поверхности для модели Кука-Торренса
        private readonly double r = 0.05;

        private readonly double A;
        private readonly double B;
        private readonly double sigma = 2;
        
        public LightModelViewer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, MapWorker dm, int lightModel = 0)
                : base(gameWindowSettings, nativeWindowSettings)
        {
            Console.WriteLine(GL.GetString(StringName.Version));
            Console.WriteLine(GL.GetString(StringName.Vendor));
            Console.WriteLine(GL.GetString(StringName.Renderer));
            Console.WriteLine(GL.GetString(StringName.ShadingLanguageVersion));

            VSync = VSyncMode.On;
            this.map = dm.depthMap;
            
            this.mode = lightModel;

            this.A = 1 - 0.5 * (Math.Pow(sigma, 2) / (Math.Pow(sigma, 2) + 0.33)); // Коэффициент А для модели Орена-Найара
            this.B = 0.45 * (Math.Pow(sigma, 2) / (Math.Pow(sigma, 2) + 0.09)); // Коэффициент В для модели Орена-Найара

            this.Width = this.Size[0];
            this.Height = this.Size[1];
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(250 / 255.0f, 250 / 255.0f, 250 / 255.0f, 255 / 255.0f);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            int width = this.Size[0];
            int height = this.Size[1];

            GL.Viewport(0, 0, width, height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, 50.0, 0.0, 50.0, -1.0, 1.0);
            GL.MatrixMode(MatrixMode.Modelview);

            base.OnResize(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            // Show FPS and Frame Time
            frameTime += (float)args.Time;
            fps++;

            if (frameTime >= 1.0f)
            {
                Title = $"Depth Map Lighting (FPS: {fps}, Frame Time: {frameTime})";
                frameTime = 0.0f;
                fps = 0;
            }

            int lightSpeedChange = 4;
            int objectSpeedChange = 8;
            float scaleSpeedChange = 0.001f;
            double rotationFactor = 0.5;

            // Process rotation and navigation
            // EXTREMELY UNPRODUCTIVE SOLUTION
            var key = KeyboardState;

            if (key.IsKeyDown(Keys.LeftShift)) rotationFactor *= 2;
            if (key.IsKeyDown(Keys.LeftControl)) rotationFactor /= 2;

            if (key.IsKeyDown(Keys.Escape))
            {
                Console.WriteLine(Keys.Escape.ToString());
                Close();
            }

            #region WASD Light Position
            if (key.IsKeyDown(Keys.W))
            {
                lightPositionY += lightSpeedChange;
            }

            if (key.IsKeyDown(Keys.S))
            {
                lightPositionY -= lightSpeedChange;
            }

            if (key.IsKeyDown(Keys.A))
            {
                lightPositionX -= lightSpeedChange;

            }

            if (key.IsKeyDown(Keys.D))
            {
                lightPositionX += lightSpeedChange;
            }
            #endregion

            #region Arrows Object Change
            if (key.IsKeyDown(Keys.Up))
            {
                objectPositionOffsetY += objectSpeedChange;
            }

            if (key.IsKeyDown(Keys.Down))
            {
                objectPositionOffsetY -= objectSpeedChange;
            }

            if (key.IsKeyDown(Keys.Left))
            {
                objectPositionOffsetX -= objectSpeedChange;
            }

            if (key.IsKeyDown(Keys.Right))
            {
                objectPositionOffsetX += objectSpeedChange;
            }
            #endregion

            #region QEVB Rotation
            if (key.IsKeyDown(Keys.Q))
            {
                rotationY += rotationFactor;
                if (rotationY > 360) rotationY -= 360.0;
            }

            if (key.IsKeyDown(Keys.E))
            {
                rotationY -= rotationFactor;
                if (rotationY < 0) rotationY += 360.0;
            }

            if (key.IsKeyDown(Keys.R))
            {
                rotationX += rotationFactor;
                if (rotationX > 360) rotationY -= 360.0;
            }

            if (key.IsKeyDown(Keys.F))
            {
                rotationX -= rotationFactor;
                if (rotationX < 0) rotationY += 360.0;
            }
            #endregion

            #region Scaling
            if (key.IsKeyDown(Keys.V))
            {
                scale += scaleSpeedChange;
            }

            if (key.IsKeyDown(Keys.B))
            {
                scale -= scaleSpeedChange;
            }
            #endregion

            #region Changing light mode
            if (key.IsKeyDown(Keys.KeyPad1))
            {
                this.mode = 0;
            }

            if (key.IsKeyDown(Keys.KeyPad2))
            {
                this.mode = 1;
            }

            if (key.IsKeyDown(Keys.KeyPad3))
            {
                this.mode = 2;
            }

            if (key.IsKeyDown(Keys.KeyPad4))
            {
                this.mode = 3;
            }
            #endregion

            base.OnUpdateFrame(args);
        }


        protected override void OnRenderFrame(FrameEventArgs args)
        {
            float observerPositionX = 0;
            float observerPositionY = 0;
            float observerPositionZ = 0;

            GL.LoadIdentity();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Rotate(rotationX, 0.0, 1.0, 0.0);
            GL.Rotate(rotationY, 0.0, 0.0, 1.0);

            GL.Scale(scale, scale, 1);

            GL.PointSize(4);

            GL.Begin(PrimitiveType.Points);
            for (int i = 0; i < map.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < map.GetLength(1) - 1; j++)
                {
                    double I = 0.0;
                    if (map[i, j] != 0 && map[i + 1, j] != 0 && map[i, j + 1] != 0)
                    {
                        /*
                            МОДЕЛЬ ЛАМБЕРТА
                            Простейшая модель освещения - чисто диффузное освещение. 
                         Считается, что свет падающий в точку, одинакового рассеивается по всем 
                         направлением полупространства. Таким образом, освещенность в точке 
                         определяется только плотностью света в точке поверхности, а она линейно 
                         зависит от косинуса угла падения.
                        */
                        if (this.mode == 0)
                        {
                            // Определение нормали для текущей точки и нормализация [0, 1] 
                            Vector3 v1 = new Vector3(0, 1, Convert.ToSingle(map[i, j + 1]) - Convert.ToSingle(map[i, j]));
                            Vector3 v2 = new Vector3(1, 0, Convert.ToSingle(map[i + 1, j]) - Convert.ToSingle(map[i, j]));
                            Vector3 N = Vector3.Normalize(Vector3.Cross(v1, v2));

                            // Получение вектора от точки до источника света + нормализация
                            // Точка
                            Vector3 PP = new Vector3(i, j, Convert.ToSingle(map[i, j]));
                            // Источник света
                            Vector3 PL = new Vector3(lightPositionX, lightPositionY, lightPositionZ);
                            // Вектор от точки к Источнику
                            Vector3 L = Vector3.Normalize(PL - PP);                                      
                            
                            // Вычисление диффузной составляющей
                            double IL = id * kd * Vector3.Dot(L, N);
                                                        
                            I = IL; // Итоговое значение интенсивности
                        }

                        // Модель Фонга
                        // Самая частоиспользуемая модель
                        if (this.mode == 1)
                        {
                            
                            //Определение нормалей в каждой точке
                            Vector3 v1 = new Vector3(0, 1, Convert.ToSingle(map[i, j + 1]) - Convert.ToSingle(map[i, j]));
                            Vector3 v2 = new Vector3(1, 0, Convert.ToSingle(map[i + 1, j]) - Convert.ToSingle(map[i, j]));
                            Vector3 N = Vector3.Normalize(Vector3.Cross(v1, v2));

                            Vector3 PL = new Vector3( lightPositionX, lightPositionY, lightPositionZ);
                            Vector3 PP = new Vector3(i, j, Convert.ToSingle(map[i, j])); //точка изображения
                            Vector3 PV = new Vector3(observerPositionX, observerPositionY, observerPositionZ);// Позиция наблюдателя

                            Vector3 L = Vector3.Normalize(PL - PP); // Вектор от точки к Источнику      
                            Vector3 V = Vector3.Normalize(PV - PP);// Вектор к наблюдателю
                            Vector3 R = Vector3.Reflect(L, N); // Вектор отражения для Фонга

                            double IL = id * kd * Vector3.Dot(L, N);
                            double IFB = Math.Pow(Math.Max(Vector3.Dot(R, V), 0.0), alpha);

                            I = IL + IFB;

                        }

                        // Модель Фонга-Блинна
                        // Усовершенствование рассчетов бликов по модели Фонга
                        // засчет использования вектора полупути вместо вектора
                        // Отраженного луча
                        if (this.mode == 1)
                        {
                            /* В ОТЛИЧИЕ ОТ МОДЕЛИ ФОНГА РАССЧЕТ ВЕКТОРА
                               ОТРАЖЕННОГО ЛУЧА НЕ ТРЕБУЕТСЯ */
                            
                            // Определение нормали для текущей точки и нормализация [0, 1] 
                            Vector3 v1 = new Vector3(0, 1, Convert.ToSingle(map[i, j + 1]) - Convert.ToSingle(map[i, j]));
                            Vector3 v2 = new Vector3(1, 0, Convert.ToSingle(map[i + 1, j]) - Convert.ToSingle(map[i, j]));
                            Vector3 N = Vector3.Normalize(Vector3.Cross(v1, v2));

                            // Получение вектора от точки до источника света и от точки до наблюдателя
                            // Точка
                            Vector3 PP = new Vector3(i, j, Convert.ToSingle(map[i, j]));
                            // Источник света
                            Vector3 PL = new Vector3(lightPositionX, lightPositionY, lightPositionZ);
                            // Позиция наблюдателя (камеры)
                            Vector3 PV = new Vector3(observerPositionX, observerPositionY, observerPositionZ); 

                            Vector3 L = Vector3.Normalize(PL - PP); // Вектор от точки к Источнику      
                            Vector3 V = Vector3.Normalize(PV - PP); // Вектор к наблюдателю
                            Vector3 H = Vector3.Normalize(L + V);   // Вектор полупути ( для модели Фонга-Блинна)

                            // Вычисление диффузной составляющей
                            double IL = id * kd * Vector3.Dot(L, N);
                            
                            // Вычисление зеркальной составляющей
                            double IFB = Math.Pow(Math.Max(Vector3.Dot(N, H), 0.0), alpha);

                            I = IFB + IL; // Итоговое значение интенсивности
                        }

                        /*  
                            Модель Орена-Найера
                            Модель освещения Ламберта хорошо работает только 
                        для сравнительно гладких поверхностей. В отличии от нее 
                        модель Орен-Найара основана на предположении, что поверхность 
                        состоит из множества микрограней, освещение каждой из которых 
                        описывается моделью Ламберта. Модель учитывает взаимное 
                        закрывание и затенение микрограней и также учитывает взаимное 
                        отражение света между микрогранями.
                        
                         */
                        if (this.mode == 2)
                        {
                            Vector3 v1 = new Vector3(0, 1, Convert.ToSingle(map[i, j + 1]) - Convert.ToSingle(map[i, j]));
                            Vector3 v2 = new Vector3(1, 0, Convert.ToSingle(map[i + 1, j]) - Convert.ToSingle(map[i, j]));
                            Vector3 N = Vector3.Normalize(Vector3.Cross(v1, v2));

                            Vector3 PL = new Vector3(lightPositionX, lightPositionY, lightPositionZ);
                            Vector3 PP = new Vector3(i, j, Convert.ToSingle(map[i, j])); //точка изображения
                            Vector3 PV = new Vector3(observerPositionX, observerPositionY, observerPositionZ);// Позиция наблюдателя

                            Vector3 L = Vector3.Normalize(PL - PP); // Вектор от точки к Источнику 
                            Vector3 V = Vector3.Normalize(PV - PP);// Вектор к наблюдателю
                            Vector3 H = Vector3.Normalize(L + V); // Вектор полупути ( для модели Фонга-Блинна)

                            Vector3 NN = Vector3.Normalize(N);
                            Vector3 LN = Vector3.Normalize(L);
                            Vector3 VN = Vector3.Normalize(V);

                            float nl = Vector3.Dot(NN, LN);
                            float nv = Vector3.Dot(NN, VN);

                            float Alpha = Math.Max(nl, nv);
                            float Beta = Math.Min(nl, nv);

                            Vector3 lProj = Vector3.Normalize(LN - NN * nl);
                            Vector3 vProj = Vector3.Normalize(VN - NN * nv);

                            float cx = Math.Max(Vector3.Dot(lProj, vProj), 0.0f);

                            double ION = nl * (A + (B * cx)) * Math.Sin(Alpha) * Math.Tan(Beta);
                            double IFB = Math.Pow(Math.Max(Vector3.Dot(N, H), 0.0), alpha);

                            //I = ION + IFB;
                            I = ION;
                        }

                        /* 
                            Модель Кука-Торренса 
                            Одной из наиболее продвинутых и согласованных 
                        с физикой является модель освещение Кука-Торранса. 
                        Она также основана на модели поверхности состоящей 
                        из микрограней, каждая из которых является 
                        идеальным зеркалом. Модель учитывает коэффициент 
                        Френеля и взаимозатенение микрограней.
                         */
                        if (this.mode == 3)
                        {
                            Vector3 v1 = new Vector3(0, 1, Convert.ToSingle(map[i, j + 1]) - Convert.ToSingle(map[i, j]));
                            Vector3 v2 = new Vector3(1, 0, Convert.ToSingle(map[i + 1, j]) - Convert.ToSingle(map[i, j]));
                            Vector3 N = Vector3.Normalize(Vector3.Cross(v1, v2));

                            Vector3 PL = new Vector3(lightPositionX, lightPositionY, lightPositionZ);
                            Vector3 PP = new Vector3(i, j, Convert.ToSingle(map[i, j])); //точка изображения
                            Vector3 PV = new Vector3(observerPositionX, observerPositionY, observerPositionZ);// Позиция наблюдателя

                            Vector3 L = Vector3.Normalize(PL - PP); // Вектор от точки к Источнику 
                            Vector3 V = Vector3.Normalize(PV - PP); // Вектор к наблюдателю
                            Vector3 H = Vector3.Normalize(L + V);   // Вектор полупути ( для модели Фонга-Блинна)

                            // Вычисление цвета точки по модели Кука-Торренса
                            double NdotV = Vector3.Dot(N, V);
                            double NdotH = Vector3.Dot(N, H);
                            double NdotL = Vector3.Dot(N, L);
                            double VdotH = Vector3.Dot(V, H);
                            
                            // Рассчет распределения Бэкмена
                            // (!) Угол между нормалью к микрограни и
                            // нормалью ко всей поверхности является случайной величиной)
                            double r_sq = r * r;
                            double NdotH_sq = NdotH * NdotH;
                            double NdotH_sq_r = 1.0 / (NdotH_sq * r_sq);
                            double roughness_exp = (NdotH_sq - 1.0) * (NdotH_sq_r);
                            double roughness = Math.Exp(roughness_exp) * NdotH_sq_r / (4.0 * NdotH_sq);

                            double r1 = 1.0 / (4.0 * r_sq * Math.Pow(NdotH, 4.0));
                            double r2 = (NdotH * NdotH - 1.0) / (r_sq * NdotH * NdotH);
                            double D = r1 * Math.Exp(r2);

                            // Рассчет функции G, отвечающей за затенение отдельных микрограней
                            double two_NdotH = 2.0 * NdotH;
                            double g1 = (two_NdotH * NdotV) / VdotH;
                            double g2 = (two_NdotH * NdotL) / VdotH;
                            double G = Math.Min(1.0, Math.Min(g1, g2));

                            // Коэффициент Френеля
                            double F = 1.0 / (1.0 + NdotV);

                            // Итоговое значение диффунзной составляющей по модели
                            double Rs = Math.Min(1.0, F * D * G / (NdotL * NdotV + 1.0e-7));

                            // Значение зеркальной составляющей
                            double IFB = Math.Pow(Math.Max(Vector3.Dot(N, H), 0.0f), alpha);
                            I = NdotL * (kd * Vector3.Dot(L, N) +  IFB * Rs);
                        }
                        
                        GL.Color3(I, I, I);
                        GL.Vertex2(j + objectPositionOffsetX, -i + objectPositionOffsetY);
                    }
                }
            }
            GL.End();
            SwapBuffers();
        }
    }
}

