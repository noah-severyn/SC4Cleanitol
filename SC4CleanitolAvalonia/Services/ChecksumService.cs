using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace SC4CleanitolAvalonia.Services;

/// <summary>
/// Provides functionality for generating a SHA-256 checksum from folder contents based on file path, size, and last write time in UTC.
/// </summary>
/// <remarks>Produces a deterministic hash by ordering files before hashing and combining each file's metadata into the hash input.</remarks>
internal class ChecksumService: IChecksumService {
    /// <summary>
    /// Computes a SHA-256 hash representing the contents and metadata of all files and directories within the specified folder(s) and their subdirectories.
    /// </summary>
    /// <param name="folders">List of folders to parse</param>
    /// <returns>A hexadecimal string representing the SHA-256 hash of the folder's contents and metadata. The hash is based on the full path, size, and last modified time of each file and directory.</returns>
    public string? Compute(List<string> folders) {
        List<string> allItems = [];
        foreach (var folder in folders) {
            allItems.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories));
        }
        var items = allItems
            .OrderBy(e => e)
            .Select(e => {
                    var info = new FileInfo(e);
                    return $"{e}|{info.Length}|{info.LastWriteTimeUtc}";
                });

        var bytes = Encoding.UTF8.GetBytes(string.Join("\n", items));
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

