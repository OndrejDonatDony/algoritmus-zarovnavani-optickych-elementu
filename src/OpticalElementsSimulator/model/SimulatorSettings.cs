using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpticalElementsSimulator.model
{
    public class SimulationSettings
    {
        public int Noise { get; set; }
        public int RefRadius { get; set; }
        public int SampleRadius { get; set; }
        public int HeightExternal { get; set; }
        public int WidthExternal { get; set; }
        public int HeightInternal { get; set; }
        public int WidthInternal { get; set; }
        public int ShiftXY { get; set; }
        public float ShiftZ { get; set; }
        public int Seed { get; set; }
    }
}
