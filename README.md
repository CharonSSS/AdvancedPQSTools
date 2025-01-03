This is a fork of **_RealSolarSystem_**, intended to separate the **PQSMod_VertexHeightMapRSS** from RealSolarSystem, for anyone who wants to use **16-bit grayscale heightmaps** for their celestials without installing RealSolarSystem.

For compatibility reasons, the PQSMod_VertexHeightMapRSS is renamed to **PQSMod_VertexHeightMap16Grayscale**, to prevent confliction of VertexHeightMapRSS itself and VertexHeightMap16 from Kopernicus Expansion.

### Dependencies: ModuleManager and Kopernicus
### Incompatibilities: No
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
