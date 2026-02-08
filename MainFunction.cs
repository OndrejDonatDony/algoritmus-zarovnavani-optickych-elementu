public class MainFunction
{
    public static void MainClass(string listPath){

        //nacitani snimku
        List<Image>? images = SpotUtils.ImportImages(listPath);
        if (images == null || images.Count == 0)
        {
            Console.WriteLine("Error: Žádné obrázky.");
            return;
        }
        //referencni bod
        var center = SpotUtils.ReferenceSpot(images[0]);

        Console.WriteLine(
                    $"CenterSpot: x={center.GetCoordX:F4}, " +
                    $"y={center.GetCoordY:F4}, " +        
                    $"r={center.GetRadius:F4}"
                );
        int startingSpots = images[0].GetSpots.Count;
        if (startingSpots == 0)
        {
            Console.WriteLine("Error: prazdny snimek");
            return;
        }
        bool zOperation = false;
        Spot? sampleNoShift = null;

        foreach (Image im in images)
        {

            //vzorek (X,Y + vzdalenost od referencniho bodu) 
            float sampleDistance = 0;
            Spot? sample = null;
            int fault = 0;

            if (im.GetSpots.Count > startingSpots)        
            {
                (sample, sampleDistance) = SpotUtils.SampleDistance(center.GetCoordX, center.GetCoordY, im, images);
            }
            else
            {
                fault += 1;         
                //pokud na prvnim snimku nebude vzorek, zkusí se dalsí. pokud se na dalsim snimku nenalezne vzorek, program se ukonci.
                if(fault == 2)
                {
                    Console.WriteLine("Error: Nepodarilo se nalezt vzorek");
                    return;
                }
                Console.WriteLine("Posun vzorek");
                continue;
            }
       
                
            if (sample != null)
            {
                Console.WriteLine($"vzorek je ve vzdalenosti {sampleDistance:F4}");
                Console.WriteLine(
                "  Spot vzorek: x={0:F4}, y={1:F4}, r={2:F4}",
                sample.GetCoordX,
                sample.GetCoordY,
                sample.GetRadius
                );
            }

            //vzorek (Z)

            if (!zOperation)
            {
                sampleNoShift = sample;
                zOperation = true;
            }
            else
            {
                (Spot? sampleComplete, float zAx) = SpotUtils.SampleZAxisDistance(sampleNoShift, sample, center.GetRadius);
                if (sampleComplete != null)
                {
                    Console.WriteLine($"vzorek je ve vzdalenosti {sampleDistance:F4}");
                    Console.WriteLine(
                    "  Spot vzorek: x={0:F4}, y={1:F4}, r={2:F4}, z={3:F4}",
                    sampleComplete.GetCoordX,
                    sampleComplete.GetCoordY,
                    sampleComplete.GetRadius,
                    zAx
                    );
                   
                }
                else
                {
                    Console.WriteLine("Error: vypocet Z");
                }                
                return;
            }

        }         

    }   
    
}
