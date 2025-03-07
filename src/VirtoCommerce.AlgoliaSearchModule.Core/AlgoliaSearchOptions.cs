namespace VirtoCommerce.AlgoliaSearchModule.Core;

public class AlgoliaSearchOptions
{
    /// <summary>
    ///  Application ID for algolia server
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// Write API Key for either algolia server.
    /// </summary>
    public string ApiKey { get; set; }
}

public class AlgoliaIndexSortReplica
{
    /// <summary>
    /// Only supported on premium version
    /// </summary>
    public bool IsVirtual { get; set; } = false;

    public string FieldName { get; set; }

    public bool IsDescending { get; set; } = true;
}
