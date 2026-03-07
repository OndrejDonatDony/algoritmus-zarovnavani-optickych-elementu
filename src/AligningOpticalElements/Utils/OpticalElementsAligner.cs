using OpenCvSharp;
using static OpenCvSharp.FileStorage;
using static OpenCvSharp.ML.DTrees;

namespace AligningOpticalElements;

public class OpticalElementsAligner
{
    private List<Spot> spots;
    private int numOfSpots = 0;
    private Spot sampleSpot;
    private Spot refSpot;
    private List<Spot> sampleSpots;
    private List<int> distances;
    private bool sampleFound;
    private int stateOfPosition = -1;

    public Spot GetRefSpot { get { return refSpot; } }
    public Spot GetSampleSpot { get { return sampleSpot; } }
    public int GetNumOfSpots { get { return numOfSpots; } }
    public List<Spot> GetSpots { get { return spots; } }
    public List<Spot> GetSampleSpots { get { return sampleSpots; } }
    public List<int> GetDistances { get { return distances; } }
    public bool GetSampleFound { get { return sampleFound; } }
    public int GetStateOfPosition { get { return stateOfPosition; } }
    public int GetShift { get; set; }

    public void ReferenceSpot(Mat img)
    {
        List<int> distances = new List<int>();

        LoadSpots(img);
        if (GetNumOfSpots == 0)
        {
            throw new Exception("nenasly se zadne body");
        }
        else if (GetNumOfSpots > 2)
        {
            throw new Exception("prilis mnoho bodu");
        }

        foreach (Spot sp in GetSpots)
        {
            int x = img.Width / 2 - sp.GetCoordX;
            int y = img.Height / 2 - sp.GetCoordY;
            distances.Add(x * x + y * y);
        }

        if (GetNumOfSpots == 2)
        {
            if (distances[0] > distances[1])
            {
                (this.spots[0], this.spots[1]) = (this.spots[1], this.spots[0]);
                (distances[0], distances[1]) = (distances[1], distances[0]);
            }
        }

        this.refSpot = this.spots[0];
        this.distances = distances;
    }

    public void SampleSpot(Mat img)
    {
        bool sampleFound = false;
        int refNumOfSpots = this.numOfSpots;
        List<int> refDistances = new List<int>(this.distances);

        LoadSpots(img);

        if (GetNumOfSpots == refNumOfSpots)
        {
            Console.WriteLine("je potreba posunout vzorek");
            this.sampleFound = sampleFound;
            this.sampleSpot = null;
            return;
        }
        else if (GetNumOfSpots == refNumOfSpots + 1)
        {
            List<int> tempDistance = new List<int>();

            foreach (Spot sp in GetSpots)
            {
                int x = img.Width / 2 - sp.GetCoordX;
                int y = img.Height / 2 - sp.GetCoordY;
                tempDistance.Add(x * x + y * y);
            }

            int maxDiff = -1;
            int sampleIndex = -1;

            for (int i = 0; i < tempDistance.Count; i++)
            {
                int minDiff = int.MaxValue;

                for (int j = 0; j < refDistances.Count; j++)
                {
                    int diff = Math.Abs(tempDistance[i] - refDistances[j]);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                    }
                }

                if (minDiff > maxDiff)
                {
                    maxDiff = minDiff;
                    sampleIndex = i;
                }
            }

            if (sampleIndex >= 0)
            {
                sampleFound = true;
                this.sampleSpot = GetSpots[sampleIndex];
                this.sampleFound = sampleFound;
                return;
            }
        }

        Console.WriteLine("neco se pokazilo");
        this.sampleFound = sampleFound;
        this.sampleSpot = null;
    }

    protected void LoadSpots(Mat img)
    {
        List<Spot> spots = new List<Spot>();

        if (img == null || img.Empty())
        {
            this.spots = spots;
            this.numOfSpots = 0;
            return;
        }

        Mat bin = BinaryImg(img);

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

        this.numOfSpots = numOfSpots;
        this.spots = spots;
    }

    public string SampleMoveXY()
    {
        this.stateOfPosition = GetStateOfPosition + 1;
        if(GetStateOfPosition == 9)
        {
            return "spatne nastaveny posun";
        }

        (int dx, int dy)[] N8 =
        {
        (-GetShift, -GetShift), (0, -GetShift), (GetShift, -GetShift),
        (-GetShift, 0),                    (GetShift, 0),
        (-GetShift, GetShift),  (0, GetShift),  (GetShift, GetShift)
    };

        var move = N8[GetStateOfPosition];
        return $"MOVE {move.dx} {move.dy}";
    }

    //vzorek se posune +Z
    public void SampleZAxisDistance()
    {
        float zDistance = 0f;
        if (sampleSpot.GetRadius <= refSpot.GetRadius)
        {
            zDistance = 0f;
            sampleSpot.GetCoordZ = zDistance;
            return;
        }

        float RVS = sampleSpot.GetRadius; //radius sample shift 
        float RV = this.sampleSpot.GetRadius; //radius sample
        float RC = refSpot.GetRadius; //radius of center spot

        float zValue = 0.01f; //Z shift from interferometer

        if (RV < RVS)
        {
            zDistance = zValue * (RC - RVS) / (RV - RVS);
        }
        else
        {
            zDistance = -zValue * (RC - RVS) / (RV - RVS);
        }
        sampleSpot.GetCoordZ = zDistance;
    }

    protected Mat ToGray(Mat img)
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
    protected Mat BinaryImg(Mat img)
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
    protected Mat AreaOpen(Mat bw, int minArea)
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