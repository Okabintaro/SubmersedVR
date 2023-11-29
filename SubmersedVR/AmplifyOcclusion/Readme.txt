# Amplify Unity Products

  While this package is provided, as is, for free, we develop and maintain professionally created 
  solutions used by thousands of developers. We invite you to check our current projects, and to 
  join our growing [Discord Community](https://discord.gg/SbNs7zK).
  
  [Amplify Shader Editor](https://assetstore.unity.com/packages/tools/visual-scripting/amplify-shader-editor-68570?aid=1011lPwI&pubref=GitHub) - Award-winning node-based shader creation tool
 
  [Amplify Impostors](https://assetstore.unity.com/packages/tools/utilities/amplify-impostors-beta-119877?aid=1011lPwI&pubref=GitHub) - 1-Click Impostor Creator
 
  [Amplify LUT Pack](https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/amplify-lut-pack-50070?aid=1011lPwI&pubref=GitHub) - 200+ LUTs for Amplify Color and Unity PPS

# Amplify Occlusion  
  
  Amplify Occlusion was the first industry-grade, full-featured screen-space ambient occlusion 
  solution to be released on the Asset Store in 2016, at a time when Unity itself lacked a decent
  SSAO implementation. It managed to remain the fastest SSAO solution for Unity released a built-in 
  implementation of "Multi-scale Volumetric Occlusion" as part of their Post-processing Stack.
  
  <p align="center"><img src="https://i.imgur.com/mSTDg79.gif"></p>

  The first version of this plugin was using a technique known as HBAO, or "Horizon-Based
  Ambient Occlusion", based on a 2008 paper titled "Image-Space Horizon-Based Ambient Occlusion" 
  by Louis Bavoil, Miguel Sainz and Rouslan Dimitrov.  

  The second version, which improved upon the first iteration on both quality and performance was
  using a technique known as GTAO, or "Ground-Truth Ambient Occlusion", based on a 2016 paper titled 
  "Practical Realtime Strategies for Accurate Indirect Occlusion" by Jorge Jimenez, Xian-Chun Wu, 
  Angelo Pesce and Adrian Jarabo.
  
  <p align="center"><img src="https://i.imgur.com/S4vPKuw.gif"></p>  

  This package was for sale on the Unity Asset Store between 2016 and 2019 with an
  average rating of 5 stars. It is now deprecated and we no longer support it, so we 
  are releasing it to open-source world under the MIT License.
	
# Description

  Amplify Occlusion sets out to deliver a new industry standard for fast, high-quality 
  Screen-Space Ambient Occlusion in Unity; delivering state of the art Ground Truth Ambient 
  Occlusion (GTAO) and bringing quality and accuracy closer to traditional raytracing. A true 
  all-in-one package, providing a highly-robust and efficient way to simulate ambient occlusion 
  and contact shadowing. Now you can attenuate reflections in occluded areas, make objects 
  actually connect to the world and add real depth to your scenes with minimal effort.
  
# Features

  * Ground Truth Ambient Occlusion
  * PS4, Xbox One and Switch compatible
  * Single and Multi-pass VR support
  * Up to 2X faster than Amplify Occlusion 1.0
  * Revamped Spatial and Temporal Filters
  * Dramatically Higher-Quality
  * Higher Flexibility
  * Under 1 ms on a mid-range GPU at Full HD
  * Accurate and fast-performing
  * Deferred and Forward Rendering
  * PBR compatible injection mode
  * Superior occlusion approximation
  * Extensive blur and intensity controls
  
# Supported Platforms

  All platforms
	
# Software Requirements

  Minimum

    Unity 5.6.0+

# Quick Guide

  Standard How-to

   1) Select and apply “Image Effects/Amplify Occlusion” to your main camera.
  
   2) Adjust the Intensity and Radius.
  
   3) Adjust the blur values until you are satisfied with the results.
 
  Scriptable Render Pipeline How-to

   1) Install packages dependencies:
     Window -> Package Manager, Advanced -> Show preview packages
     Select and install:
      
      Render-Pipelines.Core
      Render-Pipelines.High-Definition
      Render-Pipelines.Lightweight
      Post Processing

   2) Go to "Assets/Import Package/Custom Package..." and select
      "Assets/AmplifyOcclusion/Packages/PostProcessingSRP_XXX.unitypackage"

   3) How to set up an SRP project example:

   3.a) Create SRP asset via Assets menu:
   
	   Create/Rendering/High Definition Render Pipeline Asset

	   OR

	   Create/Rendering/Lightweight Render Pipeline Asset
  
   3.b) Set Edit->ProjectSettings/Player/Other settings/ColorSpace to Linear (necessary for HD SRP)
  
   3.c) Edit->ProjectSettings/Graphics/Scriptable Render Pipeline Settings: select the RenderPipelineAsset 
        created in 3.a)
  
   3.d) On Camera, using Lightweight Render Pipeline, disable MSAA
  
   3.e) Camera->Add Component->Post-Process Layer
  
   3.f) Camera->Post-Process Layer->Layer: Everything (as example)
  
   3.g) Camera->Add Component->Post-Process Volume
  
   3.h) Camera->Post-Process Volume->Is Global: check (as example)
  
   3.i) Camera->Post-Process Volume->Profile: New
  
   3.j) Camera->Post-Process Volume->Add effect... AmplifyCreations->AmplifyOcclusion

# Documentation

  Please refer to the following website for an up-to-date online manual:

    http://amplify.pt/unity/amplify-occlusion/manual

# Acknowledgements

  AO v2.0 was developed by the talented Mário Luzeiro:
  
    https://pt.linkedin.com/in/mluzeiro
    https://twitter.com/mluzeiro
    
