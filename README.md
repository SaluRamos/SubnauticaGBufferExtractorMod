## Objective

capture the g-buffers of any Unity game and save them in PNG/JPG format to create rich datasets.

the captured gbuffers are:
- Final Render
- World Normal Map
- Local Normal Map
- Depth Map
- Albedo Map
- ShaderID/Segmentation (Layer/Tag)

The mod was developed within Unity and transferred to BepInEx.

![alt text](readme_images/gbuffers.png)

## Configuration

You need to modify the Paths in PIPELINE/Assets/GBufferExtractor.cs (depending on the render pipeline you chose). 
You also need to generate the AssetsBundle for the shaders, just execute in the Editor: Build -> Build Shader AssetBundle. 
At first, I will only be implementing the mod for the BuiltIn render pipeline.

To-Do:
- HDRP pipeline
- URP pipeline

## Dependencies

Unity 2019.4.16f1: https://unity.com/releases/editor/archive

- https://github.com/BepInEx/BepInEx
- https://github.com/BepInEx/BepInEx.ConfigurationManager
