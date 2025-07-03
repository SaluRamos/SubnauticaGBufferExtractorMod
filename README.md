## Objective

capture the g-buffers of Subnautica (and Subnautica Below Zero) and save them in JPG format to create a rich dataset.  

the captured gbuffers are:  
- Final Render (WORKING)  
- World Normal Map (WORKING)  
- Depth Map (WORKING)  
- Albedo Map (WORKING)  
- Emission Map (NOT WORKING)  
- ShaderID/Segmentation per Material (NOT WORKING)  

![alt text](readme_images/gbuffers.png)  

## How to use  

Install by extrating the mod inside "PathToSubnauticaFolder/BepInEx/plugins".  
ConfigurationManager plugin is required for you to configure paths ingame.  
Inside the game, Press F1 to open manager menu, then configure "CaptureFolder" and "AssetBundlePath".  
The "assetBundleFolderPath" must be full path to "PathToSubnauticaFolder/BepInEx/plugins/GBufferCapture/Shaders"  
The "CaptureFolder" is self-explanatory.  
INSIDE A CREATIVE WORLD, press F11 to start mod core. DO NOT START MOD CORE AT GAME MENU.  
then, just press F10 to start/stop recording. The captures are taken every one second by default.  

WARNING: In current build, if you get back to game menu, you will need to restart the game for the capture system to work again  

RECOMMENDATION: I recommend you to use dev commands such as "daynightspeed" to increase light diversity in you dataset. SHIFT + ENTER opens command entry. Also, remove game GUI from captures using F6 hotkey.  

## Dependecies

- https://github.com/BepInEx/BepInEx  
- https://github.com/BepInEx/BepInEx.ConfigurationManager (configure ingame variables)  

## Utils

For those who want to improve this project.  
use Unity 2019.4.36f1: https://unity.com/releases/editor/archive  
The repository has compiled assetBundle, but you can compile inside Unity Project just by executing: Build -> Build Shader AssetBundle.  

- https://github.com/sinai-dev/UnityExplorer (very useful to inspect gameobjects ingame)  
- https://github.com/AssetRipper/AssetRipper (very useful to decompile subnautica and read every script/shader of the game, remember to use "decompile" shader option)  
