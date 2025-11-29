using System.Security.Cryptography;
using ImageProcessor.Service.Services.SaveAndRestores.Contracts;

namespace ImageProcessor.Service.Services.SaveAndRestores.Services;

public class SaveAndRestoreService : ISaveAndRestoreService
{
    public void Deduplicate(string inputFile, string dedupFile, string indexFile)
    {
        const int blockSize = 4096; // 4KB
        var dedupMap = new Dictionary<string, long>(); // هش → موقعیت در 
        var indexList = new List<(long pos, int len)>(); // موقعیت و طول بلوک 
        using var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
        using var dedup = new FileStream(dedupFile, FileMode.Create, FileAccess.Write);
        byte[] buffer = new byte[blockSize];
        int bytesRead;
        while ((bytesRead = input.Read(buffer, 0, blockSize)) > 0)
        {
            string hash = GetHash(buffer, bytesRead);
            if (!dedupMap.ContainsKey(hash))
            {
                long position = dedup.Position;
                dedup.Write(buffer, 0, bytesRead);
                dedupMap[hash] = position;
            }

            indexList.Add((dedupMap[hash], bytesRead));
        }

        using var writer = new StreamWriter(indexFile);
        foreach (var entry in indexList)
            writer.WriteLine($"{entry.pos},{entry.len}");
    }

    public void Rebuild(string dedupFile, string indexFile, string outputFile)
    {
        using var dedup = new FileStream(dedupFile, FileMode.Open, FileAccess.Read);
        using var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
        foreach (var line in File.ReadAllLines(indexFile))
        {
            var parts = line.Split(',');
            long position = long.Parse(parts[0]);
            int length = int.Parse(parts[1]);
            byte[] buffer = new byte[length];
            dedup.Seek(position, SeekOrigin.Begin);
            dedup.Read(buffer, 0, length);
            output.Write(buffer, 0, length);
        }
    }

    public string GetHash(byte[] data, int length)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data, 0, length);
        return Convert.ToBase64String(hash);
    }
}