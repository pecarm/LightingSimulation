class Led
{
    public string model { get; set; }
    public double intensity { get; set; }
    public List<double> radiationProfile { get; set; }
    double xCoord;
    double yCoord;
    double[] normalVector = new double[3];

    public Led()
    {

    }

    public Led(double intensity, List<double> radiationProfile, string model)
    {
        this.radiationProfile = radiationProfile;
        this.intensity = intensity;
        this.model = model;
    }

    #region Getters, setters
    public double GetXcoord()
    {
        return this.xCoord;
    }

    public void SetXcoord(double xCoord) // for calculating position
    {
        this.xCoord = xCoord;
    }

    public double GetYcoord()
    {
        return this.yCoord;
    }

    public void SetYcoord(double yCoord) // for calculating position
    {
        this.yCoord = yCoord;
    }

    public double[] GetNormalVector()
    {
        return normalVector;
    }

    public void SetNormalVector(double[] normalVector)
    {
        this.normalVector = normalVector;
    }

    public List<double> GetRadiationProfile()
    {
        return this.radiationProfile;
    }

    public double GetIntensity()
    {
        return this.intensity;
    }

    public string GetModel()
    {
        return this.model;
    }
    #endregion
}