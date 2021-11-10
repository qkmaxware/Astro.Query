using Qkmaxware.Measurement;

namespace Qkmaxware.Astro.Query {

/// <summary>
/// Entity queried from the Simbad Database
/// </summary>
public class SimbadResult {
    /// <summary>
    /// Epoch for coordinates
    /// </summary>
    public string Epoch {get; private set;}
    /// <summary>
    /// Equinox for coordinates
    /// </summary>
    public string Equinox {get; private set;}
    /// <summary>
    /// Classification of the given object type
    /// </summary>
    public string Class {get; private set;}
    /// <summary>
    /// Name of entity
    /// </summary>
    public string Name {get; private set;}
    /// <summary>
    /// List of alternative identifiers for this object
    /// </summary>
    public string[] IdentifierList {get; private set;}
    /// <summary>
    /// Right ascension coordinate
    /// </summary>
    /// <value></value>
    public Angle RightAscension {get; private set;}
    /// <summary>
    /// Declination coordinate
    /// </summary>
    /// <value></value>
    public Angle Declination {get; private set;}
    public SimbadResult(string name, string type, string epoch, string equinox, Angle ra, Angle dec, string[] identifierList = null) {
        this.Name = name;
        this.Epoch = epoch;
        this.Equinox = equinox;
        this.Class = type;
        this.IdentifierList = identifierList;
        this.RightAscension = ra;
        this.Declination = dec;
    }
}

}