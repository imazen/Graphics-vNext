Server-side graphics in ASP.NET - the present and future.
==============

TLDR; ASP.NET developers presently use the closed-source System.Drawing and WPF libraries, which (for excellent reasons, like global locks and GC bugs) are **unsupported** for sever-side use.

Neither will work in .NET Core, let alone cross-platform. System.Drawing is a GDI+ wrapper, and WPF is a  WIC/DirectX 9 wrapper with a bit of glue. There are [no plans to open-source or port  WPF](http://channel9.msdn.com/Blogs/DevRadio/The-Future-of-WPF).

.NET *already* stands alone as a platform without server-friendly graphics/imaging library. 

Unless we start now, there will be **NO** image-processing story ready when ASP.NET vNext and .NET Core reach stable status. Anecdotally, 9 out of 10 webapps I've used depend on imaging or graphics libraries of some kind. I think it's safe to say this will impede adoption of both .NET Core and ASP.NET vNext.

### The .NET Future is open-source and cross-platform

[.NET is going open-source and cross-platform](http://www.hanselman.com/blog/announcingnet2015netasopensourcenetonmacandlinuxandvisualstudiocommunity.aspx), and [already has multi-platform intellisense](http://www.hanselman.com/blog/OmniSharpMakingCrossplatformNETARealityAndAPleasure.aspx
). 

> We are building a .NET Core CLR for Windows, Mac and Linux and it will be both open source and it will be supported by Microsoft. It'll all happen at https://github.com/dotnet.

> ASP.NET 5 will work everywhere.
> ASP.NET 5 will be available for Windows, Mac, and Linux. Mac and Linux support will come soon and it's all going to happen in the open on GitHub at https://github.com/aspnet.

### Server-side graphics - the present

We've all seen this:

> "Classes within the System.Drawing namespace are not supported for use
within a Windows or ASP.NET service. Attempting to use these classes
from within one of these application types may produce unexpected
problems, such as diminished service performance and run-time
exceptions. For a supported alternative, see Windows Imaging
Components."
http://msdn.microsoft.com/en-us/library/system.drawing(v=vs.110).aspx

And ignored it. It might have been more effective with some detail, namely *"GDI+ (and therefore System.Drawing) has [process-wide locks everywhere](http://stackoverflow.com/questions/3719748/parallelizing-gdi-image-resizing-net)"*. 

We might have ignored it because [Windows Imaging Components (WIC)](http://msdn.microsoft.com/en-us/library/windows/desktop/ee719654(v=vs.85).aspx) lacked [MTA support](http://stackoverflow.com/questions/485086/single-threaded-apartments-vs-multi-threaded-apartments#485109) until Windows Server 2008 R2.

In 2007, Microsoft published a [minimal set of (broken, memory leaking) interop classes for using WIC from .NET](http://code.msdn.microsoft.com/wictools). It's now a broken link, as the gallery was later retired. As far as I can tell, that has been the extent of 'using WIC from .NET' documentation. [insert meme here].

WPF, which wraps WIC and DirectX, [is also unsupported for use in ASP.NET applications](http://weblogs.asp.net/bleroy/the-fastest-way-to-resize-images-from-asp-net-and-it-s-more-supported-ish). It's certainly more popular with ASP.NET developers (via [DynamicImage](https://github.com/tgjones/dynamic-image), notably) than WIC, but System.Drawing usage dwarfs them both. Most major memory leaks were finally patched in 2012, so WPF has fewer gotchas than System.Drawing.

WPF's key flaw is that it lacks high-quality image scaling. If it *was* to expose the high-quality scaling [provied in recent versions of Direct2D](https://github.com/imazen/GdiBench/blob/master/GdiBench/Direct2D.cs#L82), it would be far slower than System.Drawing - regardless of threading configuration. Currently, it only exposes the inaccurate averaging (or cubic partial sampling) algorithms from WIC, which are fast but provide unacceptable quality for product photos.

**Client-side graphics libraries (like WIC, WPF, Cairo, System.Drawing, etc) will always be making the wrong trade-offs for a server-side context. They favor response-time over throughput, features over security (usually), and make dozens of invalid assumptions (such as output color space and the threading context).**


### Will anyone pick up the ball?

I've found that developers (both inside and outside of MSFT) will wait until something actually breaks on *their machine* to care. There's just too much other stuff to worry about if it 'works for me'. 

System.Drawing uses a *process wide lock* - not AppDoman, worker-process-wide. If you're doing imaging, you now have 1 CPU core. 
That's a hosting density killer. System.Drawing has always been unsupported on ASP.NET, but it has always been *widely used* - because it could be kind of sort of made to work for some things, and because there is NO competitive alternative. Adding more sticks of RAM, turning up the Web Garden count, and taking the massive cache miss and I/O hit is apparently par for the course. I'm desperately hoping that now Microsoft is in the hosting business, that they'll eventually decide this waste is unacceptable.

However, The current level of pain hasn't been enough to provoke an actual solution yet. So until the house is on fire (I.e, System.Drawing and WPF throw NotSupportedExceptions when called from ASP.NET), I don't anticipate community-driven progress to a solution. 


### What my team has done so far

Over the last 7 years @nathanaeljones (and recently, others contracting for @Imazen, including @ecerta, @suetanvil, @tostercx, @ddobrev, @avasp, and @ydanila) have tried to improve the state of server-side imaging for .NET, writing safer (OSS) wrappers for System.Drawing & WIC, creating (or updating) interop layers for other native libraries (WebP, FreeImage), contributing to underlying libraries, and trying to educate developers about common mistakes and memory leaks in ASP.NET imaging.

Here are some of our efforts:

* Created [ImageResizer](http://imageresizing.net) - Mutli-backend (SysDrawing/WIC/FreeImage) image processing framework with 40+ plugins. Includes a high-performance HttpModule with an easy URL-based API. [Apache 2/AGPL licensed](https://github.com/imazen/resizer/blob/develop/LICENSE.md). 
* Created [LightResize](https://github.com/imazen/lightresize) embeddable safe wrapper for System.Drawing.
* Improved [Sharpen, a Java->C# conversion tool](https://github.com/imazen/sharpen/tree/commandline). We refactored it to eliminate its centeral dependency of Eclipse V.Ancient, allowing it to operate on a CI server and as a simple command-line tool. We're also improving the output quality with several improvements. 
* [Ported MetadataExtractor to C#](https://github.com/imazen/n-metadata-extractor) - Did you know there were *no* cross-platform metadata readers for .NET? All wrapped Windows APIs instead.
* Created a [managed wrapper for libwebp](https://github.com/imazen/libwebp-net)
* Created [Slimmage.js - A lightweight responsive images](https://github.com/imazen/slimage) and improved [SlimResponse](https://github.com/imazen/slimresponse), an ASP.NET output filter that makes responsive images and efficient web sites trivially easy.
* Refined cross-platform build, recursive dependency fetching, and Windows CI scripts to high-profile imaging projects, so that Windows can become a first-class target. Only possible because of AppVeyor (Feodor Fitsner).  We've started with [zlib](https://github.com/imazen/zlib), [libiconv](https://github.com/imazen/libiconv), [freetype](https://github.com/imazen/freetype), [libpng](https://github.com/imazen/libpng), [libjpeg-turbo](https://github.com/imazen/libjpeg-turbo), [libtiff](https://github.com/imazen/libtiff), [libwebp](https://github.com/imazen/libwebp), [libraw](https://github.com/imazen/LibRaw), [openjpeg](https://github.com/imazen/openjpeg), [libgd](github.com/imazen/gd-libgd) and [freeimage](https://github.com/imazen/freeimage). This is not easy.
* Updated and re-released [NuGet.Bootstrapper](https://github.com/imazen/Nuget.Bootstrapper) so that it could work properly in a CI environment.
* Greatly improved the speed and safety of image scaling in [libgd](github.com/imazen/gd-libgd). 
* Improved [CppSharp](https://github.com/ddobrev/CppSharp), the only software that can generate C# bindings for both C++ and C code. 
* [Helped get AForge on NuGet](https://github.com/nathanaeljones/AForge.Nuget)
* [Created automated low-level bindings generator for libgd](https://github.com/imazen/gd-dotnet-bindings-generator) to run as part of CI process. 
* [Brought FreeImage from CVS to GitHub](https://github.com/imazen/freeimage) in order to provided cross-platform CI and automated builds, fast security patching, and reduced attack surface varants. 

All of our income @imazen goes back into open-source development, but we can't do this alone.

### What is still needed

[work in progress]


