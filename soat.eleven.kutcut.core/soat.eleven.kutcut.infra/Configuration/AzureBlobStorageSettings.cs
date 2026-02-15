namespace soat.eleven.kutcut.infra.Configuration
{
    public class AzureBlobStorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "kutcut";
    }
}
