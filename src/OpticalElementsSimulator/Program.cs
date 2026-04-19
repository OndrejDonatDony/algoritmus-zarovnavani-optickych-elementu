using AligningOpticalElements;
using OpenCvSharp;
using OpticalElementsSimulator.SimulatorUtils;
using System.Text;
using static AligningOpticalElements.OpticalElementsAligner;
using static OpenCvSharp.ML.DTrees;
using static System.Net.Mime.MediaTypeNames;

//5.248px = 1mm
//243.9025mm x 195.122mm
//1280x1024 
//2560x2048 or 2240x1792
//487.805mm x 390.244mm or 426.8292mm x 341.4634mm

//819-1639
//r = 1230
public enum AlignState
{
    MainMenu,
    SimulationImages,
    LoadImage,
    Processing,
    PreProcessing,
    AlignXY,
    AlignZ,
    Test,
    Error   
}
class Program
{
    

    static void Main()
    {
        int hExternal = 2048;
        int wExternal = 2560;

        //test
        hExternal = 1224;
        wExternal = 1480;

        int hInternal = 1024;
        int wInternal = 1280;
        //hInternal = 1024/2;
        //wInternal = 1280/2;

        int noise = 30;
        int rr = 20;
        int rs = 30;
        float sampleShiftZ = 20;
        int threshold = 0;
        int shiftCoordXY = (wExternal-wInternal)/2;

        //real
        var aligner = new OpticalElementsAligner();
        Mat imageToProces = new Mat(); 

        aligner.GetPx = 5.248f;
        aligner.GetSampleShiftXY = shiftCoordXY;
        //Console.WriteLine("nastav posun XY");
        //String shiftCommand = Console.ReadLine();
        //aligner.GetSampleShiftXY = int.Parse(shiftCommand);
        aligner.GetSampleShiftXY = (wExternal-wInternal)/2;
        aligner.GetSampleShiftZ = -sampleShiftZ;
        //Console.WriteLine("nastav posun Z");
        //String shiftZCommand = Console.ReadLine();
        //aligner.GetSampleShiftZ = int.Parse(shiftZCommand);

        //sim
        var sim = new SimulatorUtils();
        AlignState sw = AlignState.MainMenu;
        bool endProgram = false;
        bool simulation = false;

        bool firstTime = true;

        int choice = 0;
        do
        {
            switch (sw)
            {
                case AlignState.MainMenu:
                    MainMenu();
                    //string? choiceCon = Console.ReadLine();
                    //int choice = int.Parse(choiceCon);
                    
                    switch (choice)
                    {
                        case 0:
                            sw = AlignState.SimulationImages;
                            Console.WriteLine("simulace spustena");
                            break;
                        case 1:
                            sw = AlignState.LoadImage;
                            Console.WriteLine("interferometr spusten");
                            break;

                        default:
                            Console.WriteLine("spatna volba");
                            break;
                    }
                    break;

                case AlignState.SimulationImages:
                    simulation = true;

                    sim.ImageRandGenerator(
                        hExternal, wExternal,
                        hInternal, wInternal,
                        noise, rr);

                    sw = AlignState.PreProcessing;
                    break;
                case AlignState.PreProcessing:
                   
                    aligner.InitSpotMap();
                    sw = AlignState.Processing;
                    break;
             
                case AlignState.Processing:

                    ShowSampleImage(
                        sim.GetImages[ImageKey.SampleImage],
                        sim.GetSpots[SpotKey.RefSpot],
                        sim.GetSpots[SpotKey.SampleSpot],
                        sim.GetImages[ImageKey.SampleImageTrim].Width,
                        sim.GetImages[ImageKey.SampleImageTrim].Height);

                    ShowSampleImage(
                        sim.GetImages[ImageKey.SampleImageTrim],
                        sim.GetSpots[SpotKey.RefSpotTrim],
                        sim.GetSpots[SpotKey.SampleSpotTrim],
                        sim.GetImages[ImageKey.SampleImageTrim].Width,
                        sim.GetImages[ImageKey.SampleImageTrim].Height);

                    if (choice == 0)
                    {
                        //prepisuje se to ?
                        imageToProces = sim.GetImages[ImageKey.SampleImageTrim];
                    }
                    
                    switch (aligner.GetEvaluation)
                    {
                        case AlignError.NoSpots:
                            Console.WriteLine("zadne body");
                            aligner.ReferenceSpot(imageToProces, threshold);
                            break;

                        case AlignError.TooManySpots:
                            Console.WriteLine("prilis mnoho bodu v snimku");
                            aligner.ReferenceSpot(imageToProces, threshold);
                            break;

                        case AlignError.SampleNotFound:
                            Console.WriteLine("nenasel se vzorek yy");
                            aligner.SampleMoveXY(imageToProces);
                            sim.SimSampleMoveXY(aligner.GetSampleShiftXY, aligner.GetStateOfPosition, aligner.GetSpotOnBorder());

                            aligner.SampleSpot(imageToProces, threshold);
                            break;

                        case AlignError.SampleOnEdge:
                            Console.WriteLine("vzorek se nachazi na strane snimku");
                            aligner.SampleMoveXY(imageToProces);
                            sim.SimSampleMoveXY(aligner.GetSampleShiftXY, aligner.GetStateOfPosition, aligner.GetSpotOnBorder());
                            aligner.SampleSpot(imageToProces, threshold);
                            break;
                        case AlignError.NoSampleShift:
                            Console.WriteLine("neni odkaz na puvodni info vzorku");
                            sim.SimSampleMoveXY(aligner.GetSampleShiftXY, aligner.GetStateOfPosition, aligner.GetSpotOnBorder());
                            aligner.ReferenceSpot(imageToProces, threshold);
                            sw = AlignState.Test;
                            break;
                        case AlignError.MissingZ:
                            Console.WriteLine("neni zmerena hodnota Z");
                            sim.SimSampleMoveZ(aligner.GetSampleShiftZ);
                            aligner.SampleSpot(imageToProces, threshold);
                            aligner.SampleMoveZ(imageToProces);
                            break;

                        case AlignError.Ok:
                            Console.WriteLine("zadne problemy");
                            sw = AlignState.Test;
                            break;
                    }
                    break;

                case AlignState.AlignXY:
                    //posun, chovani interferometru vs simulace, mm? pouze img jako vstup, 
                    sw = AlignState.Processing;

                    //ShowReferenceImage(sim.GetImageRef, sim.GetSpotRef);
                    ShowSampleImage(
                          sim.GetImages[ImageKey.SampleImage],
                          sim.GetSpots[SpotKey.RefSpot],
                          sim.GetSpots[SpotKey.SampleSpot],
                          sim.GetImages[ImageKey.SampleImageTrim].Width,
                          sim.GetImages[ImageKey.SampleImageTrim].Height);

                    break;

                case AlignState.AlignZ:

                    ShowSampleImage(
                          sim.GetImages[ImageKey.SampleImage],
                          sim.GetSpots[SpotKey.RefSpot],
                          sim.GetSpots[SpotKey.SampleSpot],
                          sim.GetImages[ImageKey.SampleImageTrim].Width,
                          sim.GetImages[ImageKey.SampleImageTrim].Height);
                   
                    break;

                case AlignState.Test:
                    //vzit v potaz zakazanou oblast a odecist od souradnic
                    Result(aligner.GetSampleSpot, sim.GetSpots[SpotKey.SampleSpot], aligner.GetPx, simulation,
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

    public static void Result(Spot spot, Spot spotSim, float px, bool simulation, int hExternal, int wExternal, int hInternal, int wInternal)               
    {
        StringBuilder sb = new StringBuilder();
        int borderW = (wExternal - wInternal) / 2;
        int borderH = (hExternal - hInternal) / 2;

        if (simulation && spotSim != null)
        {
            sb.AppendLine("Simulace:");
            sb.AppendLine($"X: {((spotSim.GetCoordX - borderW) / px):F4} mm");
            sb.AppendLine($"Y: {((spotSim.GetCoordY - borderH) / px):F4} mm");
            sb.AppendLine($"Z: {spotSim.GetCoordZ:F4} mm");
            sb.AppendLine($"Průměr: {spotSim.GetRadius*2/px:F4} mm");
            sb.AppendLine();
        }
        if (spot != null)
        {
            sb.AppendLine("Real:");
            sb.AppendLine($"X: {(spot.GetCoordX / px):F4} mm");
            sb.AppendLine($"Y: {(spot.GetCoordY / px):F4} mm");
            sb.AppendLine($"Z: {spot.GetCoordZ:F4} mm");
            sb.AppendLine($"Průměr: {spot.GetRadius*2/px:F4} mm");
        }
        else
        {
            Console.WriteLine("aligner selhal");
        }
            Console.WriteLine(sb.ToString());
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
        Cv2.Resize(img, small, new Size(), 0.4, 0.4);

        Cv2.ImShow("Sample", small);
        Cv2.WaitKey();
    }
}