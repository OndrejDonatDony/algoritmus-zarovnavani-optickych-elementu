using AligningOpticalElements;
using OpenCvSharp;
using OpticalElementsSimulator.SimulatorUtils;
using System.Text;
using static OpenCvSharp.ML.DTrees;
using static System.Net.Mime.MediaTypeNames;

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
        Test,
        Error
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
        int rs = 100;
        int ZS = 5;

        //real
        var aligner = new OpticalElementsAligner();

        //Console.WriteLine("nastav posun XY");
        //String shiftCommand = Console.ReadLine();
        //aligner.GetSampleShiftXY = int.Parse(shiftCommand);
        aligner.GetSampleShiftXY = 400;
        //Console.WriteLine("nastav posun Z");
        //String shiftZCommand = Console.ReadLine();
        //aligner.GetSampleShiftZ = int.Parse(shiftZCommand);
        aligner.GetSampleShiftZ = 20;

        //sim
        var sim = new SimulatorUtils();
        sim.GetNoise = noise;
        AlignState sw = AlignState.MainMenu;
        bool endProgram = false;
        bool simulation = false;

        do
        {
            switch (sw)
            {
                case AlignState.MainMenu:
                    MainMenu();
                    //string? choiceCon = Console.ReadLine();
                    //int choice = int.Parse(choiceCon);
                    int choice = 0;
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
                    //ShowReferenceImage(sim.GetImageRef, sim.GetSpotRef);
                    ShowSampleImage(
                        sim.GetImageSample,
                        sim.GetSpotRef,
                        sim.GetSpotSample,
                        sim.GetImageSampleTrim.Width,
                        sim.GetImageSampleTrim.Height);

                    //ShowReferenceImage(sim.GetImageRefTrim, sim.GetSpotRefTrim);
                    ShowSampleImage(
                        sim.GetImageSampleTrim,
                        sim.GetSpotRefTrim,
                        sim.GetSpotSampleTrim,
                        sim.GetImageSampleTrim.Width,
                        sim.GetImageSampleTrim.Height);

                    break;

                case AlignState.ReferenceImage:
                    aligner.ReferenceSpot(sim.GetImageRefTrim);
                    sw = AlignState.SampleImage;
                    break;

                case AlignState.SampleImage:
                    aligner.SampleSpot(sim.GetImageSampleTrim, ZS);
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
                    sim.SimSampleMoveXY(aligner.GetSampleShiftXY,aligner.GetStateOfPosition);
                    sw = AlignState.SampleImage;

                    //ShowReferenceImage(sim.GetImageRef, sim.GetSpotRef);
                    ShowSampleImage(
                        sim.GetImageSample,
                        sim.GetSpotRef,
                        sim.GetSpotSample,
                        sim.GetImageSampleTrim.Width,
                        sim.GetImageSampleTrim.Height);

                    //ShowReferenceImage(sim.GetImageRefTrim, sim.GetSpotRefTrim);
                    ShowSampleImage(
                        sim.GetImageSampleTrim,
                        sim.GetSpotRefTrim,
                        sim.GetSpotSampleTrim,
                        sim.GetImageSampleTrim.Width,
                        sim.GetImageSampleTrim.Height);

                    break;

                case AlignState.AlignZ:
                    //novy spot
                    //sample = aligner.SampleZAxisDistance(sample, Spot sampleShiftZ, spotRefSim.GetRadius);
                    sim.SimSampleMoveZ(ZS);
                    aligner.SampleSpot(sim.GetImageSample, ZS);
                    sw = AlignState.Test;
                    break;

                case AlignState.Test:
                    //vzit v potaz zakazanou oblast a odecist od souradnic
                    Result(aligner.GetSampleSpot, sim.GetSpotSample, simulation,
                        hExternal, wExternal, hInternal, wInternal);
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

    public static void Result(Spot spot, Spot spotSim, bool simulation, int hExternal, int wExternal, int hInternal, int wInternal)               
    {
        StringBuilder sb = new StringBuilder();
        int borderW = (wExternal - wInternal) / 2;
        int borderH = (hExternal - hInternal) / 2;

        if (simulation && spotSim != null)
        {
            sb.AppendLine("Simulace:");
            sb.AppendLine($"X: {((spotSim.GetCoordX - borderW) / 5.248):F4} mm");
            sb.AppendLine($"Y: {((spotSim.GetCoordY - borderH) / 5.248):F4} mm");
            sb.AppendLine($"Z: {spotSim.GetCoordZ:F4} mm");
            sb.AppendLine($"Radius: {spotSim.GetRadius:F4} mm");
            sb.AppendLine();
        }

        if (spot != null)
        {
            sb.AppendLine("Real:");
            sb.AppendLine($"X: {(spot.GetCoordX / 5.248):F4} mm");
            sb.AppendLine($"Y: {(spot.GetCoordY / 5.248):F4} mm");
            sb.AppendLine($"Z: {spot.GetCoordZ:F4} mm");
            sb.AppendLine($"Radius: {spot.GetRadius:F4} mm");
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

    public static void ShowSampleImage(Mat imageSample, Spot spotRef, Spot spotSample, int wInternal, int hInternal)
    {
        using var baseImg = imageSample.Clone();
        using var img = baseImg.Channels() == 1
            ? baseImg.CvtColor(ColorConversionCodes.GRAY2BGR)
            : baseImg.Clone();

        int wExternal = img.Width;
        int hExternal = img.Height;

        int borderW = (wExternal - wInternal) / 2;
        int borderH = (hExternal - hInternal) / 2;

        // border
        Cv2.Rectangle(
            img,
            new Rect(borderW, borderH, wInternal, hInternal),
            new Scalar(0, 255, 0),
            1);

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