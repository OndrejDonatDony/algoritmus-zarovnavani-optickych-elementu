using OpenCvSharp;
public class Spot
{
    private float coordX;
    private float coordY;
    private float coordZ;
    private float radius;


    // konstruktor
    public Spot(float coordX, float coordY,  float radius, float coordZ)
    {
        this.coordX = coordX;
        this.coordY = coordY;
        this.radius = radius;
        this.coordZ = coordZ;
      
    }

    // getter a setter
    public float GetCoordX
    {
        get { 
            return coordX; 
        }
    }
    public float GetCoordY
    {
        get
        {
            return coordY;
        }
    }
    public float GetCoordZ
    {
        get
        {
            return coordZ;
        }
    }
    public float GetRadius
    {
        get
        {
            return radius;
        }
    }
    
}