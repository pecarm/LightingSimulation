class RingLight
{
    public Led led { get; set; }
    public int numberOfLeds { get; set; }
    public double tilt { get; set; }
    public double radius { get; set; }
    Led[] lights;

    public RingLight()
    {

    }

    public RingLight(Led led, double radius, double tilt, int numberOfLeds)   // ring light source
    {
        this.led = led;
        this.radius = radius;
        this.tilt = tilt;
        this.numberOfLeds = numberOfLeds;

        Init(); // Initializes lights
    }

    public void Init()
    {
        tilt *= (Math.PI / 180);    // Converts to rad

        lights = new Led[numberOfLeds];
        for (int i = 0; i < numberOfLeds; i++)
        {
            Led newLed = new(led.GetIntensity(), led.GetRadiationProfile(), led.GetModel());    // creates a copy of input led with basic properties
            lights[i] = newLed;
        }
    }

    #region Getters, setters
    public Led[] GetLights()
    {
        return this.lights;
    }

    public double GetTilt()
    {
        return this.tilt;
    }

    public double GetRadius()
    {
        return this.radius;
    }
    #endregion
}