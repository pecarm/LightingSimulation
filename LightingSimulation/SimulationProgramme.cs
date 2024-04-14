using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

class SimulationProgramme
{
    public List<Simulation> configurations { get; set; }

    public SimulationProgramme()
    {

    }

    public SimulationProgramme(List<Simulation> configurations)
    {
        this.configurations = configurations;
    }

    public void Init()
    {
        foreach (Simulation simulation in configurations)
        {
            simulation.Init();
        }
    }

    public void RunProgramme()
    {
        Init();

        foreach (Simulation simulation in configurations)
        {
            simulation.CalculateIllumination();
        }
    }

    public void RunProgrammePreview(int clusterSize)
    {
        Init();

        foreach(Simulation simulation in configurations)
        {
            simulation.CalculateIlluminationPreview(clusterSize);
        }
    }
    
    public static SimulationProgramme ReadFromYamlFile(string filename)
    {
        string yamlContents = File.ReadAllText(filename);

        var input = new StringReader(yamlContents);

        var deserializer = new DeserializerBuilder()
           .WithNamingConvention(CamelCaseNamingConvention.Instance) // commented out for testing, might work just fine without it
           .Build();

        SimulationProgramme programme = new SimulationProgramme();

        try
        {
            programme = deserializer.Deserialize<SimulationProgramme>(input);
        }
        catch (Exception)
        {
            Console.WriteLine("Invalid YAML file structure, please refer to example.yml");
            throw;
        }

        return programme;
    }
    
}