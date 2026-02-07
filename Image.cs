public class Image
{

    private string image;
    private List<Spot> spots;

    // konstruktor
    public Image(List<Spot> spots, string image)
    {
        this.spots = spots;
        this.image = image;
    }

    // getter a setter
    public string GetImage
    {
        get
        {
            return image;
        }
    }
    public List<Spot> GetSpots {
        get
        {
            return spots;
        }
    }

}

