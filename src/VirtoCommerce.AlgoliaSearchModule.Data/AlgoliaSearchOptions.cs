namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public class AlgoliaSearchOptions
    {
        public string AppId { get; set; }

        public string ApiKey { get; set; }

        public AlgoliaIndexSortReplica[] Replicas { get; set; }
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
}
