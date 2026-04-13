using OpenCvSharp;

namespace AligningOpticalElements;

public enum AlignError
{
    Ok,
    NoSpots,
    TooManySpots,
    RefNotFound,
    SampleNotFound,
    SampleOnEdge,
    MissingZ
}

public enum SpotKeyA
{
    RefSpot,
    SampleSpot,
    SampleSpotShiftZ
}

public class OpticalElementsAligner
{
  

    private List<Spot> spots = new();
    private Dictionary<SpotKeyA, Spot> spotMap = new();

    private int numOfSpots;
    private int numOfFirstSpots;

    private int sampleShiftZ = 0;
    private int sampleShiftXY = 0;

    private int whitePixels;
    private int whitePixelsRef;
    private int pixelsDiff;

    private List<int> distances = new();
    private bool sampleFound;
    private int stateOfPosition = -1;
    private float px = 5.248f;
    private int whiteBorder = -1;
    private AlignError evaluation;

    public IReadOnlyList<Spot> GetSpots => spots;
    public IReadOnlyDictionary<SpotKeyA, Spot> GetSpotMap => spotMap;
    public List<int> GetDistances => distances;
    public bool GetSampleFound => sampleFound;
    public int GetStateOfPosition => stateOfPosition;
    public int GetWhiteBorder => whiteBorder;
    public int GetWhitePixels => whitePixels;
    public int GetPixelsDiff => pixelsDiff;

    public Spot GetRefSpot => spotMap.TryGetValue(SpotKeyA.RefSpot, out var s) ? s : null;
    public Spot GetSampleSpot => spotMap.TryGetValue(SpotKeyA.SampleSpot, out var s) ? s : null;
    public Spot GetSampleSpotShiftZ => spotMap.TryGetValue(SpotKeyA.SampleSpotShiftZ, out var s) ? s : null;

    public AlignError GetEvaluation
    {
        get => evaluation;
        set => evaluation = value;
    }

    public float GetPx
    {
        get => px;
        set => px = value;
    }

    public int GetSampleShiftXY
    {
        get => sampleShiftXY;
        set => sampleShiftXY = value;
    }

    public int GetSampleShiftZ
    {
        get => sampleShiftZ;
        set => sampleShiftZ = value;
    }

    public void AlignTest()
    {
        GetEvaluation = AlignError.Ok;

        if (GetSpots.Count == 0)
        {
            GetEvaluation = AlignError.NoSpots;
            return;
        }

        if (GetSpots.Count > 3)
        {
            GetEvaluation = AlignError.TooManySpots;
            return;
        }

        if (!spotMap.ContainsKey(SpotKeyA.RefSpot))
        {
            GetEvaluation = AlignError.RefNotFound;
            return;
        }

        if (!spotMap.ContainsKey(SpotKeyA.SampleSpot))
        {
            GetEvaluation = AlignError.SampleNotFound;
            return;
        }

        if (GetWhiteBorder > 0)
        {
            GetEvaluation = AlignError.SampleOnEdge;
            return;
        }

        if (GetSampleSpot.GetCoordZ == 0)
        {
            GetEvaluation = AlignError.MissingZ;
            return;
        }
    }

    public void ReferenceSpot(Mat img)
    {
        LoadSpots(img);

        distances = new List<int>();

        foreach (Spot sp in spots)
        {
            int x = img.Width / 2 - sp.GetCoordX;
            int y = img.Height / 2 - sp.GetCoordY;
            distances.Add(x * x + y * y);
        }

        if (spots.Count == 2 && distances[0] > distances[1])
        {
            (spots[0], spots[1]) = (spots[1], spots[0]);
            (distances[0], distances[1]) = (distances[1], distances[0]);
        }

        numOfFirstSpots = numOfSpots;
        whitePixelsRef = whitePixels;

        if (spots.Count > 0)
        {
            spotMap[SpotKeyA.RefSpot] = spots[0];
        }
    }

    public void SampleSpot(Mat img, float ZS)
    {
        LoadSpots(img);

        Console.WriteLine(numOfFirstSpots + " first");
        Console.WriteLine(numOfSpots + " all");

        if (numOfFirstSpots == numOfSpots)
        {
            FindWhiteBorder(img);

            if (whiteBorder > 0)
            {
                string[] shiftInfo =
                {
                    (-img.Height / 2f / px) + " mm dolu",
                    ( img.Height / 2f / px) + " mm nahoru",
                    ( img.Width  / 2f / px) + " mm doprava",
                    (-img.Width  / 2f / px) + " mm doleva"
                };

                Console.WriteLine("posunte vzorek o " + shiftInfo[whiteBorder - 1]);
                return;
            }

            int currentPixelsDiff = whitePixels - whitePixelsRef;

            if (whitePixels > whitePixelsRef + 100 && !sampleFound)
            {
                Console.WriteLine("potreba posunu po ose Z");
                sampleFound = true;
                pixelsDiff = currentPixelsDiff;
                return;
            }

            if (currentPixelsDiff > pixelsDiff && sampleFound)
            {
                sampleShiftZ = -sampleShiftZ;
                sampleFound = false;
                Console.WriteLine("zapotrebi opacneho posunu po ose Z: " + sampleShiftZ);
                return;
            }

            Console.WriteLine("potreba posunout vzorek");
            sampleFound = false;
            return;
        }

        if (numOfFirstSpots + 1 == numOfSpots)
        {
            List<int> tempDistances = new();

            foreach (Spot sp in spots)
            {
                int x = img.Width / 2 - sp.GetCoordX;
                int y = img.Height / 2 - sp.GetCoordY;
                tempDistances.Add(x * x + y * y);
            }

            int maxDiff = -1;
            int sampleIndex = -1;

            for (int i = 0; i < tempDistances.Count; i++)
            {
                int minDiff = int.MaxValue;

                for (int j = 0; j < distances.Count; j++)
                {
                    int diff = Math.Abs(tempDistances[i] - distances[j]);
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

            FindWhiteBorder(img);

            if (whiteBorder < 1 && sampleIndex >= 0)
            {
                Console.WriteLine("vzorek se nasel");
                spotMap[SpotKeyA.SampleSpot] = spots[sampleIndex];
                spotMap[SpotKeyA.SampleSpotShiftZ] = spots[sampleIndex];
                sampleFound = true;
                SampleMoveZ(ZS);
                return;
            }

            if (whiteBorder > 0)
            {
                string[] shiftInfo =
                {
                    (-img.Height / 2f / px) + " mm dolu",
                    ( img.Height / 2f / px) + " mm nahoru",
                    ( img.Width  / 2f / px) + " mm doprava",
                    (-img.Width  / 2f / px) + " mm doleva"
                };

                Console.WriteLine("posunte vzorek o " + shiftInfo[whiteBorder - 1]);
                return;
            }
        }

        Console.WriteLine("neco se pokazilo ve vzorku");
        sampleFound = false;
    }

    public string SampleMoveXY()
    {
        stateOfPosition++;

        if (stateOfPosition >= 8)
        {
            return "spatne nastaveny posun";
        }

        int shift = sampleShiftXY;

        (int dx, int dy)[] n8 =
        {
            (-shift, -shift), (0, -shift), (shift, -shift),
            (-shift, 0),                     (shift, 0),
            (-shift, shift),  (0, shift),   (shift, shift)
        };

        var move = n8[stateOfPosition];
        return $"MOVE {move.dx} {move.dy}";
    }

    public void SampleMoveZ(float ZS)
    {
        if (!spotMap.ContainsKey(SpotKeyA.SampleSpotShiftZ) ||
            !spotMap.ContainsKey(SpotKeyA.SampleSpot) ||
            !spotMap.ContainsKey(SpotKeyA.RefSpot))
        {
            return;
        }

        Spot shifted = spotMap[SpotKeyA.SampleSpotShiftZ];
        Spot sample = spotMap[SpotKeyA.SampleSpot];
        Spot reference = spotMap[SpotKeyA.RefSpot];

        float RVS = shifted.GetRadius;
        float RV = sample.GetRadius;
        float RC = reference.GetRadius;

        if (Math.Abs(RV - RVS) < 1e-6f)
        {
            return;
        }

        float zDistance = ZS * (RC - RVS) / (RV - RVS);

        spotMap[SpotKeyA.SampleSpot] = new Spot(
            sample.GetCoordX,
            sample.GetCoordY,
            sample.GetRadius,
            zDistance);
    }

    protected void LoadSpots(Mat img)
    {
        List<Spot> foundSpots = new();

        if (img == null || img.Empty())
        {
            Console.WriteLine("LoadSpots: img je null nebo empty");
            spots = foundSpots;
            numOfSpots = 0;
            whitePixels = 0;
            return;
        }

        (Mat bin, int currentWhitePixels) = BinaryImg(img);

        Cv2.FindContours(
            bin,
            out Point[][] contours,
            out HierarchyIndex[] hierarchy,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple
        );

        int foundCount = 0;

        for (int i = 0; i < contours.Length; i++)
        {
            double area = Cv2.ContourArea(contours[i]);
            if (area < 5) continue;

            Moments mom = Cv2.Moments(contours[i]);
            if (Math.Abs(mom.M00) < 1e-9) continue;

            int xImg = (int)Math.Round(mom.M10 / mom.M00);
            int yImg = (int)Math.Round(mom.M01 / mom.M00);
            int radiusPx = (int)Math.Round(Math.Sqrt(area / Math.PI));

            if (radiusPx >= 2)
            {
                foundCount++;
                foundSpots.Add(new Spot(xImg, yImg, radiusPx, 0f));
            }
        }

        numOfSpots = foundCount;
        spots = foundSpots;
        whitePixels = currentWhitePixels;

        bin.Dispose();
    }

    protected Mat ToGray(Mat img)
    {
        if (img.Channels() == 1)
        {
            return img.Clone();
        }

        Mat g = new();
        Cv2.CvtColor(img, g, ColorConversionCodes.BGR2GRAY);
        return g;
    }

    protected (Mat, int) BinaryImg(Mat img)
    {
        Mat g = ToGray(img);

        Mat blur = new();
        Cv2.GaussianBlur(g, blur, new Size(5, 5), 0);

        Mat bw = new();
        Cv2.Threshold(blur, bw, 20, 255, ThresholdTypes.Binary);

        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5));
        Mat cleaned = new();
        Cv2.MorphologyEx(bw, cleaned, MorphTypes.Open, kernel);
        Cv2.MorphologyEx(cleaned, cleaned, MorphTypes.Close, kernel);

        Mat result = AreaOpen(cleaned, 20);
        int currentWhitePixels = Cv2.CountNonZero(result);

        ShowImage(result);

        g.Dispose();
        blur.Dispose();
        bw.Dispose();
        kernel.Dispose();
        cleaned.Dispose();

        return (result, currentWhitePixels);
    }

    protected Mat AreaOpen(Mat bw, int minArea)
    {
        Mat labels = new();
        Mat stats = new();
        Mat centroids = new();

        int nLabels = Cv2.ConnectedComponentsWithStats(
            bw, labels, stats, centroids,
            PixelConnectivity.Connectivity8, MatType.CV_32S);

        Mat cleaned = new(bw.Size(), MatType.CV_8UC1, Scalar.All(0));

        for (int label = 1; label < nLabels; label++)
        {
            int area = stats.At<int>(label, (int)ConnectedComponentsTypes.Area);
            if (area < minArea)
            {
                continue;
            }

            Mat mask = new();
            Cv2.InRange(labels, new Scalar(label), new Scalar(label), mask);
            cleaned.SetTo(new Scalar(255), mask);
            mask.Dispose();
        }

        labels.Dispose();
        stats.Dispose();
        centroids.Dispose();

        return cleaned;
    }

    protected void ShowImage(Mat img)
    {
        if (img == null || img.Empty())
        {
            Console.WriteLine("ShowImage: img je null nebo empty");
            return;
        }

        using var small = new Mat();
        Cv2.Resize(img, small, new Size(), 0.5, 0.5);

        Cv2.ImShow("Reference", small);
        Cv2.WaitKey();
    }

    protected void FindWhiteBorder(Mat img)
    {
        if (img == null || img.Empty())
        {
            whiteBorder = -1;
            return;
        }

        (Mat bw, int _) = BinaryImg(img);

        int rows = bw.Rows;
        int cols = bw.Cols;

        for (int x = 0; x < cols; x++)
        {
            if (bw.At<byte>(0, x) > 0)
            {
                whiteBorder = 1;
                bw.Dispose();
                return;
            }
        }

        for (int x = 0; x < cols; x++)
        {
            if (bw.At<byte>(rows - 1, x) > 0)
            {
                whiteBorder = 2;
                bw.Dispose();
                return;
            }
        }

        for (int y = 0; y < rows; y++)
        {
            if (bw.At<byte>(y, 0) > 0)
            {
                whiteBorder = 3;
                bw.Dispose();
                return;
            }
        }

        for (int y = 0; y < rows; y++)
        {
            if (bw.At<byte>(y, cols - 1) > 0)
            {
                whiteBorder = 4;
                bw.Dispose();
                return;
            }
        }

        whiteBorder = 0;
        bw.Dispose();
    }
}