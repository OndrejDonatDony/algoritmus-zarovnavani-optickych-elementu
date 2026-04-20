using AligningOpticalElements;
using OpenCvSharp;

namespace OpticalElementsSimulator
{
    public enum ImageKey
    {
        RefImage,
        RefImageTrim,
        SampleImage,
        SampleImageTrim
    }

    public enum SpotKey
    {
        RefSpot,
        ParSpot,
        SampleSpot,
        RefSpotTrim,
        ParSpotTrim,
        SampleSpotTrim
    }
    public class SimulatorUtils
    {
        
        private Dictionary<ImageKey, Mat> images = new();
        private Dictionary<SpotKey, Spot> spots = new();
        private Random rnd = new Random();
        public void SetSeed(int seed)
        {
            rnd = new Random(seed);
        }
        private int noise;

        public IReadOnlyDictionary<ImageKey, Mat> GetImages => images;
        public IReadOnlyDictionary<SpotKey, Spot> GetSpots => spots;

        public void ImageRandGenerator(int hExternal, int wExternal, int hInternal, int wInternal, int noise, int r)
        {
            spots.Clear();
            images.Clear();

            this.noise = noise;

            int wExternalTrim = (wExternal - wInternal) / 2;
            int hExternalTrim = (hExternal - hInternal) / 2;

            int refCoordX = rnd.Next(wExternalTrim, wInternal + wExternalTrim);
            int refCoordY = rnd.Next(hExternalTrim, hInternal + hExternalTrim);
            int parCoordX = rnd.Next(wExternalTrim, wInternal + wExternalTrim);
            int parCoordY = rnd.Next(hExternalTrim, hInternal + hExternalTrim);

            int sampleCoordX = rnd.Next(0, wExternal);
            int sampleCoordY = rnd.Next(0, hExternal);

            int xr = wExternal / 2 - refCoordX;
            int yr = hExternal / 2 - refCoordY;
            int xp = wExternal / 2 - parCoordX;
            int yp = hExternal / 2 - parCoordY;

            int refRadius = rnd.Next(10, r);

            if (xr * xr + yr * yr > xp * xp + yp * yp)
            {
                (refCoordX, parCoordX) = (parCoordX, refCoordX);
                (refCoordY, parCoordY) = (parCoordY, refCoordY);
            }

           
            int sampRadius = 50;
            float sampleZ = rnd.Next(0, 200) - 100;
            if (sampleZ >= 0) sampleZ += 1;

            spots.Add(SpotKey.RefSpot, new Spot(refCoordX, refCoordY, refRadius, 0));
            spots.Add(SpotKey.ParSpot, new Spot(parCoordX, parCoordY, refRadius, 0));
            spots.Add(SpotKey.SampleSpot, new Spot(sampleCoordX, sampleCoordY, sampRadius, sampleZ));

            spots.Add(SpotKey.RefSpotTrim, new Spot(refCoordX - wExternalTrim, refCoordY - hExternalTrim, refRadius, 0));
            spots.Add(SpotKey.ParSpotTrim, new Spot(parCoordX - wExternalTrim, parCoordY - hExternalTrim, refRadius, 0));
            spots.Add(SpotKey.SampleSpotTrim, new Spot(sampleCoordX - wExternalTrim, sampleCoordY - hExternalTrim, sampRadius, sampleZ));

            // Ref image
            images.Add(ImageKey.RefImage, new Mat(hExternal, wExternal, MatType.CV_8UC1, Scalar.All(0)));
            SpotApplicator(images[ImageKey.RefImage], spots[SpotKey.RefSpot]);
            SpotApplicator(images[ImageKey.RefImage], spots[SpotKey.ParSpot]);
            NoiseApplicator(noise, images[ImageKey.RefImage]);

            // Sample image
            images.Add(ImageKey.SampleImage, images[ImageKey.RefImage].Clone());
            SpotApplicator(images[ImageKey.SampleImage], spots[SpotKey.SampleSpot]);
            NoiseApplicator(noise, images[ImageKey.SampleImage]);

            var roi = new Rect(wExternalTrim, hExternalTrim, wInternal, hInternal);

            // Ref trimmed
            images.Add(ImageKey.RefImageTrim, new Mat(images[ImageKey.RefImage], roi).Clone());

            // Sample trimmed
            images.Add(ImageKey.SampleImageTrim, new Mat(images[ImageKey.SampleImage], roi).Clone());
        }

        public void SimSampleMoveXY(int shift, int state, SpotOnBorder border)
        {
            (int dx, int dy)[] N8;
            state = state - 1;
            if (border != SpotOnBorder.None)
            {
                int x = 0;
                int y = 0;
                int i = 4;

                if (border == SpotOnBorder.Left)
                {
                    x = images[ImageKey.SampleImageTrim].Width / i;
                    y = 0;

                }else if(border == SpotOnBorder.Right)
                {
                    x = -images[ImageKey.SampleImageTrim].Width / i;
                    y = 0;
                }
                else if(border == SpotOnBorder.Top) 
                {
                    x = 0;
                    y = images[ImageKey.SampleImageTrim].Height / i;
                }
                else if (border == SpotOnBorder.Bottom) 
                {
                    x = 0;
                    y = -images[ImageKey.SampleImageTrim].Width / i;
                }
                
                spots[SpotKey.SampleSpot] = new Spot(
                    spots[SpotKey.SampleSpot].GetCoordX + x,
                    spots[SpotKey.SampleSpot].GetCoordY + y,
                    spots[SpotKey.SampleSpot].GetRadius,
                    spots[SpotKey.SampleSpot].GetCoordZ);
            }
            else
            {
                N8 = new (int dx, int dy)[]
                {
                    (-shift, shift), (shift, 0), (shift, 0),
                    (0, -shift), (0, -shift),
                    (-shift, 0), (-shift, 0), (0, shift)
                };

                spots[SpotKey.SampleSpot] = new Spot(
                    spots[SpotKey.SampleSpot].GetCoordX + N8[state].dx,
                    spots[SpotKey.SampleSpot].GetCoordY + N8[state].dy,
                    spots[SpotKey.SampleSpot].GetRadius,
                    spots[SpotKey.SampleSpot].GetCoordZ);
            }

            RefreshImage();
            NoiseApplicator(noise, images[ImageKey.SampleImage]);
            return;
        }

        public void SimSampleMoveZ(float ZS)
        {
            Spot sampleSpot = spots[SpotKey.SampleSpot];

            int RV = sampleSpot.GetRadius;
            int RC = spots[SpotKey.RefSpot].GetRadius;
            float Z = ZS + sampleSpot.GetCoordZ;
            int RVS = (int)((ZS * RC - Z * RV) / (ZS - Z));

            spots[SpotKey.SampleSpot] = new Spot(
                spots[SpotKey.SampleSpot].GetCoordX,
                spots[SpotKey.SampleSpot].GetCoordY,
                RVS,
                spots[SpotKey.SampleSpot].GetCoordZ + ZS);

            RefreshImage();
            NoiseApplicator(noise, images[ImageKey.SampleImage]);
            return;
        }

        private void RefreshImage()
        {
            int wExternal = images[ImageKey.RefImage].Width;
            int hExternal = images[ImageKey.RefImage].Height;
            int wInternal = images[ImageKey.RefImageTrim].Width;
            int hInternal = images[ImageKey.RefImageTrim].Height;

            int borderW = (wExternal - wInternal) / 2;
            int borderH = (hExternal - hInternal) / 2;

            var roi = new Rect(borderW, borderH, wInternal, hInternal);

            images[ImageKey.SampleImage] = images[ImageKey.RefImage].Clone();
            SpotApplicator(images[ImageKey.SampleImage], spots[SpotKey.SampleSpot]);
            NoiseApplicator(noise, images[ImageKey.SampleImage]);

            var roiMat = new Mat(images[ImageKey.SampleImage], roi);
            roiMat.CopyTo(images[ImageKey.SampleImageTrim]);
        }

        private void SpotApplicator(Mat img, Spot spot)
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
                        double value = 255.0 * Math.Exp(-1.0 * d2 / (radius * radius))
                            / (rnd.Next(1, 3)) * Math.Sqrt(radius / 10.0) / (radius / 10.0);

                        img.Set(y, x, (byte)value);
                    }
                }
            }
        }

        private void NoiseApplicator(int noise, Mat img)
        {
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    if (img.At<byte>(i, j) < noise)
                    {
                        img.Set(i, j, (byte)rnd.Next(1, noise));
                    }
                }
            }
        }
    }
}