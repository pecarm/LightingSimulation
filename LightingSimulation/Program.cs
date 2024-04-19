#region Constants

double[] LED_POSITION_TEST_PROFILE = [1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]; // used for testing the calculated positions of LEDs
                                                                                                // 5° conical rad. profile, thanks to lin. approx.
List<double> PROFILE = [1, 0.97, 0.9, 0.7, 0.5, 0.4, 0.3, 0.2, 0.12, 0.08, 0.05, 0.03, 0.02, 0.01, 0, 0, 0, 0, 0];  // there must be a better way to do this...
                                    // 5° precision should be enough. Rest linear approx.
double DISTANCE = 0.3;              // meters
double INTENSITY = 0.030;           // W/sr
double SOURCE_RADIUS = 0.05;        // meters
int NUMBER_OF_LEDS = 10;
double TILT = (Math.PI / 180) * 20; // degrees
int XDIM = 3840;                    // horizontal
int YDIM = 2160;                    // vertical
double FOV = (Math.PI / 180) * 90;  // can be calculated using camera + lens properties, maybe TODO, DEGREES
string MODEL = "Test Model Semi-realistic Curve";
#endregion

#region Top Level Statements
RunApp();
#endregion

#region Methods
void RunApp()
{
    Console.WriteLine("._.");

    string path = GetPathToYamlFile();

    SimulationProgramme programme = SimulationProgramme.ReadFromYamlFile(path);

    StartSimulationProgramme(programme);

    /*
    Led led = new Led(INTENSITY, PROFILE, MODEL);

    Plane plane = new Plane(XDIM, YDIM, DISTANCE, FOV);

    Console.WriteLine("Pixel plane created");
    */

    /*
    for (int i = 0; i <= 30; i=i+5)
    {
        RingLight ringLight = new RingLight(led, SOURCE_RADIUS, (Math.PI / 180) * i, NUMBER_OF_LEDS);
        Console.WriteLine("Light source created");
        
        Illumination illumination = new Illumination(plane, ringLight);
        
        illumination.FastCalculateIllumination();
        
        plane.ExportAsBitmap();
    }
    */
    
    /*
    RingLight ringLight = new RingLight(led, SOURCE_RADIUS, TILT, NUMBER_OF_LEDS);

    LightSource lightSource = new LightSource([ringLight]);

    Console.WriteLine("Light source created");

    Simulation simulation = new Simulation(plane, lightSource);

    simulation.CalculateIlluminationPreview(10);
    */
}

string GetPathToYamlFile()
{
    string filePath;

    do
    {
        Console.WriteLine("Enter the path to a configuration file:");
        filePath = Console.ReadLine();

        // Check if file exists
        if (filePath == "exit")
        {
            System.Environment.Exit(0);
        }
        else if (!File.Exists(filePath) || !(filePath.Contains(".yaml") || filePath.Contains(".yml")))
        {
            Console.WriteLine("Invalid file path. Please enter a valid path to a configuration '.yaml' / '.yml' file or delete quotes (\")from the path:");
        }

    } while (!File.Exists(filePath));

    return filePath;
}

void StartSimulationProgramme(SimulationProgramme programme)
{
    bool validInput;
    do
    {
        Console.WriteLine("Do you wish to:\n" +
            "1 - Simulate in full resolution, or\n" +
            "2 - Simulate in lower resolution (specify later)");
        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                validInput = true;
                programme.RunProgramme();
                break;
            case "2":
                validInput = true;
                programme.RunProgrammePreview(GetClusterSize());
                break;
            default:
                validInput = false;
                Console.WriteLine("Invalid input, please select 1 or 2 as an option.");
                break;
        }
    } while (!validInput);
}

int GetClusterSize()
{
    int number;
    bool isValidInput = false;

    do
    {
        Console.WriteLine("Preview is calculated in 'a x a' sized clusters.\n" +
            "Please, input a:");
        string input = Console.ReadLine();

        // TryParse to handle non-numeric input
        isValidInput = int.TryParse(input, out number);

        if (!isValidInput)
        {
            Console.WriteLine("Invalid input. Please enter an integer:");
        }

    } while (!isValidInput);

    return number;
}
#endregion