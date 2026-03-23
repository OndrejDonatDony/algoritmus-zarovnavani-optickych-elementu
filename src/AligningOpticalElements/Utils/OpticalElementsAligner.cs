using OpenCvSharp;

namespace AligningOpticalElements;

public class OpticalElementsAligner
{
    private List<Spot> spots;
    private int numOfSpots;

    private Spot sampleSpot;
    private Spot refSpot;
    private Spot sampleSpotShiftZ = null;
    private int sampleShiftZ = 0;
    private int sampleShiftXY = 0;
    private int whitePixels;
    private int whitePixelsRef;
    private int pixelsDiff;

    private List<Spot> sampleSpots;
    private List<int> distances;
    private bool sampleFound;
    private int stateOfPosition = -1;
    private int numOfFirstSpots;
    private float px = 5.248f;
    private int whiteBorder = -1;
    public Spot GetRefSpot { get { return refSpot; } }
    public Spot GetSampleSpot { get { return sampleSpot; } }
    public int GetNumOfFirstSpots { get { return numOfFirstSpots; } }
    public int GetNumOfSpots { get { return numOfSpots; } }
    public List<Spot> GetSpots { get { return spots; } }
    public List<Spot> GetSampleSpots { get { return sampleSpots; } }
    public Spot GetSampleSpotShiftZ { get { return sampleSpotShiftZ; } }
    public List<int> GetDistances { get { return distances; } }
    public bool GetSampleFound { get { return sampleFound; } }
    public int GetStateOfPosition { get { return stateOfPosition; } }
    public int GetWhiteBorder { get { return whiteBorder; } }
    public int GetWhitePixels { get { return whitePixels; } }
    public int GetWhitePixelsRef { get { return whitePixelsRef; } }
    public int GetPixelsDiff { get { return pixelsDiff; } }
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
   


    public void ReferenceSpot(Mat img)
    {
        List<int> distances = new List<int>();

        this.whitePixelsRef = LoadSpots(img);
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
                (spots[0], spots[1]) = (spots[1], spots[0]);
                (distances[0], distances[1]) = (distances[1], distances[0]);
            }
        }
        this.numOfFirstSpots = numOfSpots;
        this.refSpot = spots[0];
        this.distances = distances;
    }


    public void SampleSpot(Mat img, float ZS)
    {
        bool sampleFound = false;

        this.whitePixels = LoadSpots(img);
        Console.WriteLine(GetNumOfFirstSpots + "first");
        Console.WriteLine(GetNumOfSpots + "all");

        if (GetNumOfFirstSpots == GetNumOfSpots)
        {
            FindWhiteBorder(img);
            if(GetWhiteBorder > 0)
            {
                int[] arr = new int[]
                {
                    -img.Height / 2,
                     img.Height / 2,
                     img.Width / 2,
                    -img.Width / 2
                };
                string[] shiftInfo = new string[]
                {
                    (-img.Height / 2)/px + "mm dolu",
                    ( img.Height / 2)/px + "mm nahoru",
                    ( img.Width  / 2)/px + "mm doprava",
                    (-img.Width  / 2)/px + "mm doleva"
                };
                Console.WriteLine("posunte vzorek o " + shiftInfo[GetWhiteBorder - 1]);
                return;
            }
      
            int pixelsDiff = GetWhitePixels - GetWhitePixelsRef;
            if(GetWhitePixels > GetWhitePixelsRef+100 && !GetSampleFound)
            {
                Console.WriteLine("potreba posunu po ose Z");
                this.sampleFound = true;
                this.pixelsDiff = pixelsDiff;
                return;
            } 
            else if (pixelsDiff > this.pixelsDiff && GetSampleFound) 
            {
                this.sampleShiftZ = -GetSampleShiftZ;
                this.sampleFound = false;
                Console.WriteLine("zapotrebi opacneho posunu po ose Z:" + GetSampleShiftZ);
                return;
            }
            else
            {
                Console.WriteLine("potreba posunout vzorek");
                this.sampleFound = false;
                return;
            }
            
        }
        else if (GetNumOfFirstSpots +1 == GetNumOfSpots)
        {
            this.sampleFound = true;
            List<int> tempDistances = new List<int>();

            foreach (Spot sp in GetSpots)
            {
                int x = img.Width / 2 - sp.GetCoordX;
                int y = img.Height / 2 - sp.GetCoordY;
                tempDistances.Add(x * x + y * y);
            }

            int maxDiff = -1;
            int sampleIndex = -1;
            int diff;

            for (int i = 0; i < tempDistances.Count; i++)
            {
                int minDiff = int.MaxValue;

                for (int j = 0; j < GetDistances.Count; j++)
                {
                    diff = Math.Abs(tempDistances[i] - GetDistances[j]);
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
            if (GetSampleFound)
            {
                
                FindWhiteBorder(img);

                if (GetWhiteBorder < 1 && sampleIndex >= 0)
                {
                    Console.WriteLine("vzorek se nasel");
                    this.sampleSpot = GetSpots[sampleIndex];
                    this.sampleFound = true;
                    this.sampleSpotShiftZ = GetSpots[sampleIndex];
                    SampleMoveZ(ZS);
                    return;
                }
                int[] arr = new int[]
                {
                    -img.Height / 2,
                     img.Height / 2,
                     img.Width / 2,
                    -img.Width / 2
                };
                string[] shiftInfo = new string[]
                {
                    (-img.Height / 2)/px + "mm dolu",
                    ( img.Height / 2)/px + "mm nahoru",
                    ( img.Width  / 2)/px + "mm doprava",
                    (-img.Width  / 2)/px + "mm doleva"
                };
                Console.WriteLine("posunte vzorek o " + shiftInfo[GetWhiteBorder - 1]);
                return;
            }
        }

        Console.WriteLine("neco se pokazilo ve vzorku");
        this.sampleFound = sampleFound;
    }


    public string SampleMoveXY()
    {
        this.stateOfPosition = GetStateOfPosition + 1;
        if(GetStateOfPosition == 9)
        {
            return "spatne nastaveny posun";
        }
        int shift = this.sampleShiftXY;
        (int dx, int dy)[] N8 =
            {
                (-shift,-shift), (shift,0), (shift,0),
                (0, shift),          (0, shift),
                (-shift, 0), (-shift, 0), (0, -shift)
            };

        var move = N8[GetStateOfPosition];
        return $"MOVE {move.dx} {move.dy}";
    }


    public void SampleMoveZ(float ZS)
    {

        float RVS = sampleSpotShiftZ.GetRadius;
        float RV = sampleSpot.GetRadius;
        float RC = refSpot.GetRadius;

        //RVS radius sample shifted 
        //RV radius sample
        //RC radius center
        //zValue set by default
        Console.WriteLine(RV + " " + RVS);
        float zDistance = ZS * (RC - RVS) / (RV - RVS);
     
        this.sampleSpot = new Spot(sampleSpot.GetCoordX, sampleSpot.GetCoordY, sampleSpot.GetRadius, zDistance);
    }

    protected int LoadSpots(Mat img)
    {
        List<Spot> spots = new List<Spot>();

        if (img == null || img.Empty())
        {
            Console.WriteLine("LoadSpots: img je null nebo empty");
            this.spots = spots;
            this.numOfSpots = 0;
            return 0;
        }
        (Mat bin, int whitePixels) = BinaryImg(img);
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
        return whitePixels;
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


    protected (Mat,int) BinaryImg(Mat img)
    {
        Mat g = ToGray(img);

        Mat blur = new Mat();
        Cv2.GaussianBlur(g, blur, new Size(5, 5), 0);

        Mat bw = new Mat();
        Cv2.Threshold(blur, bw, 20, 255, ThresholdTypes.Binary);

        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5));
        Mat cleaned = new Mat();
        Cv2.MorphologyEx(bw, cleaned, MorphTypes.Open, kernel);
        Cv2.MorphologyEx(cleaned, cleaned, MorphTypes.Close, kernel);

        Mat result = AreaOpen(cleaned, 20);
        whitePixels = Cv2.CountNonZero(result);

        ShowImage(result);
        g.Dispose();
        blur.Dispose();
        bw.Dispose();
        kernel.Dispose();
        cleaned.Dispose();

        return (result,whitePixels);
    }


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
            this.whiteBorder = -1;
            return;
        }

        (Mat bw, int whitePixels) = BinaryImg(img);

        int rows = bw.Rows;
        int cols = bw.Cols;

        // 1 = nahoře
        for (int x = 0; x < cols; x++)
        {
            if (bw.At<byte>(0, x) > 0)
            {
                this.whiteBorder = 1;
                bw.Dispose();
                return;
            }
        }

        // 2 = dole
        for (int x = 0; x < cols; x++)
        {
            if (bw.At<byte>(rows - 1, x) > 0)
            {
                this.whiteBorder = 2;
                bw.Dispose();
                return;
            }
        }

        // 3 = vlevo
        for (int y = 0; y < rows; y++)
        {
            if (bw.At<byte>(y, 0) > 0)
            {
                this.whiteBorder = 3;
                bw.Dispose();
                return;
            }
        }

        // 4 = vpravo
        for (int y = 0; y < rows; y++)
        {
            if (bw.At<byte>(y, cols - 1) > 0)
            {
                this.whiteBorder = 4;
                bw.Dispose();
                return;
            }
        }
        this.whiteBorder = 0;
        bw.Dispose();
        return;
    }
}