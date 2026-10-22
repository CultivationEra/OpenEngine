using System;

namespace GameEngine.Scripting
{
    /// <summary>
    /// An element's drawn box in window space, in CSS logical px — the same space a pointer event reports.
    /// </summary>
    /// <remarks>
    /// Through every transform on the element and its ancestors, as the axis-aligned bounds of the drawn box:
    /// the engine's equivalent of a browser's <c>getBoundingClientRect</c>. This is what a tooltip, a menu or a
    /// drag anchors to, so that no caller has to re-derive a layout the document already solved.
    /// </remarks>
    public readonly record struct UiRect(float X, float Y, float Width, float Height)
    {
        /// <summary>The right edge, one past the last pixel this box covers.</summary>
        public float Right => X + Width;

        /// <summary>The bottom edge, one past the last pixel this box covers.</summary>
        public float Bottom => Y + Height;

        /// <summary>Whether a window-space point falls inside the box. Edges count as inside.</summary>
        public bool Contains(float x, float y) => x >= X && x <= Right && y >= Y && y <= Bottom;
    }
}
