##  Unity Resolve AA Rendertarget On Command
[![license](http://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-blue.svg)](https://github.com/sienaiwun/Unity_AAResolveOnCommand/pulls)

When setting rendertargets in Unity LWRP or URP templates, it will triggered the resolve operation of the old rendertarget if MSAA is used automatically. On some platforms, resolve operation is [costly](https://forum.unity.com/threads/every-graphics-blit-causes-rendertexture-resolveaa-if-msaa-enabled-which-is-killing-framerate.457653/)(0.6ms on Iphone XR for a full-screen msaa's buffer's resolve operation). This demo gives more control over  rendertargets by setting resolve operations manually.

![resovleaa](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/imgs/before.png)

This image shows the redundant resolve operations in LWRP or UWP. A MSAA rendertarget is used as a shader resource for two consecutive passes, two resolve operation is triggered although the second is not needed.

In this demo, the frame analysis is this:
![after](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/imgs/after.png) 

The above image shows in this demo, only one resolve operation is triggered in the same scenario.

The magic here is to use [RenderTexture](https://docs.unity3d.com/ScriptReference/RenderTexture.html) instead of [RenderTargetIdentifier](https://docs.unity3d.com/ScriptReference/Rendering.RenderTargetIdentifier.html). Rendertexture gives [a way](https://docs.unity3d.com/ScriptReference/RenderTextureDescriptor-bindMS.html) to control the auto-resolve, and [a method](https://docs.unity3d.com/ScriptReference/RenderTexture.ResolveAntiAliasedSurface.html) to trigger the resolve operation on command. The related changes are in submit [resolve rendertarget on command](https://github.com/sienaiwun/Unity_AAResolveOnCommand/commit/1ff584496e8cbcdb36571e327362a6ac9c9242ea).

Xcode capture frame also visualizes it：

![before](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/imgs/framegraph_before.png)

The previous way has two resolve operations. Each one costs 0.6ms 

![now](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/imgs/framegraph_now.png)

Now two resolve operations merge into a single one. Saves 0.6ms.


## Unity手动控制rendertarget的resolveAA操作

#### 介绍
带有MSAA的RT常被用于增加画面表现细节，又由于MSAA操作在某些手机硬件上[绘制是free的](https://docs.imgtec.com/PerfRec/topics/c_PerfRec_msaa_performance.html)，所以在手机上一种常有的操作是开MSAA同时降分辨率来增加效率。但是在unity引擎上有个掣肘，每次切换rendertarget都会出现切换出去的rendertarget发生aa的resolve操作。在某些情况下rendertarget的resolve操作是冗余的,而且还[影响效率]
(https://forum.unity.com/threads/every-graphics-blit-causes-rendertexture-resolveaa-if-msaa-enabled-which-is-killing-framerate.457653/)。


![resovleaa](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/imgs/before.png)

此图显示了使用LWRP,UWP中的冗余AA resolve操作，一张Texture2DMS连续被用作shader resource,每一次使用都触发了resolve操作，但是实际上第二次resolve是不需要的。

使用本文介绍的修改通过1.取消自动resolve配置和2.手动设置resolve操作来取消操作的冗余。


#### 实现步骤
LWRP,URP模板使用[RenderTargetIdentifier](https://docs.unity3d.com/ScriptReference/Rendering.RenderTargetIdentifier.html)进行RT的切换，这个类很难设置resolve的频率，但是RT也可以由[RenderTexture](https://docs.unity3d.com/ScriptReference/RenderTexture.html)设置，这个类的创建时候的[bindMS](https://docs.unity3d.com/ScriptReference/RenderTextureDescriptor-bindMS.html)参数来控制是否自动进行RT的resolve和[ResolveAntiAliasedSurface](https://docs.unity3d.com/ScriptReference/RenderTexture.ResolveAntiAliasedSurface.html)手动控制resolve的操作。
具体修改可见[resolve rendertarget on command](https://github.com/sienaiwun/Unity_AAResolveOnCommand/commit/1ff584496e8cbcdb36571e327362a6ac9c9242ea)修改。存储上，本修改在使用msaa的rendertarget通过开启bindms设置增加一个带msaa的rendertexure(m_color_handle)和一个不带msaa的rendertexture(m_resolve_handle)。看上去增加一个rt，但是在unity本身中如果关闭bindms,只用一个rt内部也会有两个rendertexutre handle,一个带msaa的texture2dMS，一个不带msaatexture2d。所以概念上是等同的。

#### 结果
修改后再rendertarget的创建函数可以指定rt由RenderTargetIdentifier还是rendertexture创建。如果是使用msaa的rendertarget，需要用GetShaderResource()返回Texture2D，原来的Identifier()则返回rendertarget handle 或者Texture2DMS.
在render中则需要显示增加一个resolvePass添加resolveAA操作。
![after](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/imgs/after.png)
显示只用一个resolve操作就能实现之前的操作，省下一个resolve操作。

Xcode 截帧也显示出了这一点：

![before](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/imgs/framegraph_before.png)

原来resolve操作有两个（图中的蓝色节点），在iphone XR耗时0.6ms。经过优化后

![now](https://github.com/sienaiwun/Unity_AAResolveOnCommand/blob/master/imgs/framegraph_now.png)

可以看到只有一个resolve操作，最后resolve的texture2d被两个pass所用，iphone XR上节省了一个600us的全屏resolve操作。
