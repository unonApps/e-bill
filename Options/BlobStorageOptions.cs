namespace TAB.Web.Options;

public class BlobStorageOptions
{
    public string? StorageConnection { get; set; }
    public string? ContainerName { get; set; }
    public string? StorageAccountName { get; set; }
    public string? StorageAccountKey { get; set; }
}
