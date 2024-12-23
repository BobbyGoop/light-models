using System;
using System.Collections.Generic;
using System.Text;

namespace WPFLighting
{
    class Config
    {
        public  float lightPositionX = 0;
        public float lightPositionY = 0;
        public float lightPositionZ = 0;

        public float observerPositionX = 0;
        public float observerPositionY = 0;
        public float observerPositionZ = 0;

        public string depthMapPath;

        public string outputModelName;
        public string outputModelFormat;

        public string outputImageName;
        public string outputImageFormat;

        // 0 - Ламберт
        // 1 - Фонг-Блинн
        // 2 - Орен-Найар
        public int lightingMode; 

    }
}
