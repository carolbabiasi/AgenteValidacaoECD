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

    public const int MaxChunkKeyLength = 128;

    public static string BuildChunkKey(string hierarchyPath, string contentHash)
    {
        var normalizedPath = hierarchyPath.Trim();
        var suffix = $"|{contentHash[..16]}";
        if (normalizedPath.Length + suffix.Length <= MaxChunkKeyLength)
        {
            return normalizedPath + suffix;
        }

        var pathHash = ComputeContentHash(normalizedPath)[..8];
        var prefixLength = MaxChunkKeyLength - suffix.Length - pathHash.Length - 1;
        return $"{normalizedPath[..prefixLength]}~{pathHash}{suffix}";
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
