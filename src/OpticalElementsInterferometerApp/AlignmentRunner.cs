using AligningOpticalElements;
using OpenCvSharp;
using System.Text;

enum AlignState
{
    Processing,
    PreProcessing,
    Test,  
}
class AlignmentRunner
{  
    static void Main()
    {      
        //real
        var aligner = new OpticalElementsAligner();
        Mat imageToProces = new Mat(); 

        aligner.GetPx = 5.248f;
        aligner.GetSampleShiftXY = 5;
        //aligner.GetSampleShiftXY = (wExternal-wInternal)/2;
        //aligner.GetSampleShiftZ = -sampleShiftZ;
        //sim
        AlignState sw = AlignState.PreProcessing;
        bool endProgram = false;
        int threshold = 1;
        do
        {
            switch (sw)
            {
                case AlignState.PreProcessing:
            
                    aligner.InitSpotMap();
                    sw = AlignState.Processing;
                    break;

                case AlignState.Processing:        
                    //prepisuje se to ?
                    imageToProces = Cv2.ImRead("image.png");

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
                            imageToProces = Cv2.ImRead("image.png");
                            aligner.SampleSpot(imageToProces, threshold);
                            break;

                        case AlignError.SampleOnEdge:
                            Console.WriteLine("vzorek se nachazi na strane snimku");
                            aligner.SampleMoveXY(imageToProces);
                            imageToProces = Cv2.ImRead("image.png");
                            aligner.SampleSpot(imageToProces, threshold);
                            break;

                        case AlignError.NoSampleShift:
                            Console.WriteLine("neni odkaz na puvodni info vzorku");
                            imageToProces = Cv2.ImRead("image.png");
                            aligner.ReferenceSpot(imageToProces, threshold);
                            sw = AlignState.Test;
                            break;

                        case AlignError.MissingZ:
                            Console.WriteLine("neni zmerena hodnota Z");
                            imageToProces = Cv2.ImRead("image.png");
                            aligner.SampleSpot(imageToProces, threshold);
                            aligner.SampleMoveZ(imageToProces);
                            break;

                        case AlignError.Ok:
                            Console.WriteLine("zadne problemy");
                            sw = AlignState.Test;
                            break;
                    }
                    break;

                case AlignState.Test:
                    //vzit v potaz zakazanou oblast a odecist od souradnic
                    endProgram = true;
                    break;
            }

        }
        while (!endProgram);
    }
}