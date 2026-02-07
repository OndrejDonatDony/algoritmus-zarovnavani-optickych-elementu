using OpenCvSharp;


class Program
{
    private const string PrefixToRemove =
        @"C:\Users\ondre\Desktop\BAKALÁŘKA\BakalarniPrace\data\";

    private static void Main(string[] args)
    {
        //string listPath = @"C:\Users\ondre\Desktop\BAKALÁŘKA\BakalarniPrace\data\odkaz.txt";
        //string listPath = @"C:\Users\ondre\Desktop\BAKALÁŘKA\BakalarniPrace\data\bezVzorkuF_1.9.txt";
        //string listPath = @"C:\Users\ondre\Desktop\BAKALÁŘKA\BakalarniPrace\data\bezVzorkuR585_v1.txt";
        //string listPath = @"C:\Users\ondre\Desktop\BAKALÁŘKA\BakalarniPrace\data\bezVzorkuZygo_flat_4.txt";
        //string listPath = @"C:\Users\ondre\Desktop\BAKALÁŘKA\BakalarniPrace\data\seVzorkemR585_v1+R52CE.txt";
        //string listPath = @"C:\Users\ondre\Desktop\BAKALÁŘKA\BakalarniPrace\data\seVzorkemR585_v1+R52CF.txt";
        string listPath = @"C:\Users\ondre\Desktop\BAKALÁŘKA\BakalarniPrace\data\seVzorkemZygo_flat_4+flat.txt";
        List<Image> images = SpotUtils.ImportImages(listPath);
        if (images == null || images.Count == 0)
        {
            Console.WriteLine("Žádné obrázky.");
            return;
        }

        var center = SpotUtils.ReferenceSpot(images[0]);


        foreach (Image im in images)
        {
            //jeden fcking objekt



            float sampleDistance = 0;
            Spot sp = null;
            if (im.GetSpots.Count >= 2)

            {
                (sp, sampleDistance) = SpotUtils.SampleDistance(center.GetCoordX, center.GetCoordY, im, images);
            }

            Console.WriteLine(
                $"CenterSpot: x={center.GetCoordX:F4}, " +
                $"y={center.GetCoordY:F4}, " +
                $"z={center.GetCoordZ:F4}, " +
                $"r={center.GetRadius:F4}"
            );

            if (sp != null)
            {
                Console.WriteLine($"vzorek je ve vzdalenosti {sampleDistance:F4}");
                Console.WriteLine(
                "  Spot vzorek: x={0:F4}, y={1:F4}, r={2:F4}, z={3:F4}",
                sp.GetCoordX,
                sp.GetCoordY,
                sp.GetRadius,
                sp.GetCoordZ
                );
            }

            string file = im.GetImage;

            string title = file.StartsWith(PrefixToRemove)
                ? file.Substring(PrefixToRemove.Length)
                : file;

            Console.WriteLine($"Image: {title}");

            var spots = im.GetSpots;
            for (int i = 0; i < spots.Count; i++)
            {
                var s = spots[i];
                Console.WriteLine("  Spot {0}: x={1:F4}, y={2:F4}, r={3:F4}, z={4:F4}", i, s.GetCoordX, s.GetCoordY, s.GetRadius, s.GetCoordZ);

            }
            Console.WriteLine();

            using var img = Cv2.ImRead(file, ImreadModes.Color);
            if (img.Empty())
            {
                Console.WriteLine($"  [WARN] Nelze načíst: {file}");
                continue;
            }
            Console.WriteLine();
            // stejné kroky jako v ImportImages (aby zobrazení odpovídalo detekci)
            using var gray = SpotUtils.ToGray(img);
            using var bw = SpotUtils.BinaryImg(img);

            // HConcat chce stejný počet kanálů -> převedeme na BGR
            using var gray3 = new Mat();
            using var bw3 = new Mat();

            Cv2.CvtColor(gray, gray3, ColorConversionCodes.GRAY2BGR);
            Cv2.CvtColor(bw, bw3, ColorConversionCodes.GRAY2BGR);

            var blobs = GetTopBlobs(bw, 8, 2f, 0.8f);
            DrawBlobs(gray3, bw3, blobs);

            // převod normalizovaných souřadnic (-1..1) zpět na pixely
            int W = gray.Width;
            int H = gray.Height;

            // zelený center
            DrawNormalizedPoint(gray3, bw3, center.GetCoordX, center.GetCoordY,
                                W, H, new Scalar(0, 255, 0));

            if (sp != null)
            {
                DrawNormalizedPoint(gray3, bw3, sp.GetCoordX, sp.GetCoordY,
                                W, H, new Scalar(255, 0, 0));
            }



            using var combined = new Mat();
            Cv2.HConcat(new Mat[] { bw3, gray3 }, combined);

            Cv2.ImShow(title, combined);

            int key = Cv2.WaitKey();
            Cv2.DestroyWindow(title);

            if (key == 27) break; // ESC
        }

        Cv2.DestroyAllWindows();
    }

    private static List<Blob> GetTopBlobs(Mat bw8, int count, float minRadius, float suppress)
    {
        using var dist = new Mat();
        Cv2.DistanceTransform(bw8, dist, DistanceTypes.L2, DistanceTransformMasks.Mask5);

        var candidates = new List<Blob>();

        int w = dist.Width;
        int h = dist.Height;

        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                float r = dist.At<float>(y, x);
                if (r < minRadius) continue;

                candidates.Add(new Blob(new Point2f(x, y), r));
            }
        }

        candidates.Sort((a, b) => b.R.CompareTo(a.R));

        var selected = new List<Blob>(count);

        foreach (var c in candidates)
        {
            bool ok = true;
            foreach (var s in selected)
            {
                float dx = c.C.X - s.C.X;
                float dy = c.C.Y - s.C.Y;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);

                float limit = suppress * (c.R + s.R);
                if (d < limit) { ok = false; break; }
            }

            if (!ok) continue;

            selected.Add(c);
            if (selected.Count >= count) break;
        }

        return selected;
    }

    private static void DrawBlobs(Mat a, Mat b, List<Blob> blobs)
    {
        var color = new Scalar(0, 0, 255);
        foreach (var c in blobs)
        {
            int r = Math.Max(1, (int)Math.Round(c.R));
            var center = new Point((int)Math.Round(c.C.X), (int)Math.Round(c.C.Y));
            Cv2.Circle(a, center, r, color, 2);
            Cv2.Circle(b, center, r, color, 2);
        }
    }

    static Point ToPixel(double normX, double normY, int width, int height)
    {
        int px = (int)Math.Round(normX * (width / 2.0) + (width / 2.0));
        int py = (int)Math.Round((height / 2.0) - normY * (height / 2.0));

        return new Point(px, py);
    }
    static void DrawNormalizedPoint(Mat a, Mat b, double normX, double normY,
                                int width, int height, Scalar color, int radius = 4)
    {
        Point p = ToPixel(normX, normY, width, height);

        Cv2.Circle(a, p, radius, color, -1);
        Cv2.Circle(b, p, radius, color, -1);
    }

}
