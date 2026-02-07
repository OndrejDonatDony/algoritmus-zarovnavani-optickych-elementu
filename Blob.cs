using OpenCvSharp;

public class Blob
{
    private Point2f c;
    private float r;

    public Blob(Point2f c, float r)
    {
        this.c = c;
        this.r = r;
    }
    public Point2f C {
        get 
        {  
            return c; 
        } 
    }
    public float R 
    {
        get 
        {
            return r;
        }
    }

}

