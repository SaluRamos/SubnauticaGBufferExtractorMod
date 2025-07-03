## Objective

capture the g-buffers of Subnautica (and Subnautica Below Zero) and save them in JPG format to create rich datasets.  
install by putting the mod DLL inside BepInEx/plugins.  
In game, just press F10 to start/stop recoording. The captures are taken every half second (Configurable ingame if you have ConfigurationManager, just press F1 to open manager menu)  

the captured gbuffers are:  
- Final Render (WORKING)  
- World/Local Normal Map  (WORKING)  
- Depth Map (WORKING)  
- Albedo Map (WORKING)  
- Emission Map (NOT WORKING)  
- ShaderID/Segmentation per Material (NOT WORKING)  

![alt text](readme_images/gbuffers.png)

## Configuration

You need to modify the Paths (variables "assetBundleFolderPath" and "captureFolder") in BepInExBuiltin/GBufferCapturePlugin.cs  
The "assetBundleFolderPath" must be fullPath to BepInExBuiltin/Shaders  
The repository has compiled assetBundle, but you can compile inside Unity Project just by executing: Build -> Build Shader AssetBundle.  

## Dependecies

- https://github.com/BepInEx/BepInEx  

## Utils

use Unity 2019.4.36f1: https://unity.com/releases/editor/archive

- https://github.com/BepInEx/BepInEx.ConfigurationManager (configure ingame capture interval)  
- https://github.com/sinai-dev/UnityExplorer  
- https://github.com/AssetRipper/AssetRipper (use decompile shader config!)  