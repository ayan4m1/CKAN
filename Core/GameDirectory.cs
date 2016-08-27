namespace CKAN
{
    public enum GameDirectory
    {
        /// <summary>
        /// GameData subdirectory of an instance
        /// </summary>
        GameData,
        /// <summary>
        /// CKAN subdirectory of an instance
        /// </summary>
        CkanDir,
        /// <summary>
        /// Shared temporary directory
        /// </summary>
        TempDir,
        /// <summary>
        /// Download cache directory
        /// </summary>
        DownloadCacheDir,
        /// <summary>
        /// Root ships directory
        /// </summary>
        Ships,
        /// <summary>
        /// VAB Ships
        /// </summary>
        ShipsVertical,
        /// <summary>
        /// SPH Ships
        /// </summary>
        ShipsHorizontal,
        /// <summary>
        /// Root ship thumbnails directory
        /// </summary>
        ShipsThumbs,
        /// <summary>
        /// Thumbnails for VAB Ships
        /// </summary>
        ShipsThumbsVertical,
        /// <summary>
        /// Thumbnails for SPH Ships
        /// </summary>
        ShipsThumbsHorizontal,
        /// <summary>
        /// Tutorial directory
        /// </summary>
        Tutorial,
        /// <summary>
        /// Custom scenario directory
        /// </summary>
        Scenarios
    }
}
