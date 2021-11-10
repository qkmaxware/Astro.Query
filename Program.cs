/*using System;
using System.Linq;

namespace Qkmaxware.Astro.Query {

class TestProgram {

    public static void Main(string[] args) {
        
    }

    public static void TestNasa() {
        var result = NasaImageLibraryAPI.QueryImages("M31").FirstOrDefault();
        if (result != null) {
            result.DownloadThumbnail("M31"); 
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

}

}*/