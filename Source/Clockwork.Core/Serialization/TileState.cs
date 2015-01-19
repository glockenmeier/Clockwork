
namespace Clockwork.Serialization
{
    /// <summary>
    /// Represents the state of a content tile.
    /// </summary>
    public enum TileState
    {
        /// <summary>
        /// The tile is neither loaded nor needed.
        /// </summary>
        None,

        /// <summary>
        /// The tile was observed and needs loading.
        /// </summary>
        Observed,

        /// <summary>
        /// The tile is currently loading.
        /// </summary>
//        Loading,

        /// <summary>
        /// The tile was loaded, but not yet mapped.
        /// </summary>
//        Loaded,

        /// <summary>
        /// The tile is loaded and ready to use.
        /// </summary>
        Mapped
    }
}
