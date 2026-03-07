using OpenCvSharp;

namespace AligningOpticalElements;

public class Spot
{
    private int coordX;
    private int coordY;
    private float coordZ;
    private int radius;

    // konstruktor
    public Spot(int coordX, int coordY, int radius, float coordZ)
    {
        this.coordX = coordX;
        this.coordY = coordY;
        this.radius = radius;
        this.coordZ = coordZ;
    }

    // getter a setter
    public int GetCoordX
    {
        get { return coordX; }
        set { coordX = value; }
    }

    public int GetCoordY
    {
        get { return coordY; }
        set { coordY = value; }
    }

    public float GetCoordZ
    {
        get { return coordZ; }
        set { coordZ = value; }
    }

    public int GetRadius
    {
        get { return radius; }
        set { radius = value; }
    }
}