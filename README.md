This is a fork of **_RealSolarSystem_**, intended to separate the **PQSMod_VertexHeightMapRSS** from RealSolarSystem, for anyone who wants to use **16-bit grayscale heightmaps** for their celestials without installing RealSolarSystem.

For compatibility reasons, the PQSMod_VertexHeightMapRSS is renamed to **PQSMod_VertexHeightMap16Grayscale**, to prevent conflict with VertexHeightMapRSS itself and VertexHeightMap16 from Kopernicus Expansion.

### Dependencies:
* ModuleManager
* Kopernicus
* (Not a mod) Use [TopoConv](https://github.com/KSP-RO/RSS-Textures/tree/master/tools/TopoConv) to get the proper 16-bit DDS format grayscale heightmap
### Incompatibilities: No
### Recommendations:
* (Not a mod) [GrayscaleGenerator](https://github.com/newo-ether/GrayscaleGenerator) is a tool to generate grayscale maps for 3D models. Specifically, it calculates the distance of each point on the surface relative to the origin point, and converts the result to an image using spherical projection. This project aims to solve the problem of importing custom celestial bodies into **_Kerbal Space Program_**.
### Usage:
	Kopernicus
	{
		Body
		{
			PQS
			{
				Mods
				{
					VertexHeightMap16Grayscale
					{
						map = YourPath/16bitGrayscaleHeightmap.dds
						offset = offset_value
						deformity = deformity_value
						scaleDeformityByRadius = false/true
						order = order_value
						enabled = true/false
					}
				}
			}
		}
	}

All credits give to KSP-RO team.

License: CC-BY-NC-SA, the same as RealSolarSystem.
