using System.Collections.Generic;

namespace _8F.Models
{
    public class GraphData
    {
        public int Id = 0;
        public string Name = "D";
        public int sol = 400;
        public int freq = 2000;
        public int gain = 35;
        public int phase = 0;
        public int txStrength = 100;
        public int strength { get => txStrength; set => txStrength = value; }
        public int postGain = 60;
        public bool isEnable = true;
        public double height = DeviceCOM.DefaultHeight;
        public double width = DeviceCOM.DefaultWidth;
        public double ex = 30;
        public double ey = 30;
        public double angel = 30;
        public List<Ellips> ellipses = new List<Ellips>();

        public double height_O = DeviceCOM.DefaultHeight_O;
        public double width_O = DeviceCOM.DefaultWidth_O;
        public double ex_O = 0;
        public double ey_O = 0;
        public double angel_O = DeviceCOM.DefaultAngel_O;
        public double NG = DeviceCOM.DefaultAngel_O;
    }
}
