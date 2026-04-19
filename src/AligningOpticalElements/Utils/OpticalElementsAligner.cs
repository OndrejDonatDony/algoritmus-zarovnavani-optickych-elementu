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
    NoSampleShift,
    MissingZ
}

public enum SpotKeyA
{
    RefSpot,
    ParSpot,
    SampleSpot,
    SampleSpotShiftZ,
    unintSpot1,
    unintSpot2
}

public enum SpotOnBorder
{
    None,
    Left,
    Right,
    Top,
    Bottom,
}
public class OpticalElementsAligner
{
  
    private List<Spot> spots = new();
    private Dictionary<SpotKeyA, Spot> spotMap = new();
    private bool firstAlignCoordZ = false;

    private int numOfFirstSpots;
    private bool allSpotsOnImage = false;
    private float sampleShiftZ;
    private int sampleShiftXY = 0;
    private int pixelsDiff;

    private int stateOfShiftXY = 0;
    private float px = 5.248f;
    private AlignError evaluation = AlignError .NoSpots;
    private SpotOnBorder spotOnBorder;
    public IReadOnlyList<Spot> GetSpots => spots;
    public IReadOnlyDictionary<SpotKeyA, Spot> GetSpotMap => spotMap;
    public int GetStateOfPosition => stateOfShiftXY;
    public int GetPixelsDiff => pixelsDiff;

    public SpotOnBorder GetSpotOnBorder()
    {
        return spotOnBorder;
    }
    public float GetSampleShiftZ
    { 
        get => sampleShiftZ;
        set => sampleShiftZ = value;
    }
    public Spot GetRefSpot => spotMap.TryGetValue(SpotKeyA.RefSpot, out var s) ? s : null;
    public Spot GetSampleSpot => spotMap.TryGetValue(SpotKeyA.SampleSpot, out var s) ? s : null;
    public Spot GetSampleSpotShiftZ => spotMap.TryGetValue(SpotKeyA.SampleSpotShiftZ, out var s) ? s : null;

    public AlignError GetEvaluation
    {
        get => evaluation;
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

    public void AlignTest()
    {

        evaluation = AlignError.Ok;
        if (spots.Count == 0)
        {
            evaluation = AlignError.NoSpots;
            spots.Clear();
        }
        else if (spots.Count > 3)
        {
            evaluation = AlignError.TooManySpots;
            spots.Clear();
        }
        else if (spotMap[SpotKeyA.SampleSpot] == null)
        {
            evaluation = AlignError.SampleNotFound;
        }
        else if (spotOnBorder != SpotOnBorder.None)
        {
            evaluation = AlignError.SampleOnEdge;
            //spotMap[SpotKeyA.SampleSpotShiftZ] != null && spotMap[SpotKeyA.SampleSpot] != null &&
            //spots.Count != spotMap.Values.Count(v => v != null))
        } 
        else if (spotMap[SpotKeyA.SampleSpot].GetCoordZ == 0)
        {
            evaluation = AlignError.MissingZ;
        }
        else if (spotMap[SpotKeyA.SampleSpot]?.GetCoordZ == spotMap[SpotKeyA.SampleSpotShiftZ]?.GetCoordZ)
        {
            evaluation = AlignError.NoSampleShift;
        }
        return;
    }
    public void InitSpotMap()
    {
        spotMap.Clear();
        foreach (SpotKeyA key in Enum.GetValues(typeof(SpotKeyA)))
        {
            spotMap[key] = null;
        }
    }

    public void ReferenceSpot(Mat img,int threshold)
    {
        List<double> distances = new List<double>();
        LoadSpots(img,threshold);

        if (spots.Count == 0)
        {
            AlignTest();
            return;
        }
        if (spots.Count > 3)
        {
            AlignTest();
            return;
        }


        if (spots.Count == 1)
        {
            spotMap[SpotKeyA.RefSpot] = spots[0];
        }
        else
        {
            foreach (Spot sp in spots)
            {
                int x = img.Width / 2 - sp.GetCoordX;
                int y = img.Height / 2 - sp.GetCoordY;
                distances.Add(x * x + y * y);
            }
            double min = distances[0];
            spotMap[SpotKeyA.RefSpot] = spots[0];
            for (int i = 1; i < distances.Count; i++)
            {
                if (distances[i] < min)
                {
                    min = distances[i];
                    spotMap[SpotKeyA.RefSpot] = spots[i];
                }
            }
            spots.Remove(spotMap[SpotKeyA.RefSpot]);
            spots.Add(spotMap[SpotKeyA.RefSpot]);
            if (spots.Count == 3)
            {
                spotMap[SpotKeyA.unintSpot1] = spots[0];
                spotMap[SpotKeyA.unintSpot2] = spots[1];
            }
            else if (spots.Count == 2)
            {
                spotMap[SpotKeyA.unintSpot1] = spots[0];
            }
        }

        this.numOfFirstSpots = spots.Count;
        AlignTest();
    }

    public void SampleSpot(Mat img, int threshold)
    {
        if(spotMap[SpotKeyA.SampleSpot] != null)
        {
            spotMap[SpotKeyA.SampleSpotShiftZ] = spotMap[SpotKeyA.SampleSpot].Clone();
        }
   
        //histo-por !!!
        LoadSpots(img,threshold);
        Console.WriteLine(numOfFirstSpots + " first");
        Console.WriteLine(spots.Count() + " actual");
        if (spots.Count == 0)
        {
            Console.WriteLine("zadny spot v samplu");
            AlignTest();
            return;
        }
        if(spots.Count == 1)
        {
            Console.WriteLine("jeden v samplu");
            AlignTest();
            return;
        }
        if (spots.Count == 2) 
        {
            Console.WriteLine("dva v samplu");
            for (int i = 0; i < spots.Count; i++)
            {
                double dxRef = spotMap[SpotKeyA.RefSpot].GetCoordX - spots[i].GetCoordX;
                double dyRef = spotMap[SpotKeyA.RefSpot].GetCoordY - spots[i].GetCoordY;
                double refSpotCheck = dxRef * dxRef + dyRef * dyRef;

                Spot parCandidate = spotMap[SpotKeyA.unintSpot1];

                if (parCandidate != null)
                {
                    double dxPar = parCandidate.GetCoordX - spots[i].GetCoordX;
                    double dyPar = parCandidate.GetCoordY - spots[i].GetCoordY;
                    double parSpotCheck = dxPar * dxPar + dyPar * dyPar;

                    if (parSpotCheck < 9)
                    {
                        Console.WriteLine("parazit se nasel");
                        spotMap[SpotKeyA.ParSpot] = spots[i];
                    }
                }
                else
                {
                    Console.WriteLine("parazit neexistuje (null)");
                }
                if (refSpotCheck > 9 && parCandidate == null)
                {
                    Console.WriteLine("vzorek se nasel s");
                    spotMap[SpotKeyA.SampleSpot] = spots[i];
                }
            }
        }
        if (spots.Count == 3)
        {
            Console.WriteLine("tri v samplu");
            for (int i = 0; i < spots.Count; i++)
            {
                double dxRef = spotMap[SpotKeyA.RefSpot].GetCoordX - spots[i].GetCoordX;
                double dyRef = spotMap[SpotKeyA.RefSpot].GetCoordY - spots[i].GetCoordY;
                double refSpotCheck = dxRef * dxRef + dyRef * dyRef;

                Spot unit1 = spotMap[SpotKeyA.unintSpot1];
                Spot unit2 = spotMap[SpotKeyA.unintSpot2];

                double Unit1SpotCheck = double.MaxValue;
                double Unit2SpotCheck = double.MaxValue;

                if (unit1 != null)
                {
                    double dxUnit1 = unit1.GetCoordX - spots[i].GetCoordX;
                    double dyUnit1 = unit1.GetCoordY - spots[i].GetCoordY;
                    Unit1SpotCheck = dxUnit1 * dxUnit1 + dyUnit1 * dyUnit1;
                }

                if (unit2 != null)
                {
                    double dxUnit2 = unit2.GetCoordX - spots[i].GetCoordX;
                    double dyUnit2 = unit2.GetCoordY - spots[i].GetCoordY;
                    Unit2SpotCheck = dxUnit2 * dxUnit2 + dyUnit2 * dyUnit2;
                }

                double parSpotCheck = Unit2SpotCheck + Unit1SpotCheck;

                if (refSpotCheck < 9)
                {
                    continue;
                }
                if (Unit1SpotCheck < 9 || Unit2SpotCheck < 9)
                {
                    spotMap[SpotKeyA.ParSpot] = spots[i];
                }
                else
                {
                    spotMap[SpotKeyA.SampleSpot] = spots[i];

                    Console.WriteLine("vzorek se nasel");
                    AlignTest();
                }
            }

        }
        if (spots.Count > 3)
        {
            Console.WriteLine("prilis v samplu");
            AlignTest();
            return;
        }
    }

    public void SampleMoveXY(Mat img)
    {
        FindWhiteBorder(img);
        AlignTest();
        if (spotOnBorder != SpotOnBorder.None)
        {
            string shiftFromBorder = "";

            if (spotOnBorder == SpotOnBorder.Left)
            {
                shiftFromBorder = (-img.Width / 2f / px) + " mm doleva";
            }
            else if (spotOnBorder == SpotOnBorder.Right)
            {
                shiftFromBorder = (img.Width / 2f / px) + " mm doprava";
            }
            else if (spotOnBorder == SpotOnBorder.Top)
            {
                shiftFromBorder = (-img.Height / 2f / px) + " mm dolu";
            }
            else if (spotOnBorder == SpotOnBorder.Bottom)
            {
                shiftFromBorder = (img.Height / 2f / px) + " mm nahoru";
            }

            Console.WriteLine("posunte vzorek o " + shiftFromBorder);
            AlignTest();
            return;
        }
        

        if (stateOfShiftXY >= 9)
        {
            Console.WriteLine("spatne nastaveny posun");
            return;
        }
        if(spots.Count == 3 && !allSpotsOnImage)
        {
            stateOfShiftXY = 0;
            sampleShiftXY = sampleShiftXY / 4;
            allSpotsOnImage = true;
        }
        if(spots.Count != 3 && allSpotsOnImage)
        {
            stateOfShiftXY = 0;
            sampleShiftXY = sampleShiftXY * 4;
            allSpotsOnImage = false;
        }
        int shift = sampleShiftXY;

        (int dx, int dy)[] n8 =
        {
            (-shift, shift), (shift, 0), (shift, 0),
            (0, -shift), (0, -shift),
            (-shift, 0), (-shift, 0), (0, shift)
        };

        var move = n8[stateOfShiftXY];
        stateOfShiftXY++;
        Console.WriteLine($"MOVE {move.dx} {move.dy}");
        AlignTest();
        return;
    }

    public void SampleMoveZ(Mat img)
    {
        LoadSpots(img,0);
        if (firstAlignCoordZ)
        {
            sampleShiftZ = -sampleShiftZ;
        }
        firstAlignCoordZ = true;
        if (spotMap[SpotKeyA.SampleSpotShiftZ] == null ||
            spotMap[SpotKeyA.SampleSpot] == null ||
            spotMap[SpotKeyA.RefSpot] == null)
        {
            Console.WriteLine("nejaka chyba na z");
            return;
        }

        Spot shifted = spotMap[SpotKeyA.SampleSpotShiftZ];
        Spot sample = spotMap[SpotKeyA.SampleSpot];
        Spot reference = spotMap[SpotKeyA.RefSpot];

        float RVS = shifted.GetRadius;
        float RV = sample.GetRadius;
        float RC = reference.GetRadius;

        Console.WriteLine("RVS " + RVS + " RV " + RV + " RC " + RC);
        float zDistance = sampleShiftZ * (RC - RVS) / (RV - RVS);
        Console.WriteLine("zdist " + zDistance);
        spotMap[SpotKeyA.SampleSpot] = new Spot(
            sample.GetCoordX,
            sample.GetCoordY,
            sample.GetRadius,
            -zDistance);
        Console.WriteLine("vzorek Z");
        AlignTest();
    }

    private void LoadSpots(Mat img,int treshold) //TH zakomponovat
    {
        List<Spot> sp = new List<Spot>();
        if (img == null || img.Empty())
        {
            Console.WriteLine("LoadSpots: img je null nebo empty");
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
                sp.Add(new Spot(xImg, yImg, radiusPx, 0f));
            }
        }
        //border
        FindWhiteBorder(img);

        spots = sp;
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
        (Mat bw, int _) = BinaryImg(img);

        int rows = bw.Rows;
        int cols = bw.Cols;

        for (int x = 0; x < cols; x++)
        {
            if (bw.At<byte>(0, x) > 0)
            {
                spotOnBorder = SpotOnBorder.Top;
                Console.WriteLine("Vzorek se nachází na horním okraji");
                bw.Dispose();
                return;
            }
        }

        for (int x = 0; x < cols; x++)
        {
            if (bw.At<byte>(rows - 1, x) > 0)
            {
                spotOnBorder = SpotOnBorder.Bottom;
                Console.WriteLine("Vzorek se nachází na spodním okraji");
                bw.Dispose();
                return;
            }
        }

        for (int y = 0; y < rows; y++)
        {
            if (bw.At<byte>(y, 0) > 0)
            {
                spotOnBorder = SpotOnBorder.Left;
                Console.WriteLine("Vzorek se nachází na levém okraji");
                bw.Dispose();
                return;
            }
        }

        for (int y = 0; y < rows; y++)
        {
            if (bw.At<byte>(y, cols - 1) > 0)
            {
                spotOnBorder = SpotOnBorder.Right;
                Console.WriteLine("Vzorek se nachází na pravém okraji");
                bw.Dispose();
                return;
            }
        }

        spotOnBorder = SpotOnBorder.None;
        bw.Dispose();
    }
}