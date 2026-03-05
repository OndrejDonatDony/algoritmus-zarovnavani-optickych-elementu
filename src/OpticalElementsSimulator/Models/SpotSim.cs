using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpticalElementsSimulator.Models
{
    public class SpotSim
    {
        private int coordX;
        private int coordY;
        private float coordZ;
        private int radius;


        // konstruktor
        public SpotSim(int coordX, int coordY, int radius, float coordZ)
        {
            this.coordX = coordX;
            this.coordY = coordY;
            this.radius = radius;
            this.coordZ = coordZ;
        }

        // getter a setter
        public int GetCoordX
        {
            get
            {
                return coordX;
            }
        }
        public int GetCoordY
        {
            get
            {
                return coordY;
            }
        }
        public float GetCoordZ
        {
            get
            {
                return coordZ;
            }
        }
        public int GetRadius
        {
            get
            {
                return radius;
            }
        }
        public void SetCoordZ(float coordZ)
        {       
            this.coordZ = coordZ;           
        }
    }
}
