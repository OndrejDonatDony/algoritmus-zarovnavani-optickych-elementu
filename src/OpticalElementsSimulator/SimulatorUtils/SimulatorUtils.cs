using AligningOpticalElements;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public Spot GetSpotRef { get { return refSpot; } }
        public Spot GetSpotSample { get { return sampleSpot; } }
        public Spot GetSpotRefTrim { get { return refTrimSpot; } }
        public Spot GetSpotSampleTrim { get { return sampleTrimSpot; } }
        public Mat GetImageRef { get { return imageRef; } }
        public Mat GetImageSample { get { return imageSample; } }
        public Mat GetImageRefTrim { get { return imageRefTrim; } }
        public Mat GetImageSampleTrim { get { return imageSampleTrim; } }
        public List<Spot> GetSampleSpots { get { return sampleSpots; } }

        public void ReferenceImageRand(int hExternal, int wExternal, int hInternal, int wInternal, int noise, int r)
        {
            var img = new Mat(hExternal, wExternal, MatType.CV_8UC1, Scalar.All(0));

            for (int i = 0; i < hExternal; i++)
            {
                for (int j = 0; j < wExternal; j++)
                {
                    img.Set(i, j, rnd.Next(1, 1 + noise));
                }
            }

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

            int radius = rnd.Next(10, r);
            float refPointZ = 0;

            if (xr * xr + yr * yr > xp * xp + yp * yp)
            {
                (refPointX, parPointX) = (parPointX, refPointX);
                (refPointY, parPointY) = (parPointY, refPointY);
            }

            int r2 = radius * radius;

            for (int x = refPointX - radius; x <= refPointX + radius; x++)
            {
                if (x < 0 || x >= wExternal) continue;

                for (int y = refPointY - radius; y <= refPointY + radius; y++)
                {
                    if (y < 0 || y >= hExternal) continue;

                    int dx = x - refPointX;
                    int dy = y - refPointY;
                    int d2 = dx * dx + dy * dy;

                    if (d2 <= r2)
                    {
                        double t = 1.0 - Math.Sqrt(d2) / radius;
                        t = t * t;
                        t += rnd.Next(20, 255);
                        img.Set(y, x, t);
                    }
                }
            }

            for (int x = parPointX - radius; x <= parPointX + radius; x++)
            {
                if (x < 0 || x >= wExternal) continue;

                for (int y = parPointY - radius; y <= parPointY + radius; y++)
                {
                    if (y < 0 || y >= hExternal) continue;

                    int dx = x - parPointX;
                    int dy = y - parPointY;
                    int d2 = dx * dx + dy * dy;

                    if (d2 <= r2)
                    {
                        double t = 1.0 - Math.Sqrt(d2) / radius;
                        t = t * t;
                        t += rnd.Next(20, 255);
                        img.Set(y, x, t);
                    }
                }
            }

            var roi = new Rect(borderW, borderH, wInternal, hInternal);
            Mat imgTrim = new Mat(img, roi).Clone();

            Spot newRefTrimSpot = new Spot(refPointX - borderW, refPointY - borderH, radius, 0);
            Spot newRefSpot = new Spot(refPointX, refPointY, radius, refPointZ);

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
            int radius = rnd.Next(10, 200 + r);
            float sampleZ = (float)(radius * 1.5);

            int r2 = radius * radius;
            for (int x = sampleX - radius; x <= sampleX + radius; x++)
            {
                if (x < 0 || x >= imgSamp.Width) continue;

                for (int y = sampleY - radius; y <= sampleY + radius; y++)
                {
                    if (y < 0 || y >= imgSamp.Height) continue;

                    int dx = x - sampleX;
                    int dy = y - sampleY;
                    int d2 = dx * dx + dy * dy;

                    if (d2 <= r2)
                    {
                        double t = 1.0 - Math.Sqrt(d2) / radius;
                        t = t * t;
                        t += rnd.Next(20, 255);
                        imgSamp.Set(y, x, t);
                    }
                }
            }

            int borderW = (wExternal - wInternal) / 2;
            int borderH = (hExternal - hInternal) / 2;

            var roi = new Rect(borderW, borderH, wInternal, hInternal);
            Mat imgTrim = new Mat(imgSamp, roi).Clone();
            Spot newSampleTrimSpot = new Spot(sampleX - borderW, sampleY - borderH, radius, 0);

            for (int i = borderH; i < hInternal + borderH; i++)
            {
                this.imageRef.Set(i, borderW, 254);
                this.imageRef.Set(i, borderW + wInternal, 254);
                imgSamp.Set(i, borderW, 254);
                imgSamp.Set(i, borderW + wInternal, 254);
            }

            for (int i = borderW; i < wInternal + borderW; i++)
            {
                this.imageRef.Set(borderH, i, 254);
                this.imageRef.Set(borderH + hInternal, i, 254);
                imgSamp.Set(borderH, i, 254);
                imgSamp.Set(borderH + hInternal, i, 254);
            }

            Spot newSampleSpot = new Spot(sampleX, sampleY, radius, sampleZ);

            this.sampleSpot = newSampleSpot;
            this.sampleTrimSpot = newSampleTrimSpot;
            this.imageSample = imgSamp;
            this.imageSampleTrim = imgTrim;
        }

        protected void NewSampleImage()
        {
            Mat NewImgSamp = this.imageSample.Clone();

            int wExternal = NewImgSamp.Width;
            int hExternal = NewImgSamp.Height;
            int wInternal = this.imageSampleTrim.Width;
            int hInternal = this.imageSampleTrim.Height;

            int sampleX = this.sampleSpot.GetCoordX;
            int sampleY = this.sampleSpot.GetCoordY;
            int radius = this.sampleSpot.GetRadius;
            float sampleZ = this.sampleSpot.GetCoordZ;

            int r2 = radius * radius;
            for (int x = sampleX - radius; x <= sampleX + radius; x++)
            {
                if (x < 0 || x >= NewImgSamp.Width) continue;

                for (int y = sampleY - radius; y <= sampleY + radius; y++)
                {
                    if (y < 0 || y >= NewImgSamp.Height) continue;

                    int dx = x - sampleX;
                    int dy = y - sampleY;
                    int d2 = dx * dx + dy * dy;

                    if (d2 <= r2)
                    {
                        double t = 1.0 - Math.Sqrt(d2) / radius;
                        t = t * t * 255;
                        NewImgSamp.Set(y, x, (byte)t);
                    }
                }
            }

            int borderW = (wExternal - wInternal) / 2;
            int borderH = (hExternal - hInternal) / 2;

            var roi = new Rect(borderW, borderH, wInternal, hInternal);
            Mat imgTrim = new Mat(NewImgSamp, roi).Clone();
            Spot newSampleTrimSpot = new Spot(sampleX - borderW, sampleY - borderH, radius, 0);

            for (int i = borderH; i < hInternal + borderH; i++)
            {
                this.imageRef.Set(i, borderW, 254);
                this.imageRef.Set(i, borderW + wInternal, 254);
                NewImgSamp.Set(i, borderW, 254);
                NewImgSamp.Set(i, borderW + wInternal, 254);
            }

            for (int i = borderW; i < wInternal + borderW; i++)
            {
                this.imageRef.Set(borderH, i, 254);
                this.imageRef.Set(borderH + hInternal, i, 254);
                NewImgSamp.Set(borderH, i, 254);
                NewImgSamp.Set(borderH + hInternal, i, 254);
            }

            Spot newSampleSpot = new Spot(sampleX, sampleY, radius, sampleZ);

            this.sampleSpot = newSampleSpot;
            this.sampleTrimSpot = newSampleTrimSpot;
            this.imageSample = NewImgSamp;
            this.imageSampleTrim = imgTrim;
        }
        public void SimSampleMoveXY(int shift, int state)
        {
            List<Spot> spots = new List<Spot>();
            (int dx, int dy)[] N8 =
            {
                (-shift,-shift), (0,-shift), (shift,-shift),
                (-shift, 0),          (shift, 0),
                (-shift, shift), (0, shift), (shift, shift)
            };
            sampleSpot.GetCoordX = this.sampleSpot.GetCoordX + N8[state].dx;
            sampleSpot.GetCoordY = this.sampleSpot.GetCoordY + N8[state].dy;
            NewSampleImage();
        }

        public (int, int, int) SimSampleMoveZ(int sampleX, int sampleY, int radius, int shift)
        {
            int sampleXNew = sampleX + shift;
            int sampleYNew = sampleY + shift;
            int radiusNew = 0;

            return (sampleXNew, sampleYNew, radiusNew);
        }
    }
}