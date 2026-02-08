using OpenCvSharp;


public static class SpotUtils
{
    
    public static Spot? ReferenceSpot(Image image)
    {
        List<Spot> spots = image.GetSpots;

        if (spots == null || spots.Count == 0)
            return null;

        return spots.MinBy(sp => sp.GetCoordX);
    }

    public static float[] DistanceOfSpots(float x, float y, Image image)
    {
        float dx;
        float dy;
        float[] distance = { 0, 0, 0 };
        int c = 0;
        foreach (Spot sp in image.GetSpots){
            dx = sp.GetCoordX;
            dy = sp.GetCoordY;
            if(dx == x && dy == y)
            {
                Console.WriteLine("error1");
                continue;
            }
            
            distance[c] = ((float)Math.Sqrt(Math.Pow(Math.Abs(dx) - Math.Abs(x),2)
                + Math.Pow(Math.Abs(dy) - Math.Abs(y),2)));
           
            c++;
        }
        
        return distance;
    }
    public static (Spot? , float) SampleDistance(float xr, float yr, Image image, List<Image> images)
    {
        
        float[] distances = DistanceOfSpots(xr, yr, image);

        List<float[]> distancesToCompare = new List<float[]>();
        foreach (Image img in images) {
           if(img.GetSpots.Count > 3)
            {
                Console.WriteLine("prilis mnoho spotu: "+img.GetSpots.Count);
                continue;
            }
            distancesToCompare.Add(DistanceOfSpots(xr, yr, img));
        }
                
        for(int i = 0; i < distancesToCompare.Count; i++) 
        {
            
            for(int j = 0; j<3; j++)
            {
                if (distancesToCompare[i] != distances)
                {
                    bool sample = true;
                    for (int k = 0; k < 3; k++)
                    {
                        if (Math.Abs(distancesToCompare[i][k] - distances[j]) < 0.01)
                        {
                            sample = false;
                            continue;
                        }
                    
                        if(k==2 && sample)
                        {
                        
                            
                            
                            return (image.GetSpots[j], distances[j]);
                        }
                    }
                }     
            }
        }
        return (null,0);
    }


    public static List<Image>? ImportImages(string listPath)
    {

        List<Image> images = new List<Image>();

        if (!File.Exists(listPath))
        {
            Console.WriteLine("Nenalezen soubor: " + listPath);
            return null;
        }

        string[] lines = File.ReadAllLines(listPath);

        // 2) Vyfiltruj prazdne radky
        List<string> files = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length > 0)
            {
                files.Add(line);
            }
        }
        for (int i = 0; i < files.Count; i++)
        {
            Mat img = Cv2.ImRead(files[i], ImreadModes.Color);
            Mat bw = BinaryImg(img);

            (List<Spot> spots, int numOfSpots) = LoadSpots(bw);

            int W = img.Width;
            int H = img.Height;

           

            Image im = new Image(spots, files[i]);
            images.Add(im);

            img.Dispose();
            bw.Dispose();
        }

        Console.WriteLine("Nacteno souboru: " + files.Count);
        return images;

    }
    public static Mat ToGray(Mat img)
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
    public static Mat BinaryImg(Mat img)
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
    private static Mat AreaOpen(Mat bw, int minArea)
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
    private static (List<Spot>, int) LoadSpots(Mat m)
    {
        List<Spot> spots = new List<Spot>();

        if (m == null || m.Empty())
            return (spots,0);

        int W = m.Width;
        int H = m.Height;

        Mat bin = m;
        if (bin.Type() != MatType.CV_8UC1)
        {
            bin = new Mat();
            Cv2.CvtColor(m, bin, ColorConversionCodes.BGR2GRAY);
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
            float xImg = (float)(mom.M10 / mom.M00);
            float yImg = (float)(mom.M01 / mom.M00);

            // převod do kartézského systému se středem obrazu
            float x = (xImg - W / 2f) / (W / 2f);
            float y = (H / 2f - yImg) / (H / 2f);

            float radius = (float)Math.Sqrt(area / Math.PI) / (Math.Min(W, H) / 2f);
            if(radius > 0.005)
            {
                numOfSpots += 1;
                spots.Add(new Spot(x, y, radius, 0f));
            } 
       
        }

        return (spots, numOfSpots);
    }
    //vzorek se posune +Z
    public static (Spot? , float) SampleZAxisDistance(Spot sampleNoShiftZ, Spot sampleShiftZ, float centerRadius)
    {
        float zDistance = 0f;
        if (sampleShiftZ.GetRadius <= centerRadius)
        {
            zDistance = 0f;
            return (sampleShiftZ, zDistance);
        }

        float RVS = sampleNoShiftZ.GetRadius; //radius sample shift 
        float RV = sampleNoShiftZ.GetRadius; //radius sample
        float RC = centerRadius; //radius of center spot

        float zValue = 0.01f; //Z shift from interferometer

        if (RV < RVS){
            zDistance = zValue * (RC - RVS) / (RV - RVS);
        }
        else
        {
            zDistance = -zValue * (RC - RVS) / (RV - RVS);
        }

        return (sampleShiftZ, zDistance);
    }
}