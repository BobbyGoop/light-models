using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace WPFLighting
{
    public class MapWorker
    {
        public string filePath;
        public double[,] depthMap;
        public MapWorker(string path)
        {
            // Результат экспорта находится по пути
            // .\SharpModels\RenderGL\bin\Debug\netcoreapp3.1\models\
            this.filePath = path;
        }

        public void loadData()
        {
            BinaryReader reader = new BinaryReader(File.Open(this.filePath, FileMode.Open));
            double Height = reader.ReadDouble();
            double Width = reader.ReadDouble();

            this.depthMap = new double[Convert.ToInt16(Height), Convert.ToInt16(Width)];

            for (int i = 0; i < depthMap.GetLength(0); i++)
            {
                for (int j = 0; j < depthMap.GetLength(1); j++)
                {
                    depthMap[i, j] = reader.ReadDouble();
                }
            }
            reader.BaseStream.Close();
        }

        public void exportSTL(string outFileName)
        {
            using (StreamWriter w = new StreamWriter(outFileName, false, Encoding.UTF8))
            {
                w.WriteLine("solid Lab");
                for (int i = 0; i < depthMap.GetLength(0) - 1; i++)
                {
                    for (int j = 0; j < depthMap.GetLength(1) - 1; j++)
                    {
                        // В каждой точке проходим по квадрату и заносим точки двух треугольников
                        if (depthMap[i, j] != 0 && depthMap[i + 1, j] != 0 && depthMap[i + 1, j + 1] != 0 && depthMap[i, j + 1] != 0)
                        {
                            // Для STL формата необходимо указывать вектор
                            // нормали к поверхности, но его расчет на данный
                            // момент не представляется возможным
                            // Практически используемая формула:
                            //
                            //   Nx = Y1 * (Z2 - Z3) + Y2 * (Z3 - Z1) + Y3 * (Z1 - Z2);
                            //   Ny = Z1 * (X2 - X3) + Z2 * (X3 - X1) + Z3 * (X1 - X2);
                            //   Nz = X1 * (Y2 - Y3) + X2 * (Y3 - Y1) + X3 * (Y1 - Y2);

                            // Проверить STL можно тут (Blender и Paint не подгружают из-за отсутствия нормалей):
                            // https://imagetostl.com/ru/view-stl-online#convert

                            w.WriteLine("facet normal 0 0 0");
                            w.WriteLine("outer loop");
                            w.WriteLine("vertex " + i + " " + j + " " + depthMap[i, j].ToString("E").Replace(',', '.'));
                            w.WriteLine("vertex " + (i + 1) + " " + j + " " + depthMap[i + 1, j].ToString("E").Replace(',', '.'));
                            w.WriteLine("vertex " + i + " " + (j + 1) + " " + depthMap[i, j + 1].ToString("E").Replace(',', '.'));
                            w.WriteLine("endloop");
                            w.WriteLine("endfacet");

                            w.WriteLine("facet normal 0 0 0");
                            w.WriteLine("outer loop");
                            w.WriteLine("vertex " + (i + 1) + " " + j + " " + depthMap[i + 1, j].ToString("E").Replace(',', '.'));
                            w.WriteLine("vertex " + (i + 1) + " " + (j + 1) + " " + depthMap[i + 1, j + 1].ToString("E").Replace(',', '.'));
                            w.WriteLine("vertex " + i + " " + (j + 1) + " " + depthMap[i, j + 1].ToString("E").Replace(',', '.'));
                            w.WriteLine("endloop");
                            w.WriteLine("endfacet");
                        }
                    }
                }
                w.WriteLine("endsolid Lab");
            }
        }
        public void exportAMF(string outFileName)
        {
            using (StreamWriter w = new StreamWriter(outFileName, false, Encoding.UTF8))
            {
                w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
                w.WriteLine("<amf unit='mm'>");
                w.WriteLine("<object id='1'>");
                w.WriteLine("<mesh>");
                w.WriteLine("<vertices>");

                for (int i = 0; i < depthMap.GetLength(0) - 1; i++)
                {
                    for (int j = 0; j < depthMap.GetLength(1) - 1; j++)
                    {
                        if (depthMap[i, j] != 0 && depthMap[i + 1, j] != 0 && depthMap[i + 1, j + 1] != 0 && depthMap[i, j + 1] != 0)
                        {
                            // В каждой точке проходим по квадрату и заносим точки двух треугольников
                            w.WriteLine("<vertex><coordinates><x>" + i + "</x><y>" + j + "</y><z>" + depthMap[i, j].ToString().Replace(',', '.') + "</z></coordinates></vertex>");
                            w.WriteLine("<vertex><coordinates><x>" + (i + 1) + "</x><y>" + j + "</y><z>" + depthMap[i + 1, j].ToString().Replace(',', '.') + "</z></coordinates></vertex>");
                            w.WriteLine("<vertex><coordinates><x>" + i + "</x><y>" + (j + 1) + "</y><z>" + depthMap[i, j + 1].ToString().Replace(',', '.') + "</z></coordinates></vertex>");

                            w.WriteLine("<vertex><coordinates><x>" + (i + 1) + "</x><y>" + j + "</y><z>" + depthMap[i + 1, j].ToString().Replace(',', '.') + "</z></coordinates></vertex>");
                            w.WriteLine("<vertex><coordinates><x>" + (i + 1) + "</x><y>" + (j + 1) + "</y><z>" + depthMap[i + 1, j + 1].ToString().Replace(',', '.') + "</z></coordinates></vertex>");
                            w.WriteLine("<vertex><coordinates><x>" + i + "</x><y>" + (j + 1) + "</y><z>" + depthMap[i, j + 1].ToString().Replace(',', '.') + "</z></coordinates></vertex>");
                        }
                    }
                }
                w.WriteLine("</vertices>");
                w.WriteLine("<volume>");
                int k = 0;
                for (int i = 0; i < depthMap.GetLength(0) - 1; i++)
                {
                    for (int j = 0; j < depthMap.GetLength(1) - 1; j++)
                    {
                        if (depthMap[i, j] != 0 && depthMap[i + 1, j] != 0 && depthMap[i + 1, j + 1] != 0 && depthMap[i, j + 1] != 0)
                        {
                            //строим 2 треугольника
                            w.WriteLine("<triangle><v1>" + k + "</v1><v2>" + (k + 1) + "</v2><v3>" + (k + 2) + "</v3></triangle>");
                            w.WriteLine("<triangle><v1>" + (k + 3) + "</v1><v2>" + (k + 4) + "</v2><v3>" + (k + 5) + "</v3></triangle>");
                            k = k + 6;
                        }
                    }
                }
                w.WriteLine("</volume>");
                w.WriteLine("</mesh>");
                w.WriteLine("</object>");
                w.WriteLine("</amf>");
            }
            Console.WriteLine("Export AMF: Done");
        }

        public void exportPLY(string outFileName)
        {
            int facesCount = 0;
            int vertexIndex = 0;
            int vertexCount = 0;
            using (StreamWriter w = new StreamWriter("tmp.txt", false, Encoding.ASCII))
            {
                // FIRSTLY WRITE DATA
                // SECONDLY INSERT HEADER

                // Write Vertex Data
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.NumberDecimalSeparator = ".";

                for (int i = 0; i < depthMap.GetLength(0) - 1; i++)
                {
                    for (int j = 0; j < depthMap.GetLength(1) - 1; j++)
                    {
                        if (depthMap[i, j] != 0 && depthMap[i + 1, j] != 0 && depthMap[i + 1, j + 1] != 0 && depthMap[i, j + 1] != 0)
                        {
                            // В каждой точке проходим по квадрату и заносим точки двух треугольников
                            w.WriteLine((double)i + " " + (double)j + " " + depthMap[i, j].ToString(nfi));
                            w.WriteLine((double)(i + 1) + " " + (double)j + " " + depthMap[i + 1, j].ToString(nfi));
                            w.WriteLine((double)i + " " + (double)(j + 1) + " " + depthMap[i, j + 1].ToString(nfi));

                            w.WriteLine((double)(i + 1) + " " + (double)j + " " + depthMap[i + 1, j].ToString(nfi));
                            w.WriteLine((double)(i + 1) + " " + (double)(j + 1) + " " + depthMap[i + 1, j + 1].ToString(nfi));
                            w.WriteLine((double)i + " " + (double)(j + 1) + " " + depthMap[i, j + 1].ToString(nfi));
                            vertexCount += 6;
                        }
                    }
                }

                // Write Faces Data
                for (int i = 0; i < depthMap.GetLength(0) - 1; i++)
                {
                    for (int j = 0; j < depthMap.GetLength(1) - 1; j++)
                    {
                        if (depthMap[i, j] != 0 && depthMap[i + 1, j] != 0 && depthMap[i + 1, j + 1] != 0 && depthMap[i, j + 1] != 0)
                        {
                            //строим 2 треугольника
                            w.WriteLine("3" + " " + vertexIndex + " " + (vertexIndex + 1) + " " + (vertexIndex + 2) + " ");
                            w.WriteLine("3" + " " + (vertexIndex + 3) + " " + (vertexIndex + 4) + " " + (vertexIndex + 5) + " ");
                            vertexIndex += 6;
                            facesCount += 2;
                        }
                    }
                }
            }
            string Header = $"ply\nformat ascii 1.0\nelement vertex {vertexCount}\n" +
                    $"property float x\nproperty float y\nproperty float z\n" +
                    $"element face {facesCount}\nproperty list int int vertex_index\nend_header";

            var dataLines = File.ReadAllLines("tmp.txt").ToList();
            dataLines.Insert(0, Header);
            File.WriteAllLines(outFileName, dataLines, Encoding.ASCII);
            File.Delete("tmp.txt");
            Console.WriteLine("Export PLY: Done");
        }

        public void exportWRL(string outFileName)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            using (StreamWriter w = new StreamWriter(outFileName, false, Encoding.UTF8))
            {
                w.WriteLine("#VRML V2.0 utf8");
                w.WriteLine("Shape {");
                w.WriteLine("  geometry IndexedFaceSet {");
                w.WriteLine("    coord Coordinate {");
                w.WriteLine("      point [");

                int vc = 0;
                for (int i = 1; i < depthMap.GetLength(0); i++)
                {
                    for (int j = 0; j < depthMap.GetLength(1) - 1; j++)
                    {

                        if (depthMap[i, j] > 0 && depthMap[i - 1, j] > 0 && depthMap[i - 1, j + 1] > 0 && depthMap[i, j + 1] > 0)
                        {
                            // В каждой точке проходим по квадрату и заносим точки двух треугольников
                            w.WriteLine("        " + i + " " + j + " " + depthMap[i, j].ToString(nfi) + ",");
                            w.WriteLine("        " + (i - 1) + " " + j + " " + depthMap[i - 1, j].ToString(nfi) + ",");
                            w.WriteLine("        " + (i - 1) + " " + (j + 1) + " " + depthMap[i - 1, j + 1].ToString(nfi) + ",");
                            w.WriteLine("        " + i + " " + (j + 1) + " " + depthMap[i, j + 1].ToString(nfi) + ",");
                            vc += 4;
                        }
                    }
                }

                w.WriteLine("      ]");
                w.WriteLine("    }");
                w.WriteLine("    coordIndex [");
                for (int idx = 0; idx < vc; idx += 4)
                {
                    //строим 2 треугольника
                    w.WriteLine("      " + idx + ", " + (idx + 1) + ", " + (idx + 2) + ", -1,");
                    w.WriteLine("      " + idx + ", " + (idx + 3) + ", " + (idx + 2) + ", -1,");
                }
                w.WriteLine("    ]");
                w.WriteLine("  }");
                w.WriteLine("}");
            }
            Console.WriteLine("Export VRML: Done");
        }
    }
}
