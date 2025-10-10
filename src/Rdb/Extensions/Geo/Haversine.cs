namespace codecrafters_redis.Rdb.Extensions.Geo;

public static class Haversine
{
    /// <summary>
    /// Calculates the great-circle distance between two points on Earth using the Haversine formula.
    /// </summary>
    /// <param name="lon1">Longitude of the first point in degrees.</param>
    /// <param name="lat1">Latitude of the first point in degrees.</param>
    /// <param name="lon2">Longitude of the second point in degrees.</param>
    /// <param name="lat2">Latitude of the second point in degrees.</param>
    /// <returns>The distance between the two points in meters.</returns>
    public static double Calculate(double lon1, double lat1, double lon2, double lat2)
    {
        // Earth's radius in meters (WGS-84 ellipsoid semi-major axis)
        const double earthRadiusInMeters = 6372797.5608568;

        // Calculate differences in latitude and longitude
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        lat1 = ToRadians(lat1);
        lat2 = ToRadians(lat2);

        // Haversine formula: calculate the square of half the chord length between the points
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

        // Calculate the angular distance in radians
        var c = 2 * Math.Asin(Math.Sqrt(a));

        // Convert to distance in meters
        return earthRadiusInMeters * c;
    }
    
    private static double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }
}