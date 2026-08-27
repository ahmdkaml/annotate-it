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
        /// Solid ink drawing mode.
        /// </summary>
        Drawing,

        /// <summary>
        /// Semi-transparent highlighter drawing mode.
        /// </summary>
        Highlighter,

        /// <summary>
        /// Laser pointer mode: Ephemeral ink stroke that automatically fades out after a delay.
        /// </summary>
        LaserPointer,

        /// <summary>
        /// Point eraser mode: Erases individual segments of strokes touched by the cursor.
        /// </summary>
        PointEraser,

        /// <summary>
        /// Stroke eraser mode: Erases entire stroke paths touched by the cursor.
        /// </summary>
        StrokeEraser
    }
}
