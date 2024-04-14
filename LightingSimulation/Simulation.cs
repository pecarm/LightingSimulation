using System.Diagnostics;

class Simulation
{
    public Plane plane { get; set; }
    public LightSource lightSource { get; set; }

    public Simulation()
    {

    }

    public Simulation(Plane plane, LightSource lightSource)
    {
        this.plane = plane;
        this.lightSource = lightSource;
    }

    public void Init()
    {
        plane.Init();
        lightSource.Init();
    }

    #region Full res calculation
    public void CalculateIllumination()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        Console.WriteLine("Calculating Illumination..."); 
        foreach (Pixel pixel in plane.GetPixels())
        {
            CalculatePixel(pixel);
        }
        sw.Stop();
        Console.WriteLine("Elapsed: " + sw.Elapsed);

        plane.ExportAsBitmap();
        GenerateLog();
    }

    void CalculatePixel(Pixel pixel) // Calculates power of light hitting a single pixel
    {
        double halfPixelSize = plane.GetPixelSize() / 2;
        double x0 = pixel.GetXcoord() - halfPixelSize;
        double x1 = pixel.GetXcoord() + halfPixelSize;

        double y0 = pixel.GetYcoord() - halfPixelSize;
        double y1 = pixel.GetYcoord() + halfPixelSize;

        double illumination = CalculateAreaIllumination(x0, x1, y0, y1);

        pixel.SetIllumination(illumination);
    }
    #endregion

    #region Preview calculation
    public void CalculateIlluminationPreview(int clusterSize) // calculates clusters of pixels, giving smaller resolution, but way faster compute times
    {
        if ((plane.GetXdim() % clusterSize != 0) || (plane.GetYdim() % clusterSize != 0))
        {
            Console.WriteLine("ERROR: Cluster size incompatible with camera resolution");
            return;
        }

        Stopwatch sw = new Stopwatch();
        sw.Start();
        Console.WriteLine("Calculating Illumination preview...");
        for (int y = 0; y < plane.GetYdim() / clusterSize; y++)
        {
            for (int x = 0; x < plane.GetXdim() / clusterSize; x++)
            {
                CalculateCluster(x, y, clusterSize); // just iterates through clusters
            }
        }
        sw.Stop();
        Console.WriteLine("Elapsed: " + sw.Elapsed);

        plane.ExportAsBitmap();
        GenerateLog();
    }

    void CalculateCluster(int x, int y, int clusterSize) // Calculates power of light hitting a 10x10 cluster of pixels, x and y are coords of cluster on a Plane
    {
        int xDim = plane.GetXdim();
        double halfPixelSize = plane.GetPixelSize() / 2;

        // calculate boundaries
        double x0 = plane.GetPixels()[x * clusterSize].GetXcoord() - halfPixelSize;
        double x1 = plane.GetPixels()[x * clusterSize + clusterSize - 1].GetXcoord() + halfPixelSize;

        double y0 = plane.GetPixels()[y * clusterSize * xDim].GetYcoord() - halfPixelSize;
        double y1 = plane.GetPixels()[(y * clusterSize + clusterSize - 1)* xDim].GetYcoord() + halfPixelSize;

        // calculate illumination of cluster
        double illumination = CalculateAreaIllumination(x0, x1, y0, y1);

        // add contribution to each pixel of cluster
        for (int yDelta = 0; yDelta < clusterSize; yDelta++)
        {
            for (int xDelta = 0; xDelta < clusterSize; xDelta++)
            {
                plane.GetPixels()[(x * clusterSize + xDelta) + (y * clusterSize + yDelta) * xDim].SetIllumination(illumination / (clusterSize * clusterSize));
            }
        }
    }
    #endregion

    #region Universal methods for calculating illumination
    double CalculateAreaIllumination(double x0, double x1, double y0, double y1) // calculates how much light falls onto a rectangular area (pixel, cluster)
    {
        double illumination = 0;

        foreach (Led led in lightSource.GetLights()) // iterates through LEDs in lightsource
        {
            illumination += CalculateLedContribution(CalculateSolidAngle(x0, x1, y0, y1, led), CalculateAngle((x0 + x1) / 2, (y0 + y1) / 2, led), led);
        }

        return illumination;
    }

    double CalculateLedContribution(double solidAngle, double angle, Led led) // calculates light contribution by a single LED, adjusts for angle and solid angle
    {
        double contribution = AdjustedIntensity(led, angle) * solidAngle; // result in watts

        return contribution;
    }

    double CalculateAngle(double x, double y, Led led) // Calculates angle between vector normal and vector LED -> point on a Plane
    {
        // LED normal vector is calculated when a light source is created
        double[] pixelVector = CalculatePixelVector(led, x, y); // vector LED -> pixel

        double angle = Math.Acos(VectorDotProduct(led.GetNormalVector(), pixelVector) / (VectorMagnitude(led.GetNormalVector()) * VectorMagnitude(pixelVector)));

        return angle;
    }

    double AdjustedIntensity(Led led, double angle) // adjusts intensity of light from LED based on angle
    {
        // LED radiation profile is in increments of 5°
        // index 0 -> 0°
        // index 18 -> 90°
        // this function does a linear approximation between these two

        double angleDegrees = angle * 180 / Math.PI;

        if (angleDegrees > 90) // outside of LED FOV, who knows, might happen... >90°
        {
            return 0;
        }

        int index = (int)Math.Floor(angleDegrees / 5);

        double upperProportion = (angleDegrees % 5) / 5;
        double lowerProportion = 1 - upperProportion;

        double intensity = (lowerProportion * led.GetRadiationProfile()[index] + upperProportion * led.GetRadiationProfile()[index + 1]) * led.GetIntensity();

        return intensity;
    }

    double CalculateSolidAngle(double x0, double x1, double y0, double y1, Led led) // x and y ranges of illuminated area
    {
        double[] vecA = CalculatePixelVector(led, x0, y0);
        double[] vecB = CalculatePixelVector(led, x1, y0);
        double[] vecC = CalculatePixelVector(led, x0, y1);
        double[] vecD = CalculatePixelVector(led, x1, y1);
        
        double magA = VectorMagnitude(vecA); // for easier readability, somewhat
        double magB = VectorMagnitude(vecB);
        double magC = VectorMagnitude(vecC);
        double magD = VectorMagnitude(vecD);

        double tetrahedronA = 2 * Math.Atan( Math.Abs(VectorDotProduct(vecA, VectorCrossProduct(vecB, vecC))) /
            ( magA * magB * magC + VectorDotProduct(vecA, vecB) * magC + VectorDotProduct(vecA, vecC) * magB + VectorDotProduct(vecB, vecC) * magA)); //thA: points ABC
        double tetrahedronB = 2 * Math.Atan( Math.Abs(VectorDotProduct(vecA, VectorCrossProduct(vecC, vecD))) /
            (magA * magC * magD + VectorDotProduct(vecA, vecC) * magD + VectorDotProduct(vecA, vecD) * magC + VectorDotProduct(vecC, vecD) * magA)); //thB: points ACD

        /*
        Can be implemented as 2*A, this is a test that shows the relative error is 0,01%

        double a2 = tetrahedronA * 2;
        double ab = tetrahedronA + tetrahedronB;
        Console.WriteLine("A*2: " + a2);
        Console.WriteLine("A+B: " + ab);

        Console.WriteLine("Delta: " + (a2 - ab));

        Console.WriteLine("RelDelta%: " + (100*(a2 - ab) / ab));
        */

        return tetrahedronA + tetrahedronB;
    }

    double[] CalculatePixelVector(Led led, double x, double y) // Calculates vector between LED and a point on a Plane
    {
        return new double[] { x - led.GetXcoord(), y - led.GetYcoord(), plane.GetDistance() };
    }
    #endregion

    #region Vector operations
    double VectorMagnitude(double[] vector)
    {
        return Math.Sqrt(vector[0]* vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
    }

    double VectorDotProduct(double[] a, double[] b)
    {
        return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    }

    double[] VectorCrossProduct(double[] a, double[] b)
    {
        return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
    }
    #endregion

    public void GenerateLog()
    {
        string dateTime = DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + "\n";

        double totalPower = 0;
        foreach(Pixel pixel in plane.GetPixels())
        {
            totalPower += pixel.GetIllumination();
        }

        string irradiance = "Average irradiance of image: " + totalPower / (plane.GetXdim() * plane.GetYdim() * Math.Pow(plane.GetPixelSize(), 2)) + " W/m2\n\n";


        //
        Dictionary<string, string> ledProperties = new Dictionary<string, string>();

        foreach (RingLight ringLight in lightSource.GetRingLights())
        {
            string model = ringLight.GetLights()[0].GetModel();
            string properties = "Intensity: " + ringLight.GetLights()[0].GetIntensity() + " W/m2\n"
                + "Tilt: " + ringLight.GetTilt() * (180 / Math.PI) + "°\n"
                + "Radius: " + ringLight.GetRadius() + "\n"
                + "Number of LEDs: " + ringLight.GetLights().Length;
            ledProperties.TryAdd(properties, model);
        }

        string lightSourceInfo = "";
        foreach (KeyValuePair<string, string> ledProperty in ledProperties)
        {
            string ledInfo = "LED model name: " + ledProperty.Value + "\n"
                + ledProperty.Key + "\n\n"; // properties are stored in key, because they are unique
            lightSourceInfo += ledInfo;
        }

        string log = dateTime + irradiance + lightSourceInfo;
        string path = "C:\\Users\\Martin\\Desktop\\docs\\DIPLOMKA\\SIM RESULTS";
        string fileName = path + "\\log-" + DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".txt";

        using (StreamWriter sw = new StreamWriter(fileName))
        {
            sw.WriteLine(log);
        }
    }
}