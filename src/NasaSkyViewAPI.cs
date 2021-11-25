using System;
using System.Collections.Generic;
using Qkmaxware.Astro.Equipment;
using Qkmaxware.Measurement;

namespace Qkmaxware.Astro.Query {


/// <summary>
/// Interface to handle requests to Nasa Sky View API (https://skyview.gsfc.nasa.gov/current/docs/batchpage.html)
/// </summary>
public class NasaSkyViewAPI {
    private static string baseUrl = "https://skyview.gsfc.nasa.gov/cgi-bin/images";

    /// <summary>
    /// Return formats for Sky View
    /// </summary>
    public enum ReturnFormat {
        PNG, GIF, FITS
    }
    
    /// <summary>
    /// Coordinate systems support by Sky View
    /// </summary>
    public enum CoordinateSystem {
        J2000, B1950, Galactic, ICRS
    }

    /// <summary>
    /// Base parameters for Sky View queries
    /// </summary>
    public class QueryParametres {
        public string[] Surveys = new string[] { "digitized sky survey" };
        public CoordinateSystem CoordinateSystem = CoordinateSystem.J2000;
        public ReturnFormat FileFormat = ReturnFormat.PNG;
        public Angle HorizontalFov;
        public Angle VerticalFov;
        public Angle Fov {
            set {
                this.HorizontalFov = value;
                this.VerticalFov = value;
            }
        }
        public bool AllowGreyscale = true;
        public CoordinateSystem? OverlayGrid = null;
        public int Width = 300;
        public int Height = 300;
    }

    private Uri params2Uri(Angle ra, Angle dec, QueryParametres p) {
        var builder = new UriBuilder(baseUrl);
        // Format can be in HH MM SS
        builder.AddParametre("Position", $"{(double)ra.TotalHours()},{(double)dec.TotalDegrees()}");
        builder.AddParametre("Survey", string.Join(",", p.Surveys));
        builder.AddParametre("Coordinates", p.CoordinateSystem);
        if (p.HorizontalFov != null && p.VerticalFov != null) {
            builder.AddParametre("Size", $"{(double)p.HorizontalFov.TotalDegrees()},{(double)p.VerticalFov.TotalDegrees()}");
        }
        builder.AddParametre("Pixels", $"{p.Width},{p.Height}");
        if (p.OverlayGrid.HasValue) {
            builder.AddParametre("Grid", p.OverlayGrid.Value);
        }
        if (!p.AllowGreyscale) {
            builder.AddParametre("RGB", string.Empty);
        }
        builder.AddParametre("Return", p.FileFormat);
        
        return builder.Uri;
    } 

    /// <summary>
    /// Generic image query of the given location in the sky
    /// </summary>
    /// <param name="ra">right ascension</param>
    /// <param name="dec">declination</param>
    /// <param name="params">query parameters</param>
    /// <returns>image</returns>
    public ImageUriReference Query(Angle ra, Angle dec, QueryParametres @params) {
        var uri = params2Uri(ra, dec, @params);
        return new ImageUriReference(uri);
    }

    /// <summary>
    /// Query an image based on parameters defined by the an imaging setup
    /// </summary>
    /// <param name="ra">right ascension</param>
    /// <param name="dec">declination</param>
    /// <param name="ccd">imaging camera specs</param>
    /// <param name="lens">telescope lens specs</param>
    /// <returns></returns>
    public ImageUriReference Scan(Angle ra, Angle dec, Camera ccd, Telescope lens) {
        // Auto compute fov and pixels based on imaging setup
        var p = new QueryParametres {
            HorizontalFov = lens.HorizontalFieldOfView(ccd),
            VerticalFov = lens.VerticalFieldOfView(ccd),
            Width = ccd.Resolution.Width,
            Height = ccd.Resolution.Height
        };
        // Query
        return Query(ra, dec, p);
    }
    
    private Angle lerpAngle(Angle a1, Angle a2, float t) {
        return (1 - t) * a1 + t * a2;
    }
    
    /// <summary>
    /// Scan over an area of sky taking multiple pictures as the camera is moved
    /// </summary>
    /// <param name="startRa">starting pointing right ascension</param>
    /// <param name="endRa">ending pointing right ascension</param>
    /// <param name="horizontalTiling">amount of images to take horizontally over right ascension range</param>
    /// <param name="startDec">starting pointing declination</param>
    /// <param name="endDec">ending pointing declination</param>
    /// <param name="verticalTitling">amount of images to take vertically over the decimlination range</param>
    /// <param name="ccd">imaging camera specs</param>
    /// <param name="lens">telescope lens specs</param>
    /// <returns>enumerable of images</returns>
    public IEnumerable<ImageUriReference> Scan(Angle startRa, Angle endRa, int horizontalTiling, Angle startDec, Angle endDec, int verticalTitling, Camera ccd, Telescope lens) {
        var vtile = Math.Max(1, verticalTitling);
        var htile = Math.Max(1, horizontalTiling);

        for (var v = 0; v < vtile; v++) {
            var decT = v / (vtile - 1);
            for (var h = 0; h < htile; h++) {
                var raT = h / (htile - 1);

                var positionRa = lerpAngle(startRa, endRa, raT);
                var positionDec = lerpAngle(startDec, endDec, decT);
                yield return Scan(positionRa, positionDec, ccd, lens);
            }
        }
    }
}

}