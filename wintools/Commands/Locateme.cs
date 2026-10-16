using System.Globalization;
using System.Runtime.CompilerServices;
using Windows.Devices.Geolocation;

namespace WinTools;

// LocateMe (locateme): print the current location from the Windows Location service (WinRT Geolocator:
// GPS where present, otherwise Wi-Fi positioning). Needs Settings > Privacy & security > Location with
// "Let desktop apps access your location" turned on.
static class Locateme
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("LocateMe", "Find your location using the Windows Location service", Run);

    static int Run(string[] args)
    {
        string format = null;
        bool longFormat = false, google = false;
        var timeout = 15.0;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine($$"""
                        USAGE: LocateMe [options]
                        Version: {{Commands.Version}}
                        Outputs your current location using the Windows Location service.
                        Options:
                          -h          This help message
                          -l          Long format output
                          -g          Generate a Google Map URL
                          -t secs     Give up after this many seconds (default 15)
                          -f format   Format output according to your arguments:
                                        {LAT}   Latitude
                                        {LON}   Longitude
                                        {ALT}   Altitude (meters)
                                        {SPD}   Speed (meters/second)
                                        {DIR}   Direction (degrees from north)
                                        {HAC}   Horizontal accuracy (meters)
                                        {VAC}   Vertical accuracy (meters)
                                        {TIME}  Timestamp
                                        {SRC}   Position source (Satellite, WiFi, IPAddress, ...)
                        Example: LocateMe -f "lat={LAT},lon={LON}"
                        """);
                    return 0;
                case "-l": longFormat = true; break;
                case "-g": google = true; break;
                case "-f": format = ++i < args.Length ? args[i] : throw new UsageException("-f needs a format"); break;
                case "-t":
                    timeout = ++i < args.Length && double.TryParse(args[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var t) && t > 0
                        ? t : throw new UsageException("-t needs seconds");
                    break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        Geoposition position;
        try
        {
            var access = Geolocator.RequestAccessAsync().AsTask().GetAwaiter().GetResult();
            if (access != GeolocationAccessStatus.Allowed)
                throw new InvalidOperationException("location access is off (Settings > Privacy & security > Location: turn on Location services and \"Let desktop apps access your location\")");
            var locator = new Geolocator { DesiredAccuracy = PositionAccuracy.High };
            position = locator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(timeout)).AsTask().GetAwaiter().GetResult();
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Runtime.InteropServices.COMException or TaskCanceledException)
        {
            throw new InvalidOperationException($"the location could not be determined ({e.Message.Trim()})");
        }
        var c = position.Coordinate;
        var p = c.Point.Position;
        string N(double? v, string f = "0.00") => (v ?? -1).ToString(f, CultureInfo.InvariantCulture);
        var time = c.Timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss +0000", CultureInfo.InvariantCulture);
        var altitude = c.Point.AltitudeReferenceSystem == AltitudeReferenceSystem.Unspecified ? (double?)null : p.Altitude;
        if (format != null)
        {
            Console.WriteLine(format.Replace("{LAT}", N(p.Latitude, "0.########")).Replace("{LON}", N(p.Longitude, "0.########"))
                .Replace("{ALT}", N(altitude)).Replace("{SPD}", N(c.Speed)).Replace("{DIR}", N(c.Heading)).Replace("{HAC}", N(c.Accuracy))
                .Replace("{VAC}", N(c.AltitudeAccuracy)).Replace("{TIME}", time).Replace("{SRC}", c.PositionSource.ToString())
                .Replace("\\n", "\n").Replace("\\t", "\t"));
        }
        else if (google)
            Console.WriteLine($"http://maps.google.com/maps?q={N(p.Latitude, "0.########")},{N(p.Longitude, "0.########")}");
        else if (longFormat)
        {
            Console.WriteLine($"Latitude: {N(p.Latitude, "0.########")}");
            Console.WriteLine($"Longitude: {N(p.Longitude, "0.########")}");
            Console.WriteLine($"Altitude (m): {N(altitude)}");
            Console.WriteLine($"Speed (m/s): {N(c.Speed)}");
            Console.WriteLine($"Direction: {N(c.Heading)}");
            Console.WriteLine($"Horizontal Accuracy (m): {N(c.Accuracy)}");
            Console.WriteLine($"Vertical Accuracy (m): {N(c.AltitudeAccuracy)}");
            Console.WriteLine($"Source: {c.PositionSource}");
            Console.WriteLine($"Timestamp: {time}");
        }
        else
            Console.WriteLine($"<{p.Latitude.ToString("+0.00000000;-0.00000000", CultureInfo.InvariantCulture)},{p.Longitude.ToString("+0.00000000;-0.00000000", CultureInfo.InvariantCulture)}> +/- {N(c.Accuracy)}m (speed {N(c.Speed)} mps / course {N(c.Heading)}) @ {time}");
        return 0;
    }
}
