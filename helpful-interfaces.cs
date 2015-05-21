  /// <summary>
    /// Provides a lossless interface to any underlying representation of a querystring. Neither Dictionary nor NameValueCollection can provide this. 
    /// </summary>
    public interface IQuerystring
    {
        /// <summary>
        /// Enumerates the values of all pairs with the given querystring key. Key lookup should be case sensitive, Ordinal. Returns null if key doesn't exist. Returns an empty string if the key is used, but has a blank value.  Keys and values are in URL decoded form.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEnumerable<string> GetQueryValues(string key);

        /// <summary>
        /// Returns either the first value associated with the pair, or a comma-delimited list. If you wish to handle duplicate keys properly, use GetQueryValues. Provided for performance. Returns null if key doesn't exist. Returns an empty string if the key is used, but has a blank value. Keys and values are in URL decoded form.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetQueryValue(string key);

        /// <summary>
        /// Returns the querystring in the form of string-string pairs. Keys are not guaranteed to be unique. Values may be empty strings, but should never be null. Keys and values are in URL decoded form.
        /// </summary>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string, string>> GetQueryPairs();
    }
    
      /// <summary>
    /// Provides a way to edit a querstring in a lossless manner via SetQueryPairs
    /// </summary>
    public interface IMutableQuerystring : IQuerystring
    {

        /// <summary>
        /// To delete the entire pair, provide a value of null. 
        /// In the case of pairs with duplicate keys, only one pair will be retained and modified. Keys and values are in URL decoded form.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        void SetQueryValue(string key, string newValue);

        /// <summary>
        /// Replaces the entire querystring with the given set. Keys and values are in URL decoded form.
        /// </summary>
        /// <param name="pairs"></param>
        void SetQueryPairs(IEnumerable<KeyValuePair<string, string>> pairs);
    }
    
    //Interfaces beyond this point are unproven.
    
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Does not allow upside-down (win bmp, freeimage) representation. Vertical flip during lock/unlock if needed.
    /// </remarks>
    public interface IBitmapRegion : ITrackable
    {
        /// <summary>
        /// The width of the region or bitmap, in pixels
        /// </summary>
        int Width { get; }
        /// <summary>
        /// The height of the region or bitmap, in pixels; I.e, the number of scan rows.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// The byte length of each row  (including unused padding, common for alignment or crop purposes)
        /// </summary>
        int Stride { get; }

        /// <summary>
        /// Pointer to the first byte in the region; should be of length > h * stride
        /// </summary>
        IntPtr Byte0 { get; }

        /// <summary>
        /// The number of bytes we are permitted to access following Pixel0. Should be >= Stride * Height
        /// </summary>
        long ByteCount { get; }

        /// <summary>
        /// If true, we may modify any pixels
        /// </summary>
        bool PixelsWriteable { get; }

        /// <summary>
        /// If true, we may modify any padding bytes between rows (equivalent to stride_readonly)
        /// </summary>
        bool PaddingWriteable { get; }

        /// <summary>
        /// The number of bytes per pixel, or -1 if pixels do not align to byte boundaries
        /// </summary>
        int BytesPerPixel { get; }

        /// <summary>
        /// The pixel format
        /// </summary>
        IPixelFormat Format { get; }

        /// <summary>
        /// Notify the implementation that pixels have been changed, and may need to be saved (if this is a temporary buffer).
        /// </summary>
        void MarkChanged();

        /// <summary>
        /// Closes the region, potentially causing any changes to be 
        /// copied back into the parent frame (if an intermediate buffer was required).
        /// Changes may be lost if MarkChanged() was not called.
        /// </summary>
        void Close();
    }
    
    public enum FrameDimension
    {
        Time,
        Page,
        Resolution, 
        ZAxis
    }
    
    public interface IBitmapContainer : ITrackable
    {
        /// <summary>
        /// Returns the number of frames (in each dimension) provided it this image. TIFF and volumetric images may be multi-dimensional.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Tuple<FrameDimension, long>> GetFrameCounts();

        /// <summary>
        /// Opens the given frame for access. Does not provide any thread safety.
        /// </summary>
        /// <param name="frameIndicies">Provide null to access the first frame</param>
        /// <returns></returns>
        IBitmapFrame OpenFrame(IEnumerable<Tuple<FrameDimension, long>> frameIndicies);

        /// <summary>
        /// Returns true if it is permissible to call OpenFrame. 
        /// Some implementations only permit one frame to be opened at a time.
        /// </summary>
        bool CanOpenFrame { get; }

    }
    
    public interface IBitmapFrame: ITrackable
    {
        /// <summary>
        /// The width of the region or bitmap, in pixels
        /// </summary>
        int Width { get; }
        /// <summary>
        /// The height of the region or bitmap, in pixels; I.e, the number of scan rows.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// The pixel data format
        /// </summary>
        IPixelFormat PixelFormat { get; }

        /// <summary>
        /// Makes a portion of the frame accessible in a contiguous buffer at a fixed point in memory. 
        /// Does not guarantee thread safety.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        IBitmapRegion OpenRegion(int x, int y, int w, int h, RegionAccessMode accessMode);

        /// <summary>
        /// Returns true if it is permissible to call OpenRegion. 
        /// Some implementations only permit one region per frame (or per bitmap!) to be opened at a time.
        /// This may be an expensive operation, especially if an intermediate buffer is required. Changes may not take effect until the region is Flushed.
        /// </summary>
        bool CanOpenRegion { get; }


        /// <summary>
        /// Rendering hints 
        /// </summary>
        IGraphicsHints Hints { get; }


        ///// <summary>
        ///// Checks if it is possible to change the dimensions of the frame - in place. 
        ///// </summary>
        ///// <returns></returns>
        //bool CanMutateDimensions();

        ///// <summary>
        ///// After rotating a frame by mutating the entire region, this can be used
        ///// to update the stride and dimensions to match.
        ///// May cause replacement of the underlying buffer or bitmap.
        ///// </summary>
        ///// <param name="w"></param>
        ///// <param name="h"></param>
        ///// <param name="stride"></param>
        //void MutateDimensions(int w, int h, int stride);
    }
    public interface IPixelFormat
    {
        /// <summary>
        /// Represents a pixel layout. 
        /// Predictable bit significance; sytem byte order (endianess). 
        /// Use for algorithms that work with pixels in their 'common' data type, such as 'int' for BGRA.
        /// I.e, bit masks or bit shifting is used to extract or set individual channels.
        /// </summary>
        BitmapPixelFormats BitwiseFormat { get; }

        /// <summary>
        /// Represents a pixel layout. 
        /// Predictable byte order (endianess); system bit significance.
        /// Use for algorithms that access bytes 
        /// </summary>
        BitmapPixelFormats BytewiseFormat { get; }

        /// <summary>
        /// The number of bits per pixel.
        /// </summary>
        int BitsPerPixel { get; }
    }
    
    public interface ITrackable
    {
        /// <summary>
        /// Read the ITrackingScope managing the lifetime of this object. May be null. 
        /// Write access is for exclusive use by ITrackingScope implementations. 
        /// Changing this directly will trigger an InvalidOperationException at a later time.
        /// </summary>
        ITrackingScope TrackingScope { get; set; }

        /// <summary>
        /// May be called repeatedly; should release associated resources
        /// </summary>
        void ReleaseResources();
    }
    public enum BitmapCompositingMode
    {
        Replace_self = 0,
        Blend_with_self = 1,
        Blend_with_matte = 2
    };


    public interface IGraphicsHints
    {

        //gamma
        //ICC profile


        /// <summary>
        /// If other images are drawn onto this canvas region, this setting controls how they will be composed.
        /// </summary>
        BitmapCompositingMode Compositing { get; set; }

        /// <summary>
        /// Gets the matte color to use when compositing (Blend_with_matte). If null, treat as transparent.
        /// </summary>
        /// <returns></returns>
        byte[] GetMatte();

        /// <summary>
        /// Changes the matte color to use when compositing (Blend_with_matte).
        /// Blend color should be in same pixel format as canvas.
        /// </summary>
        /// <param name="color"></param>
        void SetMatte(byte[] color);

        /// <summary>
        /// Indicates meaningful data in the alpha channel. 
        /// If true, the alpha channel should be honored when present.
        /// </summary>
        bool RespectAlpha { get; }

        /// <summary>
        /// Results in RespectAlpha being set to true.
        /// </summary>
        void MarkAlphaUsed();

        //TODO: allowreuse?

    }
