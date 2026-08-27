namespace AnnotateIt
{
    /// <summary>
    /// Defines the operational interaction modes for the Annotate It overlay.
    /// </summary>
    public enum AppMode
    {
        /// <summary>
        /// Normal desktop flow: Pointer clicks pass directly through to background applications.
        /// </summary>
        PassThrough,

        /// <summary>
        /// Annotation mode: Pointer clicks and strokes are captured by the overlay canvas.
        /// </summary>
        Drawing,

        /// <summary>
        /// Erasing mode: Pointer clicks interact with overlay annotations to delete them.
        /// </summary>
        Eraser
    }
}
