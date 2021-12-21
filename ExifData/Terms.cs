namespace ExifData
{
    /// <summary>
    /// Terms are used when returning suggested properties.
    /// This class is simply used to declare the terms as constants, rather than
    /// having strings manually typed everywhere.
    /// </summary>
    internal static class Terms
    {
        // Terms are used when returning suggested properties.
        // TODO: Add terms as constants here.
        internal const string PhotoTaken = "Photo Taken";
        internal const string Latitude = "Latitude";
        internal const string Longitude = "Longitude";
        internal const string Url = "Url";

        public static string[] All =
        {
			Terms.PhotoTaken,
            Terms.Latitude,
            Terms.Longitude,
            Terms.Url
        };
    }
}
