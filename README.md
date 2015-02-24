Server-side graphics in ASP.NET - the present and future.
==============

TLDR; ASP.NET developers presently use the closed-source System.Drawing and WPF libraries, which (for excellent reasons, like global locks and GC issues) are **unsupported** for sever-side use.

Neither will work in .NET Core, let alone cross-platform. System.Drawing is a GDI+ wrapper, and WPF is a  WIC/DirectX 9 wrapper with a bit of glue. There are [no plans to open-source or port  WPF](http://channel9.msdn.com/Blogs/DevRadio/The-Future-of-WPF).

.NET lacks a *server-friendly* graphics/imaging library, especially on Windows. Let's build one.

Unless we start now, there will be **NO** image-processing story ready when ASP.NET vNext and .NET Core reach stable status. Given the popularity of images inside webapps, this might affect adoption. 

**We're actively looking for companies to help sponsor the replacement, libgd.net** 
We've invested as much time and money as we've had available for the last two years (and got a ton of stuff done, see end of page), but it's not enough. We're still 6 aggressive months away from beta and 12 months away from stable, and we've had to stop development for funding reasons.

Among the improvements we can bring to the table - better quality and 8-22x higher throughput compared to System.Drawing.Bitmap.DrawImage. If you spend a lot on server infrastructure, or have a business model (like e-commerce) that is affected by image quality, you should [contact us](http://www.imazen.io/contact). Our algorithms can process 1.9 gigapixels per second on a laptop VM; if moved to the YCbCr colorspace and flattened with the decoder, we suspect 3+ gigapixels/second to be possible. For a CDN or search engine, we would expect this to provide immediate ROI. 

## The .NET Future is open-source and cross-platform

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

WPF's key flaw is that it lacks high-quality image scaling. If it *was* to expose the high-quality scaling [provided in recent versions of Direct2D](https://github.com/imazen/GdiBench/blob/master/GdiBench/Direct2D.cs#L82), it would be far slower than System.Drawing - regardless of threading configuration. Currently, it only exposes the inaccurate averaging (or cubic partial sampling) algorithms from WIC, which are fast but provide unacceptable quality for product photos.

**Client-side graphics libraries (like WIC, WPF, Cairo, System.Drawing, etc) will always be making the wrong trade-offs for a server-side context. They favor response-time over throughput, features over security (usually), and make dozens of invalid assumptions (such as output color space and the threading context).**


## Will anyone pick up the ball?

I've found that developers (both inside and outside of MSFT) will wait until something actually breaks on *their machine* to care. There's just too much other stuff to worry about if it 'works for me'. 

System.Drawing uses a *process wide lock* - not AppDoman, worker-process-wide. If you're doing imaging, you now have 1 CPU core. 
That's a hosting density killer. System.Drawing has always been unsupported on ASP.NET, but it has always been *widely used* - because it could be kind of sort of made to work for some things, and because there is NO competitive alternative. Adding more sticks of RAM, turning up the Web Garden count, and taking the massive cache miss and I/O hit is apparently par for the course. I'm desperately hoping that now Microsoft is in the hosting business, that they'll eventually decide this waste is unacceptable.

However, the current level of pain hasn't been enough to provoke an actual solution yet. So until the house is on fire (I.e, System.Drawing and WPF throw NotSupportedExceptions when called from ASP.NET), I don't anticipate community-driven progress to a solution. 


### What my team has done so far

Over the last 7 years @nathanaeljones (and recently, others contracting for @Imazen, including @ecerta, @suetanvil, @tostercx, @ddobrev, @avasp, and @ydanila) have tried to improve the state of server-side imaging for .NET, writing safer (OSS) wrappers for System.Drawing & WIC, creating (or updating) interop layers for other native libraries (WebP, FreeImage), contributing to underlying libraries, and trying to educate developers about common mistakes and memory leaks in ASP.NET imaging.

Here are some of our efforts:p

* Created [ImageResizer](http://imageresizing.net) - Mutli-backend (SysDrawing/WIC/FreeImage) image processing framework with 40+ plugins. Includes a high-performance HttpModule with an easy URL-based API. [Apache 2/AGPL licensed](https://github.com/imazen/resizer/blob/develop/LICENSE.md). 
* Created [LightResize](https://github.com/imazen/lightresize) embeddable safe wrapper for System.Drawing.
* Improved [Sharpen, a Java->C# conversion tool](https://github.com/imazen/sharpen/tree/commandline). We refactored it to eliminate its central dependency of an ancient version of Eclipse, allowing it to operate on a CI server and as a simple command-line tool. We're also improving the output quality with several improvements. 
* [Ported MetadataExtractor to C#](https://github.com/imazen/n-metadata-extractor) - Did you know there were *no* cross-platform metadata readers for .NET? All wrapped Windows APIs instead.
* Created a [managed wrapper for libwebp](https://github.com/imazen/libwebp-net)
* Created [Slimmage.js - A lightweight responsive images](https://github.com/imazen/slimage) and improved [SlimResponse](https://github.com/imazen/slimresponse), an ASP.NET output filter that makes responsive images and efficient web sites trivially easy.
* Refined cross-platform build, recursive dependency fetching, and Windows CI scripts to high-profile imaging projects, so that Windows can become a first-class target. Only possible because of AppVeyor (Feodor Fitsner).  We've started with [zlib](https://github.com/imazen/zlib), [libiconv](https://github.com/imazen/libiconv), [freetype](https://github.com/imazen/freetype), [libpng](https://github.com/imazen/libpng), [libjpeg-turbo](https://github.com/imazen/libjpeg-turbo), [libtiff](https://github.com/imazen/libtiff), [libwebp](https://github.com/imazen/libwebp), [libraw](https://github.com/imazen/LibRaw), [openjpeg](https://github.com/imazen/openjpeg), [libgd](github.com/imazen/gd-libgd) and [freeimage](https://github.com/imazen/freeimage). This is not easy.
* Updated and re-released [NuGet.Bootstrapper](https://github.com/imazen/Nuget.Bootstrapper) so that it could work properly in a CI environment.
* Greatly improved the speed and safety of image scaling in [libgd](github.com/imazen/gd-libgd). 
* Improved [CppSharp](https://github.com/ddobrev/CppSharp), the only software that can generate C# bindings for both C++ and C code. 
* [Helped get AForge on NuGet](https://github.com/nathanaeljones/AForge.Nuget)
* [Convinced the awesome folks at Bitmiracle to put LibTiff.NET and LibJpeg.NET on GitHub and Nuget](https://github.com/BitMiracle) 
* [Created automated low-level bindings generator for libgd](https://github.com/imazen/gd-dotnet-bindings-generator) to run as part of CI process. We [first created manual bindings](https://github.com/imazen/libgd-net), but realized an automated generation layer would let us catch libgd ABI changes at build time instead of runtime.
* [Brought FreeImage from CVS to GitHub](https://github.com/imazen/freeimage) in order to provided cross-platform CI and automated builds, fast security patching, and reduced attack surface variants. 

All of our income @imazen goes back into open-source development, but we can't do this alone.

## What is still needed

[We looked at nearly 100 libraries](https://github.com/nathanaeljones/imaging-wiki), and found LibGD to be the only imaging library specifically designed for server-side use. It has the added benefit of already being used on millions of servers as a core part of PHP, and being written in simple and approachable ANSI C.

We've put a lot of work into both libgd and the .NET wrapper over the last 2 years, but there's a long way to go before the the combination could be considered production ready. 

### In libgd itself:

* Integrate our high-quality, high-performance image scaling algorithm from ImageResizer. This will yield visual quality better than System.Drawing, while providing 430-2200% better performance (yes, you read that right). We figured out a pretty cool technique for memory structure pivoting that solves memory locality. No SIMD or assembly required after all, although we do hand-unroll loops.
* Upgrade from 7 to 8-bit alpha component
* Allow enforced contiguous memory bitmaps
* Implement format detection based on magic bytes instead of file extensions. 
* Extend API to offer lower-level integration with libjpeg/libjpeg-turbo; we can further reduce RAM requirements by performing some operations during decoding. 
* Make error handling consistent and easily map to both .NET and PHP wrappers.
* Spend some quality time with valgrind. PHP is forgiving of memory leaks. .NET is not.
* Improve test coverage

### In the wrapper

* Design intuitive abstractions and classes, then map the several hundred operations to them. Translate error conditions on every one.
* Increase test coverage from < 20% to 100%.
* Make both automated and explicit memory management seamless
* Fully support .NET streams (vs. fopen/fclose/pathname)

### In the ecosystem/tooling

Native binaries are prohibitively painful to work with when dealing with more than one architecture or platform. We must make this better, or LibGD.NET and everything like it will have limited adoption. 

Ideally?

* Conditional/platform-architecture-specific references, in projects, .NET assemblies, and in NuGet packages. Preferable without hand-editing XML.
* Conditional nuget references, in particular, are essential. Building from source on Windows is a non-starter. Fat packages (or binaries) are insufficient on their own. Imaging distributing something huge like OpenCV. Packaging 3 architectures x 3 platforms x 180MB would make for a 1.6GB nuget package. 
* Architecture-aware assembly loaders. Skip assemblies if they're not in the right format; don't fail with BadFormatException. We can't prevent all runtime incompatibilities, but 99% are easily solved.
* First-class native dependencies for managed assemblies.
* A standard bin subfolder convention, respected by Visual Studio, NuGet, the ASP.NET loader, testing tools, and the .NET probe path. We should be able to take an immutable directory tree and execute tests under a variety of runtime modes - WoW64, emulation, etc. We should be able to switch between 32 and 64 bit application pools without fear. .NET set the expectation of architecture-agnosticism, and it should be upheld consistently.
* Flexible hooks for overriding search paths for both managed and native DLLs, or injecting runtime binary fetching. We could fix most of this ourselves with permissive APIs. 


Practically?

There are a lot of issues that make it hard to monkey-patch around, although [we're trying](https://github.com/imazen/Imazen.NativeDependencyManager) - [very hard](https://github.com/imazen/paragon). 

* We need an open-source, cross-platform assembly parser [[built it, check](https://github.com/imazen/Imazen.NativeDependencyManager)].
* We need an injectable replacement loader for ASP.NET websites, so that we can load the right version of any arch-specific managed assemblies. This will uglify Web.config, but even PreApplicationStart is too late to help. [in progress] 
* We need a replacement assembly loader for non-web projects. This is more difficult, as AssemblyResolve is only called when there is no matching dll. Do we ask for an empty bin folder?
* Come help us and track progress at [Imazen.NativeDependencyManager](https://github.com/imazen/Imazen.NativeDependencyManager).
* 

#### Also, some standard point/rectangle data structures in the BCL would be nice. 

### What about Cairo and Mono System.Drawing?

Client-side graphics libraries make the wrong tradeoffs for our context. This means

* Increased overhead
* increased attack surface area
* Integration with the GPU, device, and display layers makes unit testing harder and less likely. We only need byte[].
* Focus on the wrong performance metrics

Compatibility with System.Drawing will be important for porting apps to vNext, but System.Drawing is a poor end goal. 

We **intend** to contribute to Mono System.Drawing. There is much from ImageResizer and libgd that could help fill gaps in the API. We hope Mono System.Drawing has a well-supported future in vNext. We're tryng to help [fix the native dependency nightmare](https://github.com/imazen/Imazen.NativeDependencyManager) so that it will be more practical for it to appear on NuGet.

But the scope of both Cairo and System.Drawing *overlaps very poorly* with what is most needed in a server context. LibGD is of managable size, scope, and complexity, and would seem to be a better use of our resources.





