using OpenTK.Mathematics;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Newtonsoft.Json;

namespace WPFLighting
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    public partial class MainWindow : Window
    {

        private float changeLightPositionX = 0;
        private float changeLightPositionY = 0;
        private float changeLightPositionZ = 0;


        private readonly double[,] map;

        private readonly double kd = 0.5; // Коэффициент диффузного отражения
        private readonly double id = 1; // рассеянная составляющая освещенности в точке,
        private readonly double alpha = 100; // Коэффициент блеска (свойство материала)
        private readonly double r = 0.05; // Неровность поверхности для модели Кука-Торренса 

        private readonly Config conf;
        private readonly MapWorker mapWorker;

        public MainWindow()
        {
            InitializeComponent();
            using (StreamReader r = new StreamReader("config.json"))
            {
                string json = r.ReadToEnd();
                this.conf = JsonConvert.DeserializeObject<Config>(json);
            }

            this.mapWorker = new MapWorker(conf.depthMapPath);
            this.mapWorker.loadData();
            this.map = this.mapWorker.depthMap;

            switch (conf.lightingMode)
            {
                case 0:
                    this.radio1.IsChecked = true;
                    break;
                case 1:
                    this.radio2.IsChecked = true;
                    break;
                case 2:
                    this.radio3.IsChecked = true;
                    break;
                default:
                    break;
            }

            this.modelNameLabel.Content = this.conf.outputModelName + "." + this.conf.outputModelFormat;

            this.lightSourceInfo.Text = "x = " + this.conf.lightPositionX.ToString() + "\ny = " + this.conf.lightPositionY.ToString() + "\nz = " + this.conf.lightPositionZ.ToString();
            this.observerInfo.Text = "x = " + this.conf.observerPositionX.ToString() + "\ny = " + this.conf.observerPositionY.ToString() + "\nz = " + this.conf.observerPositionZ.ToString();
            this.outputFileName.Content = this.conf.outputModelName + '.' + this.conf.outputModelFormat;
            this.inputFileName.Content = this.conf.depthMapPath;
            this.RenderObject();
        }

        private void RenderObject()
        {

            byte[,,] pixelArray = new byte[map.GetLength(0), map.GetLength(1), 3];

            for (int i = 0; i < map.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < map.GetLength(1) - 1; j++)
                {
                    double I = 0.0;
                    byte pixelColor = 0;
                    if (map[i, j] != 0 && map[i + 1, j] != 0 && map[i, j + 1] != 0)
                    {
                        // Модель Ламберта
                        if (radio1.IsChecked == true)
                        {
                            //Определение нормалей в каждой точке
                            Vector3 v1 = new Vector3(0, 1, Convert.ToSingle(map[i, j + 1]) - Convert.ToSingle(map[i, j]));
                            Vector3 v2 = new Vector3(1, 0, Convert.ToSingle(map[i + 1, j]) - Convert.ToSingle(map[i, j]));

                            Vector3 N = Vector3.Normalize(Vector3.Cross(v1, v2));
                            Vector3 PL = new Vector3(
                                this.conf.lightPositionX + Convert.ToSingle(changeLightPositionX),
                                this.conf.lightPositionY + Convert.ToSingle(changeLightPositionY),
                                this.conf.lightPositionZ + Convert.ToSingle(changeLightPositionZ));

                            Vector3 PP = new Vector3(i, j, Convert.ToSingle(map[i, j])); //точка изображения

                            Vector3 L = Vector3.Normalize(PL - PP); // Вектор от точки к Источнику      
                            double IL = id * kd * Vector3.Dot(L, N);
                            I = IL;
                        }

                        // Модель Фонга-Блинна
                        if (radio2.IsChecked == true)
                        {
                            //Определение нормалей в каждой точке
                            Vector3 v1 = new Vector3(0, 1, Convert.ToSingle(map[i, j + 1]) - Convert.ToSingle(map[i, j]));
                            Vector3 v2 = new Vector3(1, 0, Convert.ToSingle(map[i + 1, j]) - Convert.ToSingle(map[i, j]));
                            Vector3 N = Vector3.Normalize(Vector3.Cross(v1, v2));

                            Vector3 PL = new Vector3(
                                this.conf.lightPositionX + Convert.ToSingle(changeLightPositionX),
                                this.conf.lightPositionY + Convert.ToSingle(changeLightPositionY),
                                this.conf.lightPositionZ + Convert.ToSingle(changeLightPositionZ));
                            Vector3 PP = new Vector3(i, j, Convert.ToSingle(map[i, j])); //точка изображения
                            Vector3 PV = new Vector3(this.conf.observerPositionX, this.conf.observerPositionY, this.conf.observerPositionZ);// Позиция наблюдателя

                            Vector3 L = Vector3.Normalize(PL - PP); // Вектор от точки к Источнику      
                            Vector3 V = Vector3.Normalize(PV - PP);// Вектор к наблюдателю
                            Vector3 H = Vector3.Normalize(L + V); // Вектор полупути ( для модели Фонга-Блинна)

                            double IL = id * kd * Vector3.Dot(L, N);
                            double IFB = Math.Pow(Math.Max(Vector3.Dot(N, H), 0.0), alpha);

                            // Суммирование составляющих цвета
                            // ( Diffuse (Ламберт) + Specular (Фонг-Блинн))
                            I = IFB + IL;
                        }

                        // Модель Кука-Торренса
                        if (radio3.IsChecked == true)
                        {
                            Vector3 v1 = new Vector3(0, 1, Convert.ToSingle(map[i, j + 1]) - Convert.ToSingle(map[i, j]));
                            Vector3 v2 = new Vector3(1, 0, Convert.ToSingle(map[i + 1, j]) - Convert.ToSingle(map[i, j]));
                            Vector3 N = Vector3.Normalize(Vector3.Cross(v1, v2));

                            Vector3 PL = new Vector3(this.conf.lightPositionX, this.conf.lightPositionY, this.conf.lightPositionZ);
                            Vector3 PP = new Vector3(i, j, Convert.ToSingle(map[i, j]));
                            //точка изображения 
                            Vector3 PV = new Vector3(this.conf.observerPositionX, this.conf.observerPositionY, this.conf.observerPositionZ);// Позиция наблюдателя 

                            Vector3 L = Vector3.Normalize(PL - PP); // Вектор от точки к Источнику
                            Vector3 V = Vector3.Normalize(PV - PP); // Вектор к наблюдателю
                            Vector3 H = Vector3.Normalize(L + V);   // Вектор полупути ( для модели Фонга - Блинна) 

                            // Вычисление цвета точки по модели Кука-Торренса 
                            double NdotV = Vector3.Dot(N, V);
                            double NdotH = Vector3.Dot(N, H);
                            double NdotL = Vector3.Dot(N, L);
                            double VdotH = Vector3.Dot(V, H);

                            //double F = Math.Pow(1.0 - VdotH, 5.0) * (1.0 - F0) + F0; 

                            double r_sq = r * r;
                            double NdotH_sq = NdotH * NdotH;
                            double NdotH_sq_r = 1.0 / (NdotH_sq * r_sq);
                            double roughness_exp = (NdotH_sq - 1.0) * (NdotH_sq_r);
                            double roughness = Math.Exp(roughness_exp) * NdotH_sq_r / (4.0 * NdotH_sq);

                            double r1 = 1.0 / (4.0 * r_sq * Math.Pow(NdotH, 4.0));
                            double r2 = (NdotH * NdotH - 1.0) / (r_sq * NdotH * NdotH);
                            double D = r1 * Math.Exp(r2);

                            double two_NdotH = 2.0 * NdotH;
                            double g1 = (two_NdotH * NdotV) / VdotH;
                            double g2 = (two_NdotH * NdotL) / VdotH;
                            double G = Math.Min(1.0, Math.Min(g1, g2));

                            double F = 1.0 / (1.0 + NdotV);
                            double Rs = Math.Min(1.0, F * D * G / (NdotL * NdotV + 1.0e7));

                            I = NdotL * (kd * Vector3.Dot(L, N) + Math.Pow(Math.Max(Vector3.Dot(N, H), 0.0f), alpha) * Rs);
                        }

                        //Перевод Цвета из Системы [0,1] в [0,255]
                        if (I < 1 && I > 0) { pixelColor = Convert.ToByte(255 * I); }
                        else if (I < 0) { pixelColor = 0; }
                        else if (I > 1) { pixelColor = 255; }

                        pixelArray[i, j, 0] = pixelColor;
                        pixelArray[i, j, 1] = pixelColor;
                        pixelArray[i, j, 2] = pixelColor;
                    }
                    else
                    {
                        pixelArray[i, j, 0] = 255;
                        pixelArray[i, j, 1] = 255;
                        pixelArray[i, j, 2] = 255;
                    }

                }
            }
            imageBox.Source = BitmapFromArray(pixelArray);
        }

        public BitmapImage BitmapFromArray(byte[,,] pixelArray)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                int width = pixelArray.GetLength(1);
                int height = pixelArray.GetLength(0);
                int stride = (width % 4 == 0) ? width : width + 4 - width % 4;
                int bytesPerPixel = 3;

                byte[] bytes = new byte[stride * height * bytesPerPixel];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = (y * stride + x) * bytesPerPixel;
                        bytes[offset + 0] = pixelArray[y, x, 2]; // blue
                        bytes[offset + 1] = pixelArray[y, x, 1]; // green
                        bytes[offset + 2] = pixelArray[y, x, 0]; // red
                    }
                }

                var image = Image.LoadPixelData<Rgb24>(bytes, width, height);
                image.Mutate(x => x.Grayscale());

                image.SaveAsBmp(memory);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            switch (conf.outputModelFormat)
            {
                case "ply":
                    this.mapWorker.exportPLY(this.conf.outputModelName + "." + this.conf.outputModelFormat);
                    MessageBox.Show("Модель сохранена", "Информация", MessageBoxButton.OK);
                    break;
                case "amf":
                    this.mapWorker.exportAMF(this.conf.outputModelName + "." + this.conf.outputModelFormat);
                    MessageBox.Show("Модель сохранена", "Информация", MessageBoxButton.OK);
                    break;
                case "stl":
                    this.mapWorker.exportSTL(this.conf.outputModelName + "." + this.conf.outputModelFormat);
                    MessageBox.Show("Модель сохранена", "Информация", MessageBoxButton.OK);
                    break;
            }
        }
    }
}


