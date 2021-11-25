# Astro.Query
Qkmaxware.Astro.Query contains methods and classes to interact with astronomical APIs.

## Example Usage(s)
1. Downloading an image of M31
```cs
public void downloadFromNasa() {
    var result = NasaImageLibraryAPI.QueryImages("M31").FirstOrDefault();
    if (result != null) {
        result.DownloadThumbnailImageToFile("M31"); 
    }
}
```
2. Querying SIMBAD for alternative names for M31
```cs
public string[] getAliasesFromSimbad() {
    var result = SimbadAPI.WithIdentifier("M31").FirstOrDefault();
    if (result != null) {
        return result.IdentifierList;
    } else {
        return null;
    }
}
```