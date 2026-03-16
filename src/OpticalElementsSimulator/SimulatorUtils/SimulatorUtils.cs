using AligningOpticalElements;
using OpenCvSharp;
using static OpenCvSharp.ML.DTrees;

namespace OpticalElementsSimulator.SimulatorUtils
{
    internal class SimulatorUtils
    {
        private Spot refSpot;
        private Spot sampleSpot;
        private Spot refTrimSpot;
        private Spot sampleTrimSpot;
        private Mat imageRef = new Mat();
        private Mat imageSample = new Mat();
        private Mat imageRefTrim = new Mat();
        private Mat imageSampleTrim = new Mat();
        private List<Spot> sampleSpots = new List<Spot>();
        private Random rnd = new Random();
        private int noise;

        public Spot GetSpotRef { get { return refSpot; } }
        public Spot GetSpotSample { get { return sampleSpot; } }
        public Spot GetSpotRefTrim { get { return refTrimSpot; } }
        public Spot GetSpotSampleTrim { get { return sampleTrimSpot; } }
        public Mat GetImageRef { get { return imageRef; } }
        public Mat GetImageSample { get { return imageSample; } }
        public Mat GetImageRefTrim { get { return imageRefTrim; } }
        public Mat GetImageSampleTrim { get { return imageSampleTrim; } }
        public List<Spot> GetSampleSpots { get { return sampleSpots; } }
        public int GetNoise
        {
            get => noise;
            set => noise = value;
        }



        public void ReferenceImageRand(int hExternal, int wExternal, int hInternal, int wInternal, int noise, int r)
        {
            var img = new Mat(hExternal, wExternal, MatType.CV_8UC1, Scalar.All(0));

           

            int borderW = (wExternal - wInternal) / 2;
            int borderH = (hExternal - hInternal) / 2;

            int refPointX = rnd.Next(borderW, wInternal + borderW);
            int refPointY = rnd.Next(borderH, hInternal + borderH);
            int parPointX = rnd.Next(borderW, wInternal + borderW);
            int parPointY = rnd.Next(borderH, hInternal + borderH);

            int xr = wExternal / 2 - refPointX;
            int yr = hExternal / 2 - refPointY;
            int xp = wExternal / 2 - parPointX;
            int yp = hExternal / 2 - parPointY;

            int radius = rnd.Next(10, r); //polomer

            float refPointZ = 0;

            if (xr * xr + yr * yr > xp * xp + yp * yp)
            {
                (refPointX, parPointX) = (parPointX, refPointX);
                (refPointY, parPointY) = (parPointY, refPointY);
            }

            var roi = new Rect(borderW, borderH, wInternal, hInternal);
            Spot newRefTrimSpot = new Spot(refPointX - borderW, refPointY - borderH, radius, 0);
            Spot newRefSpot = new Spot(refPointX, refPointY, radius, refPointZ);
            Spot newParSpot = new Spot(parPointX, parPointY, radius, refPointZ);

            ImageDesign(newRefSpot, img);
            ImageDesign(newParSpot, img);

            Mat imgTrim = new Mat(img, roi).Clone();

            this.refSpot = newRefSpot;
            this.refTrimSpot = newRefTrimSpot;
            this.imageRef = img;
            this.imageRefTrim = imgTrim;
        }


        public void SampleImageRand(int hExternal, int wExternal, int hInternal, int wInternal, int r)
        {
            Mat imgSamp = this.imageRef.Clone();

            int sampleX = rnd.Next(0, imgSamp.Width);
            int sampleY = rnd.Next(0, imgSamp.Height);
            int radius = rnd.Next(10, r); //polomer
            radius = 300;
            float sampleZ = rnd.Next(0, 200) - 100;
            if (sampleZ >= 0) sampleZ += 1;

            int borderW = (wExternal - wInternal) / 2;
            int borderH = (hExternal - hInternal) / 2;

            
           
            Spot newSampleTrimSpot = new Spot(sampleX - borderW, sampleY - borderH, radius, 0);
            Spot newSampleSpot = new Spot(sampleX, sampleY, radius, sampleZ);


            ImageDesign(newSampleSpot, imgSamp);
            var roi = new Rect(borderW, borderH, wInternal, hInternal);
            Mat imgTrim = new Mat(imgSamp, roi).Clone();

            this.sampleSpot = newSampleSpot;
            this.sampleTrimSpot = newSampleTrimSpot;
            this.imageSample = imgSamp;
            this.imageSampleTrim = imgTrim;
        }


        protected void NewSampleImage()
        {
            Mat newImageSample = this.imageRef.Clone();

            int wExternal = newImageSample.Width;
            int hExternal = newImageSample.Height;
            int wInternal = imageSampleTrim.Width;
            int hInternal = imageSampleTrim.Height;

            int sampleX = this.sampleSpot.GetCoordX;
            int sampleY = this.sampleSpot.GetCoordY;
            int radius = this.sampleSpot.GetRadius;
            float sampleZ = this.sampleSpot.GetCoordZ;
           
            int borderW = (wExternal - wInternal) / 2;
            int borderH = (hExternal - hInternal) / 2;

            var roi = new Rect(borderW, borderH, wInternal, hInternal);
            
            Spot newSampleTrimSpot = new Spot(sampleX - borderW, sampleY - borderH, radius, sampleZ);

            this.sampleSpot = new Spot(sampleX, sampleY, radius, sampleZ);
            ImageDesign(GetSpotSample, newImageSample);
            Mat imgTrim = new Mat(newImageSample, roi).Clone();


            this.sampleTrimSpot = newSampleTrimSpot;
            this.imageSample = newImageSample;
            this.imageSampleTrim = imgTrim;
        }


        public void SimSampleMoveXY(int shift, int state)
        {
            List<Spot> spots = new List<Spot>();
            (int dx, int dy)[] N8 =
            {
                (-shift,-shift), (shift,0), (shift,0),
                (0, shift),          (0, shift),
                (-shift, 0), (-shift, 0), (0, -shift)
            };
            this.sampleSpot = new Spot(sampleSpot.GetCoordX + N8[state].dx, sampleSpot.GetCoordY + N8[state].dy, sampleSpot.GetRadius, sampleSpot.GetCoordZ);
            NewSampleImage();
        }


        public void SimSampleMoveZ(int ZS)
        {
            int RV = sampleSpot.GetRadius;
            int RC = refSpot.GetRadius;
            float Z = ZS + sampleSpot.GetCoordZ;
            int RVS = (int)((ZS * RC - Z * RV) / (ZS - Z)); 
            this.sampleSpot = new Spot(sampleSpot.GetCoordX, sampleSpot.GetCoordY, RVS, sampleSpot.GetCoordZ + ZS);
            NewSampleImage();
        }


        protected void ImageDesign(Spot spot, Mat img)
        {
            int radius = spot.GetRadius;
            int pointX = spot.GetCoordX;
            int pointY = spot.GetCoordY;
        
            int r2 = radius * radius;

            for (int x = pointX - radius; x <= pointX + radius; x++)
            {
                if (x < 0 || x >= img.Width) continue;

                for (int y = pointY - radius; y <= pointY + radius; y++)
                {
                    if (y < 0 || y >= img.Height) continue;

                    int dx = x - pointX;
                    int dy = y - pointY;
                    int d2 = dx * dx + dy * dy;

                    if (d2 <= r2)
                    {
                        double value = 255.0 * Math.Exp(-1.0 * d2 / (radius * radius)) / (rnd.Next(1, 3))*Math.Sqrt(radius/10)/(radius/10);
                        img.Set(y, x, (byte)value);
                    }
                }
            }
     
            //noise 
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    if (img.At<byte>(i, j) < GetNoise)
                    {
                        img.Set(i, j, (byte)rnd.Next(1, GetNoise));
                    }
                }
            }
        } 
    }
}