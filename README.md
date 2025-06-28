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
- 

Components in main Camera:
[Info   : Unity Log] Componente mainCam: UnityEngine.Transform, Assembly: UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: UnityEngine.Camera, Assembly: UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: UnityEngine.FlareLayer, Assembly: UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: CullingCamera, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: ShaderGlobals, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: WaterscapeVolumeOnCamera, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: WaterSurfaceOnCamera, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: WaterSunShaftsOnCamera, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: WBOIT, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: LensWaterController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: LensWater, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: RadiationsScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: ExplosionScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: DamageScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: TeleportScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: WarpScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: MesmerizedScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: MapRoomCameraScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: TelepathyScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: SonarScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: EndSequenceWarpScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: RadialBlurScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: CyclopsSmokeScreenFX, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: UnityEngine.PostProcessing.PostProcessingBehaviour, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: ColorCorrection, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: VisualizeDepth, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: UWE.FrameTimeOverlay, Assembly: Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: LensWaterController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: RadiationsScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: UnityStandardAssets.ImageEffects.Grayscale, Assembly: Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: MapRoomCameraScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: TeleportScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: WarpScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: MesmerizedScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: CyclopsSmokeScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: TelepathyScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: EndSequenceWarpScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: RadialBlurScreenFXController, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: MainCamera, Assembly: Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: MainCameraV2, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: UwePostProcessingManager, Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: UnityEngine.StreamingController, Assembly: UnityEngine.StreamingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
[Info   : Unity Log] Componente mainCam: FrameTimeRecorder, Assembly: Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null

All used Shaders in game:
[Info   : Unity Log] Found shader: Hidden/Internal-StencilWrite
[Info   : Unity Log] Found shader: Hidden/VFXSchoolFishUpdate
[Info   : Unity Log] Found shader: Hidden/Waterscape/ColorCorrection
[Info   : Unity Log] Found shader: Hidden/Waterscape/LensWater
[Info   : Unity Log] Found shader: Hidden/Waterscape/UpdateDisplacement
[Info   : Unity Log] Found shader: Hidden/Waterscape/Resize
[Info   : Unity Log] Found shader: Hidden/Waterscape/PackDisplacement
[Info   : Unity Log] Found shader: Hidden/Waterscape/InterpolateDisplacement
[Info   : Unity Log] Found shader: Hidden/Waterscape/UpdateNormals
[Info   : Unity Log] Found shader: Hidden/Waterscape/UpdateFoam
[Info   : Unity Log] Found shader: Hidden/AtmosphereVolume
[Info   : Unity Log] Found shader: Hidden/SettingsMapResize
[Info   : Unity Log] Found shader: Hidden/BiomeMapDebug
[Info   : Unity Log] Found shader: Hidden/BiomeMapLookup
[Info   : Unity Log] Found shader: Hidden/BiomeMapUnwrap
[Info   : Unity Log] Found shader: Hidden/BiomeMapBlur5x5x5
[Info   : Unity Log] Found shader: Hidden/Image Effects/GammaCorrection
[Info   : Unity Log] Found shader: Hidden/Waterscape/WaterSunShafts
[Info   : Unity Log] Found shader: Hidden/WBOIT Composite
[Info   : Unity Log] Found shader: Hidden/VisualizeDepth
[Info   : Unity Log] Found shader: Hidden/UI/CircularBar
[Info   : Unity Log] Found shader: Hidden/WaterscapeFog
[Info   : Unity Log] Found shader: Hidden/uSky/Stars
[Info   : Unity Log] Found shader: Hidden/Internal-MotionVectors
[Info   : Unity Log] Found shader: Hidden/Internal-ScreenSpaceShadows
[Info   : Unity Log] Found shader: Hidden/Internal-Flare
[Info   : Unity Log] Found shader: Hidden/InternalErrorShader
[Info   : Unity Log] Found shader: Hidden/BlitCopy
[Info   : Unity Log] Found shader: Hidden/Internal-DeferredReflections
[Info   : Unity Log] Found shader: Hidden/Internal-DeferredShadingCustom
[Info   : Unity Log] Found shader: Hidden/Post FX/Motion Blur
[Info   : Unity Log] Found shader: Hidden/Post FX/FXAA
[Info   : Unity Log] Found shader: Hidden/Post FX/Bloom
[Info   : Unity Log] Found shader: Hidden/Post FX/Depth Of Field
[Info   : Unity Log] Found shader: Hidden/Post FX/Uber Shader
[Info   : Unity Log] Found shader: Hidden/Post FX/Screen Space Reflection
[Info   : Unity Log] Found shader: Hidden/Post FX/Ambient Occlusion
[Info   : Unity Log] Found shader: Hidden/Post FX/Blit
[Info   : Unity Log] Found shader: UWE/Terrain/Triplanar
[Info   : Unity Log] Found shader: UWE/Terrain/Triplanar with Capping
[Info   : Unity Log] Found shader: UWE/SIG AlphaCutout + Noisey Wave
[Info   : Unity Log] Found shader: UWE/SIG Terrain Grass
[Info   : Unity Log] Found shader: UWE/SIG
[Info   : Unity Log] Found shader: UWE/SIG Alpha Border
[Info   : Unity Log] Found shader: UWE/WBOIT/Clear
[Info   : Unity Log] Found shader: UWE/Terrain/TerrainOpaquePass
[Info   : Unity Log] Found shader: UWE/Graphs/PlotElement
[Info   : Unity Log] Found shader: UWE/Particles/WBOIT ScreenDrops
[Info   : Unity Log] Found shader: UWE/WBOIT-UIBar_Color
[Info   : Unity Log] Found shader: UWE/Particles/WBOIT-FakeVolumetricLight
[Info   : Unity Log] Found shader: UWE/GlassExteriorWaterFix
[Info   : Unity Log] Found shader: UWE/SIG Transparent
[Info   : Unity Log] Found shader: UWE/Sprites/Default
[Info   : Unity Log] Found shader: UWE/Marmoset/PropulsionCannon
[Info   : Unity Log] Found shader: UI/Bar
[Info   : Unity Log] Found shader: UI/IconBar
[Info   : Unity Log] Found shader: UI/Icon
[Info   : Unity Log] Found shader: UI/Default
[Info   : Unity Log] Found shader: UI/Simple
[Info   : Unity Log] Found shader: UI/ResourcesBlip
[Info   : Unity Log] Found shader: UI/Circle
[Info   : Unity Log] Found shader: UI/Default Font
[Info   : Unity Log] Found shader: Custom/DistanceField/ClipDistanceField
[Info   : Unity Log] Found shader: Custom/UIBar
[Info   : Unity Log] Found shader: Custom/BuilderObstacle
[Info   : Unity Log] Found shader: Custom/GhostModel
[Info   : Unity Log] Found shader: Custom/CaptureDepth
[Info   : Unity Log] Found shader: Custom/WaterSurface
[Info   : Unity Log] Found shader: Custom/SubName
[Info   : Unity Log] Found shader: uGUI/Compass
[Info   : Unity Log] Found shader: uGUI/DepthClear
[Info   : Unity Log] Found shader: uGUI/IntroLogo
[Info   : Unity Log] Found shader: uGUI/WorldCursor
[Info   : Unity Log] Found shader: uGUI/Default-Holographic
[Info   : Unity Log] Found shader: FX/WBOIT-HoloMap
[Info   : Unity Log] Found shader: FX/WBOIT-BaseBoxFogVolume
[Info   : Unity Log] Found shader: FX/WBOIT-WaterPlane
[Info   : Unity Log] Found shader: FX/Clouds
[Info   : Unity Log] Found shader: FX/add_WaterSplash
[Info   : Unity Log] Found shader: FX/WBOIT-fireball-Triplanar
[Info   : Unity Log] Found shader: FX/FabricatorBeam
[Info   : Unity Log] Found shader: FX/PowerPreview
[Info   : Unity Log] Found shader: FX/add_2emDistortFresnel-Triplanar
[Info   : Unity Log] Found shader: FX/RadialBlur
[Info   : Unity Log] Found shader: FX/Rain
[Info   : Unity Log] Found shader: FX/Snow
[Info   : Unity Log] Found shader: FX/WBOIT-fireball-Triplanar-VertexLit
[Info   : Unity Log] Found shader: FX/WBOIT-CyclopsShield
[Info   : Unity Log] Found shader: FX/WBOIT add_2emDistortFresnelClipUnderFlood
[Info   : Unity Log] Found shader: FX/WBOIT-CyclopsSonar
[Info   : Unity Log] Found shader: FX/WBOIT-SphereVolumeNoise
[Info   : Unity Log] Found shader: FX/WBOIT-BaseCylinderFogVolume
[Info   : Unity Log] Found shader: FX/WBOIT-HoloBlip
[Info   : Unity Log] Found shader: FX/WBOIT-PrecursorPortal
[Info   : Unity Log] Found shader: Legacy Shaders/Diffuse
[Info   : Unity Log] Found shader: Legacy Shaders/Transparent/Diffuse
[Info   : Unity Log] Found shader: Legacy Shaders/Particles/Additive
[Info   : Unity Log] Found shader: Legacy Shaders/Particles/Alpha Blended Premultiply
[Info   : Unity Log] Found shader: Legacy Shaders/Bumped Diffuse
[Info   : Unity Log] Found shader: Unlit/DepthOnly
[Info   : Unity Log] Found shader: Unlit/MipDownsample
[Info   : Unity Log] Found shader: Unlit/Blit
[Info   : Unity Log] Found shader: Unlit/Texture
[Info   : Unity Log] Found shader: Unlit/Color
[Info   : Unity Log] Found shader: Unlit/FX_WarpTube
[Info   : Unity Log] Found shader: Unlit/FX-FakeGodRays
[Info   : Unity Log] Found shader: Image Effects/radiations
[Info   : Unity Log] Found shader: Image Effects/EndSequenceWarp
[Info   : Unity Log] Found shader: Image Effects/Teleport
[Info   : Unity Log] Found shader: Image Effects/Telepathy
[Info   : Unity Log] Found shader: Image Effects/Mesmerized
[Info   : Unity Log] Found shader: uSky/uSkymap
[Info   : Unity Log] Found shader: uSky/uSkyBox
[Info   : Unity Log] Found shader: uSky/uSkyBox Horizon Offset
[Info   : Unity Log] Found shader: TextMeshPro/Distance Field
[Info   : Unity Log] Found shader: TextMeshPro/Mobile/Distance Field
[Info   : Unity Log] Found shader: TextMeshPro/Sprite
[Info   : Unity Log] Found shader: Sprites/Mask
[Info   : Unity Log] Found shader: Sprites/Default
[Info   : Unity Log] Found shader: Standard
[Info   : Unity Log] Found shader: Voxeland/Grass
[Info   : Unity Log] Found shader: Marmoset/Skybox IBL
[Info   : Unity Log] Found shader: Particles/MagmaFrag
[Info   : Unity Log] Found shader: GUI/Text Shader
[Info   : Unity Log] Found shader: Skybox/Procedural
[Info   : Unity Log] Found shader: DefaultNoCull
[Info   : Unity Log] Found shader: DontRender

CommandBuffers in mainCam:
[Info   : Unity Log] Event: AfterFinalPass, CB: Screen Space Reflection
[Info   : Unity Log] Event: BeforeImageEffectsOpaque, CB: Ambient Occlusion
[Info   : Unity Log] Event: BeforeForwardAlpha, CB: Unnamed command buffer
[Info   : Unity Log] Event: AfterForwardAlpha, CB: Builder Obstacles
[Info   : Unity Log] Event: BeforeImageEffects, CB: Motion Blur