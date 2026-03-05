using AligningOpticalElements;
using OpenCvSharp;
using OpticalElementsSimulator.Models;
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
        public (Spot, Mat,Spot, Mat) ReferenceImage(int hExternal,int wExternal,int hInternal,int wInternal,int noise,int r)
        {
            var img = new Mat(hExternal, wExternal, MatType.CV_8UC1, Scalar.All(0));
            Random rnd = new Random();

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

            int radius = rnd.Next(20, r);
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

            //kamera
            var roi = new Rect(borderW, borderH, wInternal, hInternal);
            Mat imgTrim = new Mat(img, roi).Clone();
            Spot refSpotTrim = new Spot(refPointX - borderW, refPointY - borderH, radius, 0);

            for (int i = borderH; i < hInternal + borderH; i++)
            {
                img.Set(i, borderW, 254);
                img.Set(i, borderW + wInternal, 254);

            }
            for (int i = borderW; i < wInternal + borderW; i++)
            {
                img.Set(borderH, i, 254);
                img.Set(borderH + hInternal, i, 254);

            }
            Spot refSpot = new Spot(refPointX, refPointY, radius, refPointZ);

            
            return (refSpot, img, refSpotTrim, imgTrim);
        }

        public (Spot,Mat,Spot,Mat) SampleImage(int hExternal, int wExternal, int hInternal, int wInternal, Mat img, int r)
        {
            Mat imgSamp = img.Clone();

            Random rnd = new Random();
            int sampleX = rnd.Next(0,imgSamp.Width);
            int sampleY = rnd.Next(0, imgSamp.Height);
            int radius = rnd.Next(10, 200+r);
            float sampleZ = (float)(radius * 1.5); //dodelat

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
            Spot refSpotTrim = new Spot(sampleX - borderW, sampleY - borderH, radius, 0);

            Spot sampleSpot = new Spot(sampleX, sampleY, radius, sampleZ); 
            return (sampleSpot, imgSamp, refSpotTrim, imgTrim);
        }

        public (int,int,int) SampleMoveXY(int sampleX, int sampleY, int radius, int shift)
        {
            int sampleXNew = sampleX + shift;
            int sampleYNew = sampleY + shift;
            int radiusNew = 0;

            return (sampleXNew, sampleYNew, radiusNew);
        }
        public (int, int, int) SampleMoveZ(int sampleX, int sampleY, int radius, int shift)
        {
            int sampleXNew = sampleX + shift;
            int sampleYNew = sampleY + shift;
            int radiusNew = 0;

            return (sampleXNew, sampleYNew, radiusNew);
        }

    }
}
