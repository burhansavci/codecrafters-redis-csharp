namespace codecrafters_redis.Rdb;

public static class GeoHashConverter
{
    private const double MinLongitude = -180;
    private const double MaxLongitude = 180;
    private const double LongitudeRange = MaxLongitude - MinLongitude;

    private const double MinLatitude = -85.05112878;
    private const double MaxLatitude = 85.05112878;
    private const double LatitudeRange = MaxLatitude - MinLatitude;

    /// <summary>
    /// Encodes geographic coordinates (WGS84) into a GeoHash code
    /// </summary>
    /// <param name="longitude">The longitude value (must be between -180 and 180)</param>
    /// <param name="latitude">The latitude value (must be between -85.05112878 and 85.05112878)</param>
    /// <returns>The encoded GeoHash as a long integer</returns>
    public static long Encode(double longitude, double latitude)
    {
        // Normalize to the range 0-2^26
        double normalizedLongitude = Math.Pow(2, 26) * (longitude - MinLongitude) / LongitudeRange;
        double normalizedLatitude = Math.Pow(2, 26) * (latitude - MinLatitude) / LatitudeRange;

        // Truncate to integers
        int normalizedLongitudeInt = (int)normalizedLongitude;

        int normalizedLatitudeInt = (int)normalizedLatitude;

        return Interleave(normalizedLatitudeInt, normalizedLongitudeInt);
    }

    /// <summary>
    /// Decode converts geo code (WGS84) to tuple of (latitude, longitude)
    /// </summary>
    /// <param name="geoCode">The encoded geographic code</param>
    /// <returns>Tuple containing (latitude, longitude)</returns>
    public static (double longitude, double latitude) Decode(long geoCode)
    {
        // Align bits of both latitude and longitude to take even-numbered position
        long y = geoCode >> 1;
        long x = geoCode;

        // Compact bits back to 32-bit ints
        int gridLongitudeNumber = CompactInt64ToInt32(y);
        int gridLatitudeNumber = CompactInt64ToInt32(x);

        return ConvertGridNumbersToCoordinates(gridLongitudeNumber, gridLatitudeNumber);
    }

    /// <summary>
    /// Interleaves the bits of two 32-bit integers into a single 64-bit integer
    /// </summary>
    /// <param name="x">The first integer whose bits will occupy even positions</param>
    /// <param name="y">The second integer whose bits will occupy odd positions</param>
    /// <returns>A 64-bit integer with interleaved bits</returns>
    private static long Interleave(int x, int y)
    {
        long spreadX = SpreadInt32ToInt64(x);
        long spreadY = SpreadInt32ToInt64(y);
        long yShifted = spreadY << 1;
        return spreadX | yShifted;
    }

    /// <summary>
    /// Spreads the bits of a 32-bit integer across a 64-bit integer by inserting zeros between each bit
    /// </summary>
    /// <param name="v">The 32-bit integer to spread</param>
    /// <returns>A 64-bit integer with spread bits</returns>
    private static long SpreadInt32ToInt64(int v)
    {
        long result = v & 0xFFFFFFFF;
        result = (result | (result << 16)) & 0x0000FFFF0000FFFF;
        result = (result | (result << 8)) & 0x00FF00FF00FF00FF;
        result = (result | (result << 4)) & 0x0F0F0F0F0F0F0F0F;
        result = (result | (result << 2)) & 0x3333333333333333;
        result = (result | (result << 1)) & 0x5555555555555555;
        return result;
    }


    /// <summary>
    /// Compact a 64-bit integer with interleaved bits back to a 32-bit integer.
    /// This is the reverse operation of <see cref="SpreadInt32ToInt64"/>.
    /// </summary>
    /// <param name="v">The 64-bit integer with spread bits to compact</param>
    /// <returns>A 32-bit integer with compacted bits</returns>
    private static int CompactInt64ToInt32(long v)
    {
        v &= 0x5555555555555555;
        v = (v | (v >> 1)) & 0x3333333333333333;
        v = (v | (v >> 2)) & 0x0F0F0F0F0F0F0F0F;
        v = (v | (v >> 4)) & 0x00FF00FF00FF00FF;
        v = (v | (v >> 8)) & 0x0000FFFF0000FFFF;
        v = (v | (v >> 16)) & 0x00000000FFFFFFFF;
        return (int)v;
    }

    /// <summary>
    /// Convert grid numbers back to geographic coordinates
    /// </summary>
    /// <param name="gridLatitudeNumber">Grid latitude number</param>
    /// <param name="gridLongitudeNumber">Grid longitude number</param>
    /// <returns>Tuple containing (latitude, longitude)</returns>
    private static (double longitude, double latitude ) ConvertGridNumbersToCoordinates(int gridLongitudeNumber, int gridLatitudeNumber)
    {
        // Calculate the grid boundaries
        double gridLongitudeMin = MinLongitude + LongitudeRange * (gridLongitudeNumber / Math.Pow(2, 26));
        double gridLongitudeMax = MinLongitude + LongitudeRange * ((gridLongitudeNumber + 1) / Math.Pow(2, 26));
        double gridLatitudeMin = MinLatitude + LatitudeRange * (gridLatitudeNumber / Math.Pow(2, 26));
        double gridLatitudeMax = MinLatitude + LatitudeRange * ((gridLatitudeNumber + 1) / Math.Pow(2, 26));

        // Calculate the center point of the grid cell
        double longitude = (gridLongitudeMin + gridLongitudeMax) / 2;
        double latitude = (gridLatitudeMin + gridLatitudeMax) / 2;

        return (longitude, latitude);
    }
}