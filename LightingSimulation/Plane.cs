using System.Drawing;

class Plane
{
    public int xDim { get; set; }
    public int yDim { get; set; }
    public double fov { get; set; }
    public double distance { get; set; }
    Pixel[] pixels;
    double pixelSize;

    public Plane()
    {

    }

    public Plane(int xDim, int yDim, double distance, double fov)
    {
        this.xDim = xDim;
        this.yDim = yDim;
        this.fov = fov;
        this.distance = distance;

        Init(); // Initializes pixels
    }

    public void Init()
    {
        fov *= (Math.PI / 180); // Converts to rad

        double xSize = 2 * distance * Math.Tan(fov / 2);    // Actual size of captured canvas
        pixelSize = xSize / xDim;                           // Actual size of one pixel captured on that canvas, calculated upfront so it isn't calculated for every pixel

        pixels = new Pixel[xDim * yDim];
        for (int y = 0; y < yDim; y++)
        {
            for (int x = 0; x < xDim; x++)
            {
                Pixel pixel = new Pixel();

                CalculatePixelCoords(pixel, x, y);

                pixels[x + y * xDim] = pixel; // indexed by ROWS, top-left to down-right
            }
        }
    }

    void CalculatePixelCoords(Pixel pixel, int x, int y)
    {
        double xCoord = (x - (xDim / 2) + 0.5) * pixelSize; // + 0.5 is because we are counting the coords of the middle of the pixel, also converts the first bracket to double
        double yCoord = (y - (yDim / 2) + 0.5) * pixelSize;

        pixel.SetXcoord(xCoord);
        pixel.SetYcoord(yCoord);
    }

    public void ExportAsBitmap()
    {
        Console.WriteLine("Exporting simulation...");
        double pixelArea = pixelSize * pixelSize;

        // for normalizing pixel values 
        double minIntensity = 1000;
        double maxIntensity = 0;

        foreach (Pixel pixel in pixels) // finds maximum and minimum intensity in one pass of all pixels
        {
            if (pixel.GetIllumination() > maxIntensity)
            {
                maxIntensity = pixel.GetIllumination();
            }

            if (pixel.GetIllumination() < minIntensity)
            {
                minIntensity = pixel.GetIllumination();
            }
        }

        maxIntensity = maxIntensity / pixelArea; // W/m2
        minIntensity  = minIntensity / pixelArea; // W/m2

        double delta = maxIntensity - minIntensity;

        Bitmap bmp = new Bitmap(xDim, yDim);

        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % xDim; // x coord in bmp can be calculated as i mod width in pixels
            int y = i / xDim; // c# int division automatically floors

            int brightness = (int) Math.Round(((pixels[i].GetIllumination() / pixelArea - minIntensity) / delta) * 255);
            /*
            int brightness = (int)Math.Round(((pixels[i].GetIllumination() / pixelArea) / maxIntensity) * 255);
            Alternative for actual brightness, not just relative. Might show better comparison results...
            */
            Color color = Color.FromArgb(brightness, brightness, brightness);

            bmp.SetPixel(x, y, color);
        }

        // string path = "C:\\Users\\Martin\\Desktop\\docs\\DIPLOMKA\\SIM RESULTS";
        string fileName = /*path + "\\" + */ DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".bmp";

        bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Bmp);
    }

    #region Getters, setters
    public Pixel[] GetPixels()
    {
        return this.pixels;
    }

    public double GetDistance()
    {
        return this.distance;
    }

    public double GetPixelSize()
    {
        return this.pixelSize;
    }

    public int GetXdim()
    {
        return xDim;
    }

    public int GetYdim()
    {
        return yDim;
    }
    #endregion
}