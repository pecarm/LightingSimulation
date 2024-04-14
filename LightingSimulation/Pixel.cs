class Pixel
{
    double xCoord;       // Real coords in space
    double yCoord;
    double illumination; // Amount of light falling on this pixel

    #region Getters, setters
    public double GetIllumination() // for exporting as BMP
    {
        return illumination;
    }

    public void SetIllumination(double illumination)
    {
        this.illumination = illumination;
    }

    public double GetXcoord()
    {
        return this.xCoord;
    }

    public void SetXcoord(double xCoord)
    {
        this.xCoord = xCoord;
    }

    public double GetYcoord()
    {
        return this.yCoord;
    }

    public void SetYcoord(double yCoord)
    {
        this.yCoord = yCoord;
    }
    #endregion
}