using AligningOpticalElements;
using OpenCvSharp;
using OpticalElementsSimulator;
using OpticalElementsSimulator.model;
using System.Text;

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
public class SimulatorRunner
{
    public SimulatorRunResult Run(SimulationSettings settings)
    {

        float sampleShiftZ = 20;
        int threshold = 0;
        int shiftCoordXY = (settings.WidthExternal - settings.WidthInternal) / 2;

        var aligner = new OpticalElementsAligner();
        Mat imageToProces = new Mat();

        aligner.GetPx = 5.248f;
        aligner.GetSampleShiftXY = shiftCoordXY;
        aligner.GetSampleShiftZ = -sampleShiftZ;

        var sim = new SimulatorUtils();
        sim.SetSeed(settings.Seed);

        AlignState sw = AlignState.SimulationImages;
        bool endProgram = false;
        bool simulation = false;

        Mat previewFull = new Mat();
        Mat previewTrim = new Mat();
        string resultText = "";

        do
        {
            switch (sw)
            {
                case AlignState.SimulationImages:
                    simulation = true;

                    sim.ImageRandGenerator(
                        settings.HeightExternal, settings.WidthExternal,
                        settings.HeightInternal, settings.WidthInternal,
                        settings.Noise, settings.RefRadius);

                    sw = AlignState.PreProcessing;
                    break;

                case AlignState.PreProcessing:
                    aligner.InitSpotMap();
                    sw = AlignState.Processing;
                    break;

                case AlignState.Processing:
                    previewFull = DrawSampleImage(
                        sim.GetImages[ImageKey.SampleImage],
                        sim.GetSpots[SpotKey.RefSpot],
                        sim.GetSpots[SpotKey.SampleSpot],
                        sim.GetImages[ImageKey.SampleImageTrim].Width,
                        sim.GetImages[ImageKey.SampleImageTrim].Height);

                    previewTrim = DrawSampleImage(
                        sim.GetImages[ImageKey.SampleImageTrim],
                        sim.GetSpots[SpotKey.RefSpotTrim],
                        sim.GetSpots[SpotKey.SampleSpotTrim],
                        sim.GetImages[ImageKey.SampleImageTrim].Width,
                        sim.GetImages[ImageKey.SampleImageTrim].Height);

                    imageToProces = sim.GetImages[ImageKey.SampleImageTrim];

                    switch (aligner.GetEvaluation)
                    {
                        case AlignError.NoSpots:
                            aligner.ReferenceSpot(imageToProces, threshold);
                            break;

                        case AlignError.TooManySpots:
                            aligner.ReferenceSpot(imageToProces, threshold);
                            break;

                        case AlignError.SampleNotFound:
                            aligner.SampleMoveXY(imageToProces);
                            sim.SimSampleMoveXY(
                                aligner.GetSampleShiftXY,
                                aligner.GetStateOfPosition,
                                aligner.GetSpotOnBorder());

                            aligner.SampleSpot(imageToProces, threshold);
                            break;

                        case AlignError.SampleOnEdge:
                            aligner.SampleMoveXY(imageToProces);
                            sim.SimSampleMoveXY(
                                aligner.GetSampleShiftXY,
                                aligner.GetStateOfPosition,
                                aligner.GetSpotOnBorder());

                            aligner.SampleSpot(imageToProces, threshold);
                            break;

                        case AlignError.NoSampleShift:
                            sim.SimSampleMoveXY(
                                aligner.GetSampleShiftXY,
                                aligner.GetStateOfPosition,
                                aligner.GetSpotOnBorder());

                            aligner.ReferenceSpot(imageToProces, threshold);
                            sw = AlignState.Test;
                            break;

                        case AlignError.MissingZ:
                            sim.SimSampleMoveZ(aligner.GetSampleShiftZ);
                            aligner.SampleSpot(imageToProces, threshold);
                            aligner.SampleMoveZ(imageToProces);
                            break;

                        case AlignError.Ok:
                            sw = AlignState.Test;
                            break;
                    }
                    break;

                case AlignState.Test:
                    resultText = Result(
                        aligner.GetSampleSpot,
                        sim.GetSpots[SpotKey.SampleSpot],
                        aligner.GetPx,
                        simulation,
                        settings.HeightInternal,
                        settings.WidthExternal,
                        settings.HeightInternal,
                        settings.WidthInternal);

                    endProgram = true;
                    break;
            }
        }
        while (!endProgram);

        return new SimulatorRunResult
        {
            PreviewFull = previewFull,
            PreviewTrim = previewTrim,
            ResultText = resultText
        };
    }

    public static string Result(Spot spot, Spot spotSim, float px, bool simulation, int hExternal, int wExternal, int hInternal, int wInternal)
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
            sb.AppendLine($"Průměr: {spotSim.GetRadius * 2 / px:F4} mm");
            sb.AppendLine();
        }

        if (spot != null)
        {
            sb.AppendLine("Real:");
            sb.AppendLine($"X: {(spot.GetCoordX / px):F4} mm");
            sb.AppendLine($"Y: {(spot.GetCoordY / px):F4} mm");
            sb.AppendLine($"Z: {spot.GetCoordZ:F4} mm");
            sb.AppendLine($"Průměr: {spot.GetRadius * 2 / px:F4} mm");
        }
        else
        {
            sb.AppendLine("aligner selhal");
        }

        return sb.ToString();
    }

    public static Mat DrawSampleImage(Mat imageSample, Spot spotRef, Spot spotSample, int wInternal, int hInternal)
    {
        var baseImg = imageSample.Clone();
        var img = baseImg.Channels() == 1
            ? baseImg.CvtColor(ColorConversionCodes.GRAY2BGR)
            : baseImg.Clone();

        int wExternal = img.Width;
        int hExternal = img.Height;

        int borderW = (wExternal - wInternal) / 2;
        int borderH = (hExternal - hInternal) / 2;

        Cv2.Rectangle(
            img,
            new Rect(borderW, borderH, wInternal, hInternal),
            new Scalar(0, 255, 0),
            1);

        Cv2.Circle(
            img,
            new Point(spotRef.GetCoordX, spotRef.GetCoordY),
            5,
            new Scalar(0, 0, 255),
            -1);

        Cv2.Circle(
            img,
            new Point(spotSample.GetCoordX, spotSample.GetCoordY),
            5,
            new Scalar(255, 0, 0),
            -1);

        baseImg.Dispose();
        return img;
    }
}

public class SimulatorRunResult
{
    public Mat PreviewFull { get; set; }
    public Mat PreviewTrim { get; set; }
    public string ResultText { get; set; }
}