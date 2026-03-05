using OpenCvSharp;
using static OpenCvSharp.FileStorage;
using static OpenCvSharp.ML.DTrees;
namespace AligningOpticalElements;
public class OpticalElementsAligner
{
    public (List<Spot>, int, List<int>) ReferenceSpot(Mat img)
    {

        List<Spot> spots = new List<Spot>();
        List<int> distance = new List<int>();

        (spots, int numOfSpots) = LoadSpots(img);
        if (numOfSpots == 0)
        {
            throw new Exception("nenasly se zadne body");
        }
        else if (numOfSpots == 1)
        {
            return (spots,numOfSpots, distance);
        }
        else if (numOfSpots > 2)
        {
            throw new Exception("prilis mnoho bodu");
        }

        foreach(Spot sp in spots)
        {
            int x = img.Width / 2 - sp.GetCoordX;
            int y = img.Height / 2 - sp.GetCoordY;
            distance.Add(x * x + y * y);
        }
        if (distance[0] < distance[1])
        {
            return (spots,numOfSpots, distance);
        }
        else
        {
            (spots[0],spots[1]) = (spots[1], spots[0]);
            return (spots, numOfSpots, distance);
        }
       
    }
    public (Spot?,bool) SampleSpot(Mat img, List<Spot> spots, int refNumOfSpots, List<int> distance)
    {
        bool sampleFound = false;
        List<Spot> spotsWithSample = new List<Spot>();
        (spotsWithSample, int numOfSpots) = LoadSpots(img);

        if (numOfSpots == refNumOfSpots)
        {
            Console.WriteLine("je potreba posunout vzorek");
            return (null, sampleFound);
        }
        else if (numOfSpots == refNumOfSpots+1)
        {
            List<int> tempDistance = new List<int>();
            foreach (Spot sp in spots)
            {
                int x = img.Width / 2 - sp.GetCoordX;
                int y = img.Height / 2 - sp.GetCoordY;
                tempDistance.Add(x * x + y * y);
            }
            List<int> lowDiffDistAll = new List<int>();
            int k = 0;
            int lowDiffDist = 0;
            for(int i = 0; i < tempDistance.Count; i++)
            {
                for(int j = 0; j < distance.Count; j++)
                {
                    if(Math.Abs(tempDistance[i] - distance[j]) < k)
                    {
                        k = Math.Abs(tempDistance[i] - distance[j]);
                        lowDiffDist = tempDistance[i]; 
                    }

                }
                k = 0;
                lowDiffDistAll.Add(lowDiffDist);
            }
            k = 0;
            int count = 0;
            for(int i = 0;i < lowDiffDistAll.Count; i++)
            {
                if (lowDiffDistAll[i] > k)
                {
                    k = lowDiffDistAll[i];
                    count++;
                }
            }
            sampleFound = true;
            return (spotsWithSample[count],sampleFound);
        }
        else
        {
            Console.WriteLine("neco se pokazilo");
            return (null, sampleFound);
        }
     
    }
    
    protected (List<Spot>, int) LoadSpots(Mat img)
    {
        List<Spot> spots = new List<Spot>();

        if (img == null || img.Empty())
            return (spots, 0);

        int W = img.Width;
        int H = img.Height;

        Mat bin = img;
        if (bin.Type() != MatType.CV_8UC1)
        {
            bin = new Mat();
            Cv2.CvtColor(img, bin, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(bin, bin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        }

        Cv2.FindContours(
            bin,
            out Point[][] contours,
            out HierarchyIndex[] hierarchy,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple
        );

        int numOfSpots = 0;
        for (int i = 0; i < contours.Length; i++)
        {
            double area = Cv2.ContourArea(contours[i]);
            if (area < 5) continue;

            Moments mom = Cv2.Moments(contours[i]);
            if (Math.Abs(mom.M00) < 1e-9) continue;

            // těžiště v obrazových souřadnicích
            // těžiště v pixelech
            int xImg = (int)Math.Round(mom.M10 / mom.M00);
            int yImg = (int)Math.Round(mom.M01 / mom.M00);

            // poloměr v pixelech (z plochy kontury)
            int radiusPx = (int)Math.Round(Math.Sqrt(area / Math.PI));

            // práh v pixelech
            if (radiusPx >= 2)
            {
                numOfSpots++;
                spots.Add(new Spot(xImg, yImg, radiusPx, 0f));
            }

        }

        return (spots, numOfSpots);
    }
    //vzorek se posune +Z
    public Spot? SampleZAxisDistance(Spot sampleNoShiftZ, Spot sampleShiftZ, int centerRadius)
    {
        float zDistance = 0f;
        if (sampleShiftZ.GetRadius <= centerRadius)
        {
            zDistance = 0f;
            sampleShiftZ.SetCoordZ(zDistance);
            return sampleShiftZ;
        }

        float RVS = sampleNoShiftZ.GetRadius; //radius sample shift 
        float RV = sampleNoShiftZ.GetRadius; //radius sample
        float RC = centerRadius; //radius of center spot

        float zValue = 0.01f; //Z shift from interferometer

        if (RV < RVS)
        {
            zDistance = zValue * (RC - RVS) / (RV - RVS);
        }
        else
        {
            zDistance = -zValue * (RC - RVS) / (RV - RVS);
        }
        sampleShiftZ.SetCoordZ(zDistance);
        return sampleShiftZ;
    }
    
    protected static Mat ToGray(Mat img)
    {
        // Pokud uz je grayscale, jen vrat kopii
        if (img.Channels() == 1)
        {
            return img.Clone();
        }

        // Jinak preved BGR -> Gray
        Mat g = new Mat();
        Cv2.CvtColor(img, g, ColorConversionCodes.BGR2GRAY);
        return g;
    }

    // MATLAB logika: nnz(G>1) < 1500 ? (G>1) : najdi TH (1..255) kde nnz(BW)<1500
    // potom bwareaopen(BW,3)
    protected static Mat BinaryImg(Mat img)
    {
        Mat g = ToGray(img);

        Mat tmp = new Mat();
        Cv2.Threshold(g, tmp, 1, 255, ThresholdTypes.Binary);
        int nnzGt1 = Cv2.CountNonZero(tmp);

        Mat bw = new Mat();

        Mat labels = new Mat();

        for (int th = 1; th <= 255; th++)
        {
            Cv2.Threshold(g, bw, th, 255, ThresholdTypes.Binary);
            int count = Cv2.CountNonZero(bw);

            if (count < 800)
            {
                break;
            }
        }

        // bwareaopen(BW,3)
        Mat cleaned = AreaOpen(bw, 3);

        labels.Dispose();
        // Uklid pomocnych matic
        tmp.Dispose();
        bw.Dispose();
        g.Dispose();

        return cleaned;
    }

    // Odstrani komponenty s plochou < minArea
    protected static Mat AreaOpen(Mat bw, int minArea)
    {
        Mat labels = new Mat();
        Mat stats = new Mat();
        Mat centroids = new Mat();

        int nLabels = Cv2.ConnectedComponentsWithStats(
            bw, labels, stats, centroids,
            PixelConnectivity.Connectivity8, MatType.CV_32S);

        Mat cleaned = new Mat(bw.Size(), MatType.CV_8UC1, Scalar.All(0));

        for (int label = 1; label < nLabels; label++)
        {
            int area = stats.At<int>(label, (int)ConnectedComponentsTypes.Area);
            if (area < minArea)
            {
                continue;
            }

            Mat mask = new Mat();
            Cv2.InRange(labels, new Scalar(label), new Scalar(label), mask);
            cleaned.SetTo(new Scalar(255), mask);
            mask.Dispose();
        }


        labels.Dispose();
        stats.Dispose();
        centroids.Dispose();

        return cleaned;
    }

}
