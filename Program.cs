# if DEBUG
using System;
using System.Linq;

namespace Qkmaxware.Astro.Query {

class TestProgram {

    public static void Main(string[] args) {
        TestSkyView();
    }

    public static void TestNasa() {
        var result = NasaImageLibraryAPI.QueryImages("M31").FirstOrDefault();
        if (result != null) {
            result.DownloadThumbnailImageToFile("M31"); 
        }
    }

    public static void TestSimbad() {
        var result = SimbadAPI.WithIdentifier("M31").FirstOrDefault();
        if (result != null) {
            Console.WriteLine(result.Name);
            if (result.IdentifierList != null) {
                foreach (var name in result.IdentifierList) {
                    Console.WriteLine("    " + name);
                }
            }
        }
    }

    public static void TestSkyView() {
        var image = NasaSkyViewAPI.Query(
            Measurement.Angle.HoursMinutesSeconds(00,42,44.330),
            Measurement.Angle.DegreesMinutesSeconds(41,16,07.50),
            new NasaSkyViewAPI.QueryParametres {
                FileFormat = NasaSkyViewAPI.ReturnFormat.PNG,
                Fov = Measurement.Angle.Degrees(3.31)
            }
        );
        image.DownloadImageToFile("m31.png");
    }

}

}

# endif