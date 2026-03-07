using AligningOpticalElements;
using OpenCvSharp;
using OpticalElementsSimulator.SimulatorUtils;
using static System.Net.Mime.MediaTypeNames;
using System.Text;

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
        //hExternal = 1224;
        //wExternal = 1480;

        int hInternal = 1024;
        int wInternal = 1280;

        int noise = 30;
        int rr = 20;
        int rs = 20;


        //real
        var aligner = new OpticalElementsAligner();

        Console.WriteLine("nastav posun");
        String shiftCommand = Console.ReadLine();
        aligner.GetShift = int.Parse(shiftCommand);

        //sim
        var sim = new SimulatorUtils();

        AlignState sw = AlignState.MainMenu;
        bool endProgram = false;
        bool simulation = false;

        do
        {
            switch (sw)
            {
                case AlignState.MainMenu:
                    MainMenu();
                    string? choiceCon = Console.ReadLine();
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

                    sim.ReferenceImageRand(
                        hExternal, wExternal,
                        hInternal, wInternal,
                        noise, rr);

                    sim.SampleImageRand(
                        hExternal, wExternal,
                        hInternal, wInternal, rs);

                    sw = AlignState.ReferenceImage;

                    //nahled
                    ShowReferenceImage(sim.GetImageRef, sim.GetSpotRef);
                    ShowSampleImage(sim.GetImageSample, sim.GetSpotRef, sim.GetSpotSample);

                    ShowReferenceImage(sim.GetImageRefTrim, sim.GetSpotRefTrim);
                    ShowSampleImage(sim.GetImageSampleTrim, sim.GetSpotRefTrim, sim.GetSpotSampleTrim);

                    break;

                case AlignState.ReferenceImage:
                    aligner.ReferenceSpot(sim.GetImageRefTrim);
                    sw = AlignState.SampleImage;
                    break;

                case AlignState.SampleImage:
                    aligner.SampleSpot(sim.GetImageSampleTrim);
                    if (aligner.GetSampleFound)
                    {
                        sw = AlignState.AlignZ;
                    }
                    else
                    {
                        Console.WriteLine(aligner.SampleMoveXY());
                        sw = AlignState.AlignXY;
                    }
                    break;

                case AlignState.AlignXY:
                    //posun, chovani interferometru vs simulace, mm? pouze img jako vstup, 
                    sim.SimSampleMoveXY(aligner.GetShift,aligner.GetStateOfPosition);
                    sw = AlignState.SampleImage;
                    break;

                case AlignState.AlignZ:
                    //novy spot
                    //sample = aligner.SampleZAxisDistance(sample, Spot sampleShiftZ, spotRefSim.GetRadius);
                    sw = AlignState.Test;
                    break;

                case AlignState.Test:
                    //vzit v potaz zakazanou oblast a odecist od souradnic
                    Result(aligner.GetSampleSpot, sim.GetSpotSample, simulation);
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