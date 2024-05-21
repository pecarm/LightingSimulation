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

    public void RunProgramme(bool includeGraphics)
    {
        foreach (Simulation simulation in configurations)
        {
            using (simulation)
            {
                simulation.Init();
                simulation.CalculateIllumination(includeGraphics);
            }
        }
    }

    public void RunProgrammePreview(int clusterSize, bool includeGraphics)
    {
        foreach(Simulation simulation in configurations)
        {
            using (simulation)
            {
                simulation.Init();
                simulation.CalculateIlluminationPreview(clusterSize, includeGraphics);
            }
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