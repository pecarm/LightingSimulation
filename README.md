<!-- ABOUT THE PROJECT -->
# LightingSimulation
LightingSimulation is a C# programme, which calculates the irradiance and the angle of incidence of a plane placed in a certain distance.
These results are then exported as PNG files. The input for the simulator is a YAML configuration file, which is included in the project files.
You can configure the resolution and FOV of the camera/plane as well as its distance together with the properties of the light source.
The light source is composed of multiple ring shaped light sources, each defined by its radius, number of LEDs, their tilt and type.
The type of LED is defined by including its model name for logging purposes, intensity provided in W/sr and radiation profile.
The contents of the configuration file are described later on together with the instructions on how to use it.

<!-- GETTING STARTED -->
## Getting Started
When you clone this repository you should have everything you need to compile and run it yourself, all libraries are included.

Build your project using the .NET CLI by navigating to the project directory and typing this command:
   ```sh
   dotnet build LightingSimulation.csproj
   ```
or by importing this project to Visual Studio and running it.

Then modify the configuration file based on your desired configuration and after running the simulator, when prompted, provide a path to the file.
You will then be asked whether or not you wish to simulate in full or just partial resolution. This has a notable effect on performance.

After entering the desired level of detail, the simulation will begin.
When it is over, you will be able to find the results of the simulation in the directory in which the executable file is located.
The results consist of 3 files, one being the normalized irradiance (black - lowest value, white, highest), titled "irradiance-datetime".
Another file contains normalized angle of incidence (white - best, close to perpendicular, black - worst, see log), titled "angle_of_incidence-datetime".
Last file is a log file containing basic data about the results along with a description of the light source and plane/camera information, titled "log-datetime".

## Configuration YAML file
When creating the simulator, the goal was to make it so that the user will be able to provide multiple `configurations` and set it running in order to later compare the results.
This is the reason why a configuration file was chosen. The file is read using [YamlDotNet](https://github.com/aaubry/YamlDotNet) and thus supported YAML versions are the same as in this library (if I remember to update it...).
When creating this file, please refer to the `simulation_config.yml` sample configuration file. In case something breaks, make sure to keep a copy of it on your system.

### Plane
The `plane` describes the illuminated plane that is observed by a camera. This plane is defined by its resolution using fields `xDim` and `yDim`.
Then the `fov` of the camera is required along with the `distance` from light source/camera to the plane in meters.

### Lightsource
The `lightSource` is defined by multiple `ringLights`. Each ring light must be defined by its `radius`, `numberOfLeds`, the `led` type and their `tilt`.
The tilt is measured against the normal going through the centre of the ring light. Positive angle signify tilt outwards, negative inwards.
Actual positions of LEDs in real coordinates is calculated before the simulation starts. It is currently not possible to enter an orphaned LED with specific coordinates.
If any pair of ring lights has a difference of diameters less than 1 cm, it is assumed, that these ring lights share a common ring on which they are placed on.
If we assume a ring light R1 with 10 LEDs, the angular difference between each will be 36°.
If we then add another ring light R2 with 5 LEDs with a radius within previously specified range, R1 and R2 LEDs will be placed on the same ring.
This means the angular difference LEDs will now be 24°.
The LEDs will also be fairly and evenly distributed, meaning a pair of LEDs from R1 will always be followed by an LED from R2.

Multiple ring lights can be in the same group.
If R1 (r = 7.9 cm) and R2 (r = 8.5 cm) are in the same group, then R3 (r = 9 cm) will also be in that group with R1 and R2, even though the difference between R3 and R1 does not meet the standard.
This is because there exists a colission with R2.

### LEDs
Fianlly, an `led` is defined by its `intensity`, `model` name and `radiationProfile`. The `radiationProfile` values, each representing a 5-degree increment, are entered as relative in range 0 -> 1.
The inbetween values are calculated using linear approximation, as calculating a mathematical expression for the intensity curve would be difficult and this method provides satisfactory results.
