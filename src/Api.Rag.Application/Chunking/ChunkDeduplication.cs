using System.Security.Cryptography;
using System.Text;

namespace Api.Rag.Application.Chunking;

public static class ChunkDeduplication
{
    public static string ComputeContentHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    public static string BuildChunkKey(string hierarchyPath, string contentHash)
    {
        var normalizedPath = hierarchyPath.Trim();
        return $"{normalizedPath}|{contentHash[..16]}";
    }

    public static bool ShouldSkipChunk(
        string chunkKey,
        string contentHash,
        IReadOnlySet<string> existingChunkKeys,
        IReadOnlyDictionary<string, string> existingHashesByKey)
    {
        if (existingChunkKeys.Contains(chunkKey))
        {
            return true;
        }

        if (existingHashesByKey.TryGetValue(chunkKey, out var knownHash) &&
            string.Equals(knownHash, contentHash, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
