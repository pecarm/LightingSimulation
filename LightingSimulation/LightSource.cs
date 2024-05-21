using System.Xml;

class LightSource
{
    public List<RingLight> ringLights { get; set; }    // for creating report and calculating geometry
    Led[] lights;                               // for easy foreaching of LED contributions

    public LightSource()
    {

    }

    public LightSource(List<RingLight> ringLights)
    {
        this.ringLights = ringLights;

        Init();
    }

    public void Init()
    {
        foreach (RingLight ringLight in ringLights)
        {
            ringLight.Init();
        }

        CalculateSourceGeometry();

        lights = CreateLedArray();
    }

    #region Calculating LightSource geometry
    void CalculateSourceGeometry()
    {
        double[] radii = new double[ringLights.Count];

        for (int i = 0; i < ringLights.Count; i++)
        {
            radii[i] = ringLights[i].GetRadius();
        }

        if (CheckRadiusInterference(radii))
        {
            CalculateInterferingRings();
        }
        else
        {
            CalculateRings();
        }
    }

    bool CheckRadiusInterference(double[] radii)
    {
        // checks if any of the RingLights have similar radii to determine,
        // if the positions should be calculated on a single ring or on multiple.
        // Multiple rings don't need to consider the ratios between amounts of LEDs,
        // but they should still be offset so there aren't two LEDs next to each other

        if (radii.Length < 2)
        {
            return false; // single or no ring doesn't need checking for interference, also prevents ArrayIndexOutOfBounds later
        }

        // Compare all radius pairs
        for (int i = 0; i < radii.Length - 1; i++)
        {
            for (int j = i+1; j < radii.Length; j++)
            {
                if (Math.Abs(radii[i]-radii[j]) < 0.01) // if any pair of rings has a radius difference smaller than 1 cm (ARBITRARY),
                                                        // that pair needs to be calculated on a single ring
                {
                    return true;
                }
            }
        }

        return false;   // there are no interfering rings
    }
    #endregion

    #region Simple rings
    void CalculateRings()
    {
        foreach (RingLight rl in ringLights)
        {
            CalculateRingGeometry(rl);
        }
    }

    void CalculateRingGeometry(RingLight ringLight) // LED geometry for a simple ring
    {
        Led[] lights = ringLight.GetLights();
        for (int i = 0; i < lights.Length; i++)
        {
            double azimuth = 2 * Math.PI * ((double)i / lights.Length);
            CalculateLedGeometry(lights[i], ringLight.GetRadius(), ringLight.GetTilt(), azimuth);
        }
    }
    #endregion

    #region Interfering rings
    void CalculateInterferingRings()
    {
        List<List<RingLight>> ringLightGroups = GroupRingLights(ringLights);

        foreach (List<RingLight> group in ringLightGroups)
        {
            if (group.Count == 1)
            {
                CalculateRingGeometry(group.First());
            }
            else
            {
                CalculateGroupGeometry(group);
            }
        }
    }

    #region Grouping
    List<List<RingLight>> GroupRingLights(List<RingLight> ringLights)
    {
        List<RingLight> ringLightsList = ringLights.ToList();
        ringLightsList.Sort(((x, y) => x.GetRadius().CompareTo(y.GetRadius())));    // sorts by radius ascending groups RingLights into groups 

        List<List<RingLight>> ringLightGroups = new List<List<RingLight>>();

        while (ringLightsList.Count > 0)
        {
            List<RingLight> group = CreateGroup(ringLightsList);

            ringLightGroups.Add(group);
        }

        return ringLightGroups;
    }

    List<RingLight> CreateGroup(List<RingLight> ringLightsList)
    {
        List<RingLight> group = new List<RingLight>();
        group.Add(ringLightsList.First());
        ringLightsList.RemoveAt(0);
        ringLightsList.TrimExcess();

        bool ringLightAdded = true;

        while (ringLightAdded)
        {
            if (ringLightsList.Count == 0)
            {
                ringLightAdded = false;
            }
            else if ((ringLightsList.First().GetRadius() - group.Last().GetRadius()) < 0.01) // Because rll is sorted and we always trim,
                                                                                        // we can just compare group.last with rll.first
            {
                group.Add(ringLightsList.First());
                ringLightsList.RemoveAt(0);
                ringLightsList.TrimExcess();

            }
            else
            {
                ringLightAdded = false;                                                 // And if the difference is bigger than safe, end grouping
            }
        }

        return group;
    }
    #endregion

    void CalculateGroupGeometry(List<RingLight> group)
    {
        group.Sort((x, y) => y.GetLights().Length.CompareTo(x.GetLights().Length)); // sorts RingLights descending based on number of LEDs

        // TESTED:
        // Ascending order: creates groups, ex.: 2, 1, 1 isn't distributed as caba, but as aabc, also spacing is not repeated
        // Randomized: last one is always grouped up with inconsistent spacing
        // Descending: doesn't create groups

        int numberOfPositions = TotalLedsInGroup(group);

        // Dictionary serves as a buffer to check which positions are already full
        // can be done with list, which would make AssignRingLightLeds easier,
        // but after each assignment it would have to be trimmed in order to work, checking for null might be easier for computing
        Dictionary<int, bool> ledPositions = InitializePositionsDictionary(numberOfPositions);  // Key - index of a position on the ring, initialized in this method
                                                                                                // Value - is there LED in that position?

        foreach (RingLight ringLight in group)
        {
            AssignRingLightLeds(ringLight, numberOfPositions, ledPositions);
            numberOfPositions -= ringLight.GetLights().Length;
        }
    }

    void AssignRingLightLeds(RingLight ringLight, int numberOfPositions, Dictionary<int, bool> ledPositions)
    {
        double stride = numberOfPositions / (double)ringLight.GetLights().Length;

        int cursor = 1;

        for (int i = 1; i <= ringLight.GetLights().Length; i++) // Assign each LED from one ring
        {
            int positionDelta = RoundDouble(i * stride) - RoundDouble((i - 1) * stride);    // Math.Round rounds to closest EVEN number, had to write own rounding func

            while (positionDelta > 0)
            {
                if (ledPositions[cursor] == true)
                {
                    cursor++;
                    continue;
                }

                positionDelta--;

                if (positionDelta == 0)
                {
                    // Calculate LED geometry for i-1 th LED in ringLight and assign this LED to the Dictionary
                    CalculateLedGeometry(ringLight.GetLights()[i - 1], ringLight.GetRadius(), ringLight.GetTilt(), 2 * Math.PI * ((double)(cursor - 1) / ledPositions.Count));
                    ledPositions[cursor] = true;
                }

                cursor++;
            }
        }
    }

    int RoundDouble(double value)
    {
        int i = 0;

        if ((value - (double)Math.Floor(value)) < 0.5)
        {
            i = (int)Math.Floor(value);
        }
        else
        {
            i = (int)Math.Ceiling(value);
        }
        return i;
    }

    Dictionary<int, bool> InitializePositionsDictionary(int numberOfPositions)
    {
        Dictionary<int, bool> ledPositions = new Dictionary<int, bool>();

        for (int i = 1; i <= numberOfPositions; i++)
        {
            ledPositions.Add(i, false);
        }

        return ledPositions;
    }

    int TotalLedsInGroup(List<RingLight> group)
    {
        int total = 0;

        foreach (RingLight rl in group)
        {
            total += rl.GetLights().Length;
        }

        return total;
    }
    #endregion

    void CalculateLedGeometry(Led led, double radius, double tilt, double azimuth)
    {
        double xcoord = radius * Math.Sin(azimuth);
        double ycoord = radius * Math.Cos(azimuth);

        double[] normalVector;

        if (tilt == 0)
        {
            // describes LED vector as vector from origin with magnitude of plane.GetDistance()
            
            normalVector = [0, 0, 1];
        }
        else if (tilt > 0)
        {
            // did not work for tilt = 0, tan0 = 0 -> 0/0
            normalVector = [xcoord, ycoord, radius / Math.Tan(tilt)]; // this guarantees correct magnitude
        }
        else
        {
            normalVector = [-xcoord, -ycoord, radius / Math.Tan(-tilt)];
            // vector with negative tilt points to the other side, thats why x and y are negative
            // z is calculated the same way, but it needs to point in the positive direction, that's why the tilt is turned back to positive
        }

        led.SetXcoord(xcoord);
        led.SetYcoord(ycoord);
        led.SetNormalVector(normalVector);
    }

    Led[] CreateLedArray()
    {
        int numberOfLEDs = 0;

        foreach (RingLight rl in ringLights)
        {
            numberOfLEDs += rl.GetLights().Length;
        }

        Led [] ledsTemp = new Led[numberOfLEDs];

        int index = 0;

        foreach(RingLight rl in ringLights)
        {
            foreach (Led led in rl.GetLights())
            {
                ledsTemp[index] = led;
                index++;
            }
        }

        return ledsTemp;
    }

    #region Getters, setters
    public Led[] GetLights()
    {
        return lights;
    }

    public List<RingLight> GetRingLights()
    {
        return ringLights;
    }
    #endregion
}