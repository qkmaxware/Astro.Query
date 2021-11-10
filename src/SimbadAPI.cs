using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Qkmaxware.Astro.IO.Votable;
using Qkmaxware.Measurement;
using Qkmaxware.Numbers;

namespace Qkmaxware.Astro.Query {

/// <summary>
/// Interface to the Simbad Astronomical API
/// </summary>
public static class SimbadAPI {

    private static readonly string BaseUrl = "http://simbad.u-strasbg.fr/simbad";
    private static string IdentifierQuery => BaseUrl + "/sim-id";
    private static string CoordinateQuery => BaseUrl + "/sim-coo";
    private static string ReferenceQuery => BaseUrl + "/sim-ref";
    private static string CriteraQuery => BaseUrl + "/sim-sam";
    private static string ScriptQuery => BaseUrl + "/sim-script";
    private static RateLimiter blocker = new RateLimiter(5, TimeSpan.FromSeconds(1));

    private static string sendGet(HttpClient web, UriBuilder builder) {
        var path = builder.Uri.ToString();
        var task = web.GetStringAsync(path);
        task.Wait();
        var response = task.Result;
        return response;
    }

    private static IEnumerable<SimbadResult> sendQuery(string url, Dictionary<string, string> uri_parametres = null) {
        // Configure URL parametres
        var builder = new UriBuilder(url);
        if (uri_parametres != null) {
            foreach (var kv in uri_parametres) {
                builder.AddParametre(kv.Key, kv.Value);
            }
        }
        builder.AddParametre("OutputMode", "LIST");
        builder.AddParametre("frameN", "ICRS");
        builder.AddParametre("output.max", "50000");
        builder.AddParametre("output.format", "votable");

        // Send web request
        using (var web = new HttpClient()) {
            // Query and clean up response to get just the XML
            var response = blocker.Invoke(() => sendGet(web, builder));
            return ParseSimbadResponse(response);
        }
    }

    private static string selectFirstNonEmpty(List<KeyValuePair<string, string>> row, params string[] columns) {
        for (var i = 0; i < columns.Length; i++) {
            var columnName = columns[i];
            var value = row.Where(kv => kv.Key == columnName).FirstOrDefault().Value;
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        return string.Empty;
    }

    private static Length Kiloparsecs(Scientific value) {
        return Parsecs(value * 1000000d);
    }

    private static Length Megaparsecs(Scientific value) {
        return Parsecs(value * 1000d);
    }

    private static Scientific Parsecs2Km = 30856775812800;
    private static Length Parsecs(Scientific value) {
        return Length.Kilometres(value * Parsecs2Km);
    }

    public static IEnumerable<SimbadResult> ParseSimbadResponse(string response) {
        Regex rgx = new Regex(":+data:+");
        var parts = rgx.Split(response ?? string.Empty); // Jump to the data section
        var data = parts[parts.Length - 1].TrimStart();

        // Parse XML votable output to data structure
        var deserializer = new VotableDeserializer(); 
        var votable = deserializer.Deserialize(new StringReader(data));

        // Convert VOTable to Simbad.Entry
        foreach (var table in votable) {
            var epoch = table.Epoch;
            var equinox = table.Equinox;
            foreach (var row in table) {
                // Compute distance
                var distanceUnit = selectFirstNonEmpty(row, "Distance:unit", "Distance_unit");
                string distanceString = selectFirstNonEmpty(row, "Distance:distance",  "Distance_distance");
                Length dist = null;
                if (!string.IsNullOrEmpty(distanceString)) {
                    var distance = double.Parse(distanceString);
                    dist = distanceUnit switch {
                        "kpc"   => Kiloparsecs(distance),
                        "Mpc"   => Megaparsecs(distance),
                        _       => Parsecs(distance)
                    };
                }

                // Compute celestial coordinates
                var raString = selectFirstNonEmpty(row, "RA_d", "RA(d)");
                Angle ra = null;
                if (!string.IsNullOrEmpty(raString)) {
                    var ra_deg = double.Parse(raString);
                    var ra_hr = ra_deg * (24.0d / 360.0d);
                    ra = Angle.Hours(ra_hr);
                }  
                var lastString = selectFirstNonEmpty(row, "DEC_d", "DEC(d)");
                Angle decl = null;
                if (!string.IsNullOrEmpty(lastString)) {   
                    var lat_deg = double.Parse(lastString);
                    decl = Angle.Degrees(lat_deg);
                }

                // Compute motion parametres
                /*var raPropString = table[row, "PMRA"];
                var decPropString = table[row, "PMDEC"];
                ProperMotion? motion = null;
                if (!string.IsNullOrEmpty(raPropString) && !string.IsNullOrEmpty(decPropString)) {
                    var ra_prop_motion = double.Parse(raPropString);                      // unit mas.yr-1
                    var dec_prop_motion = double.Parse(decPropString);                    // unit mas.yr-1
                    var year = TimeSpan.FromDays(365.25);
                    var ra_hour_angles_per_year = (ra_prop_motion / (3600 * 1000)) / 15;  // Convert mas to hrangle
                    var dec_degrees_per_year = dec_prop_motion / (3600 * 1000);           // Convert mas to degrees
                    motion = new ProperMotion(
                        raRate: new RateOfChange<Angle>(Angle.Hours(ra_hour_angles_per_year), year),
                        decRate: new RateOfChange<Angle>(Angle.Degrees(dec_degrees_per_year), year)
                    );
                }*/
                
                // Compute object type
                string type = string.Empty;
                var typeList = selectFirstNonEmpty(row, "maintypes", "MAINTYPES", "otype_s", "OTYPE_S");
                if (!string.IsNullOrEmpty(typeList)) {
                    type = typeList.Split(',')[0]; // Select first from type list
                }
                var maintype = selectFirstNonEmpty(row, "maintype", "MAINTYPE", "otype", "OTYPE");
                if (!string.IsNullOrEmpty(maintype)) {
                    type = maintype;                // Select the main types
                }

                // Return
                yield return new SimbadResult(
                    name:       selectFirstNonEmpty(row, "MAIN_ID"),
                    type:       type,
                    epoch:      epoch,
                    equinox:    equinox,
                    ra:         ra,
                    dec:        decl,
                    identifierList: selectFirstNonEmpty(row, "ids", "IDS")?.Split('|')
                ); 
            }
        }
    }

    // Script reference
    // http://simbad.u-strasbg.fr/simbad/sim-fscript
    // http://simbad.u-strasbg.fr/guide/sim-fscript.htx
    // http://simbad.u-strasbg.fr/simbad/sim-display?data=otypes
    private static string makeQueryString (string query) {
        var script = new ScriptBuilder();
        script.Query(query);
        return script.ToString();
    }

    /// <summary>
    /// Class for helping create script strings for SIMBAD queries
    /// </summary>
    public class ScriptBuilder {

        private int limit = 0;
        public void SetLimit(int limit) {
            this.limit = Math.Max(limit, 0);
        }
        public void ClearLimit() { SetLimit(0); }

        private string query;
        public void Query(string query) {
            this.query = query;
        }

        public void QueryId(string id, bool allowWildcards = false) {
            if (allowWildcards) {
                Query($"id wildcard {id}");
            } else {
                Query($"id {id}");
            }
        }
        public void QueryIds(IEnumerable<string> ids, bool allowWildcards = false) {
            if (allowWildcards) {
                StringBuilder sb = new StringBuilder();
                foreach (var id in ids) {
                    sb.AppendLine($"id wildcard {id}");
                }
                Query(sb.ToString());
            } else {
                StringBuilder sb = new StringBuilder();
                foreach (var id in ids) {
                    sb.AppendLine($"id {id}");
                }
                Query(sb.ToString());
            }
        }

        public void QueryAroundId(string id, Angle radius) {
            Query($"query around {id} radius={radius.TotalDegrees()}d");
        }

        public void QueryCatalogue(string catalogue) {
            Query($"cat {catalogue}");
        }

        public override string ToString() {
            var builder = new StringBuilder();
            builder.AppendLine(
@"votable vot1 {
    MAIN_ID
    IDS
    OTYPE
    RA(d)
    DEC(d)
    PMRA
    PMDEC
    Distance
}
votable open vot1
result full
set frame ICRS
set limit " + this.limit);
            
            if (!string.IsNullOrEmpty(this.query)) {
                builder.AppendLine("query " + this.query);
            }

            builder.Append("votable close");

            return builder.ToString();
        }
    }

    /// <summary>
    /// Query SIMBAD using a script
    /// </summary>
    /// <param name="script">script string</param>
    /// <returns>list of SIMBAD results</returns>
    public static IEnumerable<SimbadResult> FromScript(string script) {
        return sendQuery(ScriptQuery, new Dictionary<string, string>{
            {"script", script},
        });
    }

    /// <summary>
    /// Query SIMBAD using a script
    /// </summary>
    /// <param name="script">script builder</param>
    /// <returns>list of SIMBAD results</returns>
    public static IEnumerable<SimbadResult> FromScript(ScriptBuilder script) {
        return FromScript(script.ToString());
    }

    /// <summary>
    /// Query SIMBAD using an ID
    /// </summary>
    /// <param name="id">object id</param>
    /// <returns>list of SIMBAD results</returns>
    public static IEnumerable<SimbadResult> WithIdentifier(string id) {
        var query = makeQueryString($"id {id}");
        return FromScript(query);
    }

    /// <summary>
    /// Query SIMBAD for objects within the given distance
    /// </summary>
    /// <param name="distance">distance</param>
    /// <returns>list of SIMBAD results</returns>
    public static IEnumerable<SimbadResult> WithinDistance(Length distance) {
        var parsecs = distance.TotalKilometres() / Parsecs2Km;
        return WithCriteria ($"Distance.unit='pc' & Distance.distance={parsecs}");
    }

    /// <summary>
    /// Query SIMBAD for objects matching the given criteria
    /// </summary>
    /// <param name="query">criteria query</param>
    /// <returns>list of SIMBAD results</returns>
    public static IEnumerable<SimbadResult> WithCriteria(string query) {
        /*return sendQuery(CriteraQuery, new Dictionary<string, string>{
            {"Criteria", query},
        });*/
        return FromScript(makeQueryString($"sample {query}"));
    }

    /// <summary>
    /// Query SIMBAD for objects in the given catalogue
    /// </summary>
    /// <param name="catalogue">catalogue identifier</param>
    /// <returns>list of SIMBAD results</returns>
    public static IEnumerable<SimbadResult> FromCatalogue(string catalogue) {
        return FromScript(makeQueryString($"cat {catalogue}"));
    }

}

}