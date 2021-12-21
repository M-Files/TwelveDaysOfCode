using MFiles.Extensibility.Framework.IntelligenceServices;
using MFiles.Extensibility.IntelligenceServices;
using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExifData
{
    /// <summary>
    /// Metadata Provider.
    /// </summary>
    public class MyIntelligenceProvider :
        MarshalByRefObject,
        IIntelligenceProvider
    {
        #region Default implementations (do not typically need to be edited)

        /// <summary>
        /// The metadata structure details for the provider.
        /// </summary>
        private IntelligenceServiceConfiguration<MyConfiguration> Configuration { get; set; }

        /// <summary>
        /// Instance name. 
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Returns the declaration part of the intelligence service configuration to M-Files.
        /// </summary>
        public IntelligenceServiceDeclaration Declaration => this.Configuration.Declaration;

        /// <summary>
        ///  Can this intelligence provider extract metadata?
        /// </summary>
        public bool CanExtractMetadata => true;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the provider instance.</param>
        /// <param name="configuration">The metadata structure details for the provider.</param>
        /// <param name="hasConfigurationErrors">True, if the validation found errors from the configuration.</param>
        public MyIntelligenceProvider(
            string name,
            IntelligenceServiceConfiguration<MyConfiguration> configuration,
            bool hasConfigurationErrors
        )
        {
            // Set instance name.
            this.Name = name;

            // Save configurations.
            this.Configuration = configuration;
        }

        #endregion

        /// <summary>
        /// Gets the metadata based on the input.
        /// </summary>
        /// <param name="operationContext">Provides contextual information for the request.</param>
        /// <param name="request">The metadata request details.</param>
        /// <returns>The resulting metadata.</returns>
        public MetadataResult ExtractMetadata(
            MFiles.Extensibility.Applications.IOperationContext operationContext,
            MetadataRequest request
        )
        {
            var result = new MetadataResult();

            // TODO: Populate the metadata result.
            System.Diagnostics.Debugger.Launch();

            // The file data can be found in request.FileContents.
            foreach (var file in request.FileContents)
            {
                // Read the data.
                var metadata = MetadataExtractor.ImageMetadataReader.ReadMetadata(file.GetFileStream());

                // Extract the exif and gps data.
                var exifDirectory = metadata.OfType<MetadataExtractor.Formats.Exif.ExifSubIfdDirectory>().FirstOrDefault();
                var gpsDirectory = metadata.OfType<MetadataExtractor.Formats.Exif.GpsDirectory>().FirstOrDefault();

                // Do we have the date it was taken?
                {
                    var value = exifDirectory?.GetDescription(MetadataExtractor.Formats.Exif.ExifSubIfdDirectory.TagDateTimeOriginal);
                    if (DateTime.TryParseExact(value, "yyyy:MM:dd HH:mm:ss", System.Threading.Thread.CurrentThread.CurrentUICulture, System.Globalization.DateTimeStyles.None, out DateTime created))
                        result.Suggestions.Add(new DateTimeValueMetadataSuggestion(Terms.PhotoTaken, created.ToUniversalTime(), 1));
                }

                // Do we have the coordinates?
                {
                    try
                    {
                        var latitude = gpsDirectory?.GetDescription(MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitude)?.ConvertToDecimal();
                        var longitude = gpsDirectory?.GetDescription(MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitude)?.ConvertToDecimal();
                        var latitudeRef = gpsDirectory?.GetDescription(MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitudeRef);
                        var longitudeRef = gpsDirectory?.GetDescription(MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitudeRef);

                        if(latitude.HasValue)
                            //result.Suggestions.Add(new TextValueMetadataSuggestion(Terms.Latitude, latitude.Value.ToString(), 1)); // May be more accurate
                            result.Suggestions.Add(new DoubleValueMetadataSuggestion(Terms.Latitude, Decimal.ToDouble(latitude.Value), 1));
                        if (longitude.HasValue)
                            //result.Suggestions.Add(new TextValueMetadataSuggestion(Terms.Longitude, longitude.Value.ToString(), 1)); // May be more accurate
                            result.Suggestions.Add(new DoubleValueMetadataSuggestion(Terms.Longitude, Decimal.ToDouble(longitude.Value), 1));

                        if (latitude.HasValue & longitude.HasValue)
                            result.Suggestions.Add(new TextValueMetadataSuggestion(Terms.Url, $"http://www.google.com/maps/place/{latitude},{longitude}", 1));
                    }
                    catch
                    {

                    }

                }


            }

            return result;
        }
    }

    internal static class GpsConverters
    {
        public static decimal ConvertToDecimal(this string gpsValue)
        {
            // There are a number of types of latitude/longitude format that could be stored.  We'll deal with just the single
            // one that this image has.
            {
                if (false == string.IsNullOrWhiteSpace(gpsValue))
                {
                    var split = gpsValue.Split(" ".ToCharArray());
                    if (split.Length == 3)
                    {
                        // Process individual bits.
                        var degrees = Decimal.Parse(split[0]?.Replace("°", "")?.Trim());
                        var minutes = Decimal.Parse(split[1]?.Replace("'", "")?.Trim());
                        var seconds = Decimal.Parse(split[2]?.Replace("\"", "")?.Trim());

                        // Add and return.
                        return degrees + (minutes / 60) + (seconds / 3600);
                    }
                }
            }

            // Not a format we expect.
            throw new ArgumentException("GPS value format not as expected.");
        }
    }

}
