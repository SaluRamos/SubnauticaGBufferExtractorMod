## Objective

capture the g-buffers of Subnautica and save them in PNG/JPG format to create rich datasets.

the captured gbuffers are:
- Final Render (WORKING)
- World/Local Normal Map
- Depth Map (WORKING)
- Albedo Map
- Specular Map
- Glossiness Map
- ShaderID/Segmentation (Layer/Tag/Material)

![alt text](readme_images/gbuffers.png)

## Configuration

You need to modify the Paths in PIPELINE/Assets/GBufferExtractor.cs. 
You also need to generate the AssetsBundle for the shaders, just execute in the Editor: Build -> Build Shader AssetBundle. 

## Dependencies

Unity 2019.4.36f1: https://unity.com/releases/editor/archive

- https://github.com/BepInEx/BepInEx
- https://github.com/BepInEx/BepInEx.ConfigurationManager
- https://github.com/AssetRipper/AssetRipper
- https://github.com/sinai-dev/UnityExplorer