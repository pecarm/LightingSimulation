using System.Diagnostics;

class Simulation : IDisposable
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
    public void CalculateIllumination(bool includeGraphics)
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

        if (includeGraphics)
        {
            plane.ExportAsBitmap();
        }
        GenerateLog();
    }

    void CalculatePixel(Pixel pixel) // Calculates power of light hitting a single pixel
    {
        double halfPixelSize = plane.GetPixelSize() / 2;
        double x0 = pixel.GetXcoord() - halfPixelSize;
        double x1 = pixel.GetXcoord() + halfPixelSize;

        double y0 = pixel.GetYcoord() - halfPixelSize;
        double y1 = pixel.GetYcoord() + halfPixelSize;

        double[] results = CalculateAreaIllumination(x0, x1, y0, y1);

        pixel.SetIllumination(results[0]);
        pixel.SetAverageAngleOfIncidence(results[1]);
    }
    #endregion

    #region Preview calculation
    public void CalculateIlluminationPreview(int clusterSize, bool includeGraphics) // calculates clusters of pixels, giving smaller resolution, but way faster compute times
    {
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

        if (includeGraphics)
        {
            plane.ExportAsBitmap();
        }
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
        double[] results = CalculateAreaIllumination(x0, x1, y0, y1);

        // add contribution to each pixel of cluster
        for (int yDelta = 0; yDelta < clusterSize; yDelta++)
        {
            for (int xDelta = 0; xDelta < clusterSize; xDelta++)
            {
                plane.GetPixels()[(x * clusterSize + xDelta) + (y * clusterSize + yDelta) * xDim].SetIllumination(results[0] / (clusterSize * clusterSize));
                plane.GetPixels()[(x * clusterSize + xDelta) + (y * clusterSize + yDelta) * xDim].SetAverageAngleOfIncidence(results[1]);
            }
        }
    }
    #endregion

    #region Universal methods for calculating illumination
    double[] CalculateAreaIllumination(double x0, double x1, double y0, double y1) // calculates how much light falls onto a rectangular area (pixel, cluster)
    {
        List<double[]> pixelVectors = new List<double[]>();

        double illumination = 0;

        foreach (Led led in lightSource.GetLights()) // iterates through LEDs in lightsource
        {
            double[] pixelVector = CalculatePixelVector(led, (x0 + x1) / 2, (y0 + y1) / 2);

            double contribution = CalculateLedContribution(CalculateSolidAngle(x0, x1, y0, y1, led), CalculateAngle(pixelVector, led.GetNormalVector()), led);

            illumination += contribution;

            for (int i = 0; i < 3; i++)
            {
                pixelVector[i] *= contribution; // creates a vector, whose length is proportional to the light contribution from that direction
            }

            pixelVectors.Add(pixelVector);
        }

        return [illumination, CalculateAverageAngle(pixelVectors)];
    }

    double CalculateLedContribution(double solidAngle, double angle, Led led) // calculates light contribution by a single LED, adjusts for angle and solid angle
    {
        double contribution = AdjustedIntensity(led, angle) * solidAngle; // result in watts

        return contribution;
    }

    double CalculateAngle(double[] pixelVector, double[] ledVector) // Calculates angle between vector normal and vector LED -> point on a Plane
    {
        // LED normal vector is calculated when a light source is created
        double angle = Math.Acos(VectorDotProduct(ledVector, pixelVector) / (VectorMagnitude(ledVector) * VectorMagnitude(pixelVector)));

        return angle;
    }

    double CalculateAverageAngle(List<double[]> pixelVectors)
    {
        // Calculate sum of vectors, this way is best because it accounts for provided amount of light from each direction
        // doesn't need to be averaged, because the angle of vector a is the same as the vector 10*a
        double[] vectorSum = new double[3];
        foreach (double[] pixelVector in pixelVectors)
        {
            for (int i = 0; i < 3; i++)
            {
                vectorSum[i] += pixelVector[i];  
            }
        }

        return Math.Asin(vectorSum[2] / VectorMagnitude(vectorSum)) * (180 / Math.PI); // RESULT IN DEGREES
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

    double[] CalculatePixelVector(Led led, double x, double y) // Calculates a NORMALIZED vector between LED and a point on a Plane
    {
        double i = x - led.GetXcoord();
        double j = y - led.GetYcoord();
        double k = plane.GetDistance();

        double magnitude = VectorMagnitude([i, j, k]);

        return [i / magnitude, j / magnitude, k / magnitude];
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

    double[] VectorAverage()
    {
        double[] average = new double[3];

        return average;
    }
    #endregion

    public void GenerateLog()
    {
        string dateTime = DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss");

        double totalPower = 0;
        double worstAngleOfIncidence = 90;
        foreach(Pixel pixel in plane.GetPixels())
        {
            totalPower += pixel.GetIllumination();
            if (pixel.GetAverageAngleOfIncidence() < worstAngleOfIncidence)
            {
                worstAngleOfIncidence = pixel.GetAverageAngleOfIncidence();
            }
        }

        double pixelArea = Math.Pow(plane.GetPixelSize(), 2);
        double totalArea = plane.GetXdim() * plane.GetYdim() * pixelArea;
        double avIrradiance = totalPower / totalArea;

        
        double sDevSubtotal = 0;
        foreach (Pixel pixel in plane.GetPixels())
        {
            sDevSubtotal += Math.Pow(pixel.GetIllumination() / pixelArea - avIrradiance, 2);
        }
        double sDev = Math.Sqrt(sDevSubtotal / (plane.GetXdim() * plane.GetYdim()));

        string distance = "Distance of simulated plane from light source: " + plane.GetDistance() * 100 + " cm\n";
        string irradiance = "Average irradiance of image: " + avIrradiance + " W/m2\n";
        string standardDeviation = "Standard deviation: " + sDev + " W/m2\n";
        string cv = "Coefficient of variation: " + 100 * sDev / avIrradiance + " %\n";
        string worstAngle = "Worst angle of incidence: " + (90 - worstAngleOfIncidence) + "°\n\n";

        Dictionary<string, string> ledProperties = new Dictionary<string, string>();

        foreach (RingLight ringLight in lightSource.GetRingLights())
        {
            string model = ringLight.GetLights()[0].GetModel();
            string properties = "Radiant intensity: " + ringLight.GetLights()[0].GetIntensity() + " W/sr\n"
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

        string log = dateTime + "\n" + distance + irradiance + standardDeviation + cv + worstAngle + lightSourceInfo;
        // string path = "C:\\Users\\Martin\\Desktop\\docs\\DIPLOMKA\\SIM RESULTS";
        string fileName = /*path + "\\" +*/"log-" + dateTime + ".txt";

        using (StreamWriter sw = new StreamWriter(fileName))
        {
            sw.WriteLine(log);
        }
    }

    public void Dispose()
    {
        plane = null;
        lightSource = null;
    }
}