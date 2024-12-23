using System;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SharpModels
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Загружаем карту глубины через MapWorker класс
            // И сразу экспортируем в нужный формат
            string filePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\Maps\DepthMap_7.dat"));

            MapWorker depthMap = new MapWorker(filePath);
            depthMap.loadData();

            depthMap.exportAMF("models/model.amf");
            depthMap.exportPLY("models/model.ply");
            depthMap.exportWRL("models/model.wrl");
            depthMap.exportSTL("models/model.stl");

            // Работа с OpenGL \ OpenTK
            var nativeWinSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(1280, 720),
                Location = new Vector2i(370, 300),
                WindowBorder = WindowBorder.Resizable,
                WindowState = WindowState.Normal,
                Title = "Depth Map Visualization",
                Flags = ContextFlags.Default,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Compatability,
                API = ContextAPI.OpenGL,
                NumberOfSamples = 0,
                Icon = new WindowIcon(new OpenTK.Windowing.Common.Input.Image(512, 512, ImageToByteArray("icon.png")))
            };

            static byte[] ImageToByteArray(string Icon)
            {
                var image = (Image<Rgba32>)SixLabors.ImageSharp.Image.Load(Configuration.Default, Icon);

                image.Mutate(x => x.Flip(FlipMode.Vertical));

                var pixels = new byte[4 * image.Width * image.Height];
                image.CopyPixelDataTo(pixels);

                return pixels;
            }

            //using (ObjectViewer game = new ObjectViewer(GameWindowSettings.Default, nativeWinSettings, depthMap))
            //{
            //    // Управление стандартное - WASD + Q-E
            //    game.Run();
            //}
            using (LightModelViewer game = new LightModelViewer(GameWindowSettings.Default, nativeWinSettings, depthMap))
            {
                game.Run();
            }
        }
    }
}

