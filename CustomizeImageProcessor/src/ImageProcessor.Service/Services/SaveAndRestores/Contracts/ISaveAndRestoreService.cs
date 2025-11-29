namespace ImageProcessor.Service.Services.SaveAndRestores.Contracts;

public interface ISaveAndRestoreService
{
    public void Deduplicate(string inputFile, string dedupFile, string indexFile);
}