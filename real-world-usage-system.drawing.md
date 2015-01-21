## List of real-world, server-side System.Drawing API use

Add the APIs from System.Drawing that you're using now - *for server-side tasks*. 
Much more of System.Drawing is relevant for a desktop app, and we aren't interested 
in that use case when **building abstractions or replacements, which is our purpose here**.

### Backlinks for context. 

When browsing a source code file on GitHub, press the 'y' key to switch to the permalink version. Then click a line number (or two, for a region) to place the corresponding URL in the address bar.
Use this URL to let people see the context of System.Drawing API use.


## class/structs used in entierety

* Point
* Size
* PointF
* Rectangle
* RectangleF
* ColorMatrix
* BitmapData

## Enumerations

* PixelFormat
* PaletteFlags

## class/structs we only use portions of

* Color
* Bitmap

## Members 

* Bitmap.LoadFrom(Stream s)
* Image.LoadFrom(Stream s, useIcm:false, validate:false) - Partially loads the image, good for accessing dimensions/format/metadata

## Usage examples

## Things we can abstract better (d
* ImageFormat
* Encoding details
* Metadata reading/writing (very broken right now)

## Specific questions

* Does anyone use metafile or icon-related APIs?
* Which members of ImageAttributes, Bitmap, Graphics, 

## Things that we should probably drop, unless a use case can be demonstrated

* Any kind of metafile support (EMF, WMF) and any members or enumerations specific to them.
* Icon support
* BufferedGraphics
* ImageAnimator
* The concept of system colors

