using AligningOpticalElements;
using OpenCvSharp;
using OpticalElementsSimulator.SimulatorUtils;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using OpticalElementsSimulator.Models;

//5.248px = 1mm
//243.9025mm x 195.122mm
//1280x1024 
//2560x2048 or 2240x1792
//487.805mm x 390.244mm or 426.8292mm x 341.4634mm



//819-1639
//r = 1230
class Program
{
    public enum AlignState
    {
        MainMenu,
        SimulationImages,
        ReferenceImage,
        SampleImage,
        AlignXY,
        AlignZ,
        Test
    }
    
    static void Main()
    {
        
        int hExternal = 2048;
        int wExternal = 2560;

        //test
        hExternal = 1224;
        wExternal = 1480;

        int hInternal = 1024;
        int wInternal = 1280;



        int noise = 30;
        int rr = 50;
        int rs = 20;

        //real
        var aligner = new OpticalElementsAligner();
        List<Spot> spots = null;
        int numOfSpots = 0;
        Spot sample = null;

        //sim
        var sim = new SimulatorUtils();
        List<int> distance = new List<int>();
        Spot spotRefSim = null;
        Spot spotSampleSim = null;
        Mat imageRef = new Mat();
        Mat imageSample = new Mat();

        //trim
        Spot spotRefSimTrim = null;
        Spot spotSampleSimTrim = null;
        Mat imageRefTrim = new Mat();
        Mat imageSampleTrim = new Mat();

        AlignState sw = AlignState.MainMenu;
        bool endProgram = false;
        bool simulation = false;

        do
        {
            //0 sim, 1 Intr
            switch (sw)
            {
                case AlignState.MainMenu:
                    MainMenu();
                    string ?choiceCon = Console.ReadLine();
                    int choice = int.Parse(choiceCon);
                    switch (choice)
                    {
                        case 0:
                            sw = AlignState.SimulationImages;
                            Console.WriteLine("simulace spustena");
                            break;
                        case 1:
                            sw = AlignState.ReferenceImage;
                            Console.WriteLine("interferometr spusten");
                            break;

                        default:
                            Console.WriteLine("spatna volba");
                            break;
                    }
                    break;

                case AlignState.SimulationImages:
                    simulation = true;
                    (spotRefSim, imageRef, spotRefSimTrim, imageRefTrim) = sim.ReferenceImage(
                       hExternal, wExternal,
                       hInternal, wInternal,
                       noise, rr);

                    (spotSampleSim, imageSample, spotSampleSimTrim, imageSampleTrim) = sim.SampleImage(
                       hExternal, wExternal,
                       hInternal, wInternal, imageRef, rs);

                    sw = AlignState.ReferenceImage;

                    ShowReferenceImage(imageRef, spotRefSim);
                    ShowSampleImage(imageSample, spotRefSim, spotSampleSim);

                    ShowReferenceImage(imageRefTrim, spotRefSimTrim);
                    ShowSampleImage(imageSampleTrim, spotRefSimTrim, spotSampleSimTrim);

                    break;

                case AlignState.ReferenceImage:
                    (spots, numOfSpots, distance) = aligner.ReferenceSpot(imageRefTrim); //zmensit 
                    sw = AlignState.SampleImage;
                    break;

                case AlignState.SampleImage:
                    (sample, bool spotFound) = aligner.SampleSpot(imageSampleTrim,spots, numOfSpots, distance);
                    if (spotFound)
                    {
                        sw = AlignState.AlignZ;
                    }
                    else
                    {
                        sw = AlignState.AlignXY;
                    }
                    break;

                case AlignState.AlignXY:
                    //posun
                    sw = AlignState.SampleImage;
                    break;

                case AlignState.AlignZ:
                    //novy spot
                    //sample = aligner.SampleZAxisDistance(sample, Spot sampleShiftZ, spotRefSim.GetRadius);
                    sw = AlignState.Test;
                    break;

                case AlignState.Test:
                    //vzit v potaz zakazanou oblast a odecist od souradnic
                    Result(sample, spotSampleSim, simulation);
                    endProgram = true;
                    break;
            }
        
        }
        while (!endProgram);



    }
    public static void MainMenu()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{0,-5} {"Simulace"}");
        sb.AppendLine($"{1,-5} {"interferometr"}");

        Console.WriteLine(sb.ToString());
    }
    public static void Result(Spot spot, Spot spotSim, bool simulation)
    {
        StringBuilder sb = new StringBuilder();
        if (simulation && spotSim != null)
        {
            sb.AppendLine($"Simulace:");
            sb.AppendLine($"X: {spotSim.GetCoordX}");
            sb.AppendLine($"Y: {spotSim.GetCoordY}");
            sb.AppendLine($"Z: {spotSim.GetCoordZ}");
            sb.AppendLine($"Radius: {spotSim.GetRadius}");
            sb.AppendLine();
        }

        if (spot != null)
        {
            sb.AppendLine("Real:");
            sb.AppendLine($"X: {spot.GetCoordX}");
            sb.AppendLine($"Y: {spot.GetCoordY}");
            sb.AppendLine($"Z: {spot.GetCoordZ}");
            sb.AppendLine($"Radius: {spot.GetRadius}");
        }
        Console.WriteLine(sb.ToString());
    }

    public static void ShowReferenceImage(Mat imageRef, Spot spotRef)
    {
        using var baseImg = imageRef.Clone();
        using var img = baseImg.Channels() == 1
            ? baseImg.CvtColor(ColorConversionCodes.GRAY2BGR)
            : baseImg.Clone();

        // tečka (ne radius spotu)
        Cv2.Circle(img,
            new Point(spotRef.GetCoordX, spotRef.GetCoordY),
            5,
            new Scalar(0, 0, 255),
            -1); // vyplněné

        using var small = new Mat();
        Cv2.Resize(img, small, new Size(), 0.5, 0.5);

        Cv2.ImShow("Reference", small);
        Cv2.WaitKey();
    }

    public static void ShowSampleImage(Mat imageSample, Spot spotRef, Spot spotSample)
    {
        using var baseImg = imageSample.Clone();
        using var img = baseImg.Channels() == 1
            ? baseImg.CvtColor(ColorConversionCodes.GRAY2BGR)
            : baseImg.Clone();

        // ref - červeně
        Cv2.Circle(img,
            new Point(spotRef.GetCoordX, spotRef.GetCoordY),
            5,
            new Scalar(0, 0, 255),
            -1);

        // sample - modře
        Cv2.Circle(img,
            new Point(spotSample.GetCoordX, spotSample.GetCoordY),
            5,
            new Scalar(255, 0, 0),
            -1);

        using var small = new Mat();
        Cv2.Resize(img, small, new Size(), 0.5, 0.5);

        Cv2.ImShow("Sample", small);
        Cv2.WaitKey();
    }
}