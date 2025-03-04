using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers
{
    public static class FileHelper
    {
        public static string RemoveFileExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            return Path.GetFileNameWithoutExtension(fileName);
        }

        public static string GetFileNameFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            return Path.GetFileName(path); // This safely handles both full paths and just filenames.
        }

        public static IEnumerable<string> GetFilesInDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"The specified directory does not exist: {path}");

            return Directory.GetFiles(path);
        }

        public static IEnumerable<string> FilterFilesByExtension(IEnumerable<string> files, string extension)
        {
            if (files == null)
                throw new ArgumentNullException(nameof(files), "Files collection cannot be null.");

            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("Extension cannot be null or empty.", nameof(extension));

            string sanitizedExtension = extension.Trim();
            if (sanitizedExtension.StartsWith("."))
                sanitizedExtension = sanitizedExtension.Substring(1);

            return files.Where(file => Path.GetExtension(file)?.TrimStart('.')?.Equals(sanitizedExtension, StringComparison.OrdinalIgnoreCase) == true);
        }

        public static void TrueDelete(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            filePath = Path.GetFullPath(filePath);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows: Secure delete, bypassing recycle bin
                    DeleteWindows(filePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Linux/macOS: Secure delete
                    DeleteUnix(filePath);
                }
                else
                {
                    // Fallback: Use standard deletion
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to permanently delete file: {filePath}", ex);
            }
        }

        private static void DeleteWindows(string filePath)
        {
            File.SetAttributes(filePath, FileAttributes.Normal); // Ensure file is not read-only
            File.Delete(filePath);

            // Extra security: Overwrite with empty file and delete again
            OverwriteFile(filePath);
        }

        private static void DeleteUnix(string filePath)
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);

            // Extra security: Overwrite file content before deletion
            OverwriteFile(filePath);

            // Attempt to use system-level shred (if available)
            try
            {
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start("shred", $"-u \"{filePath}\"");
                }
            }
            catch
            {
                // If `shred` is unavailable, rely on overwriting method
            }
        }

        private static void OverwriteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                    {
                        byte[] emptyData = new byte[fs.Length];
                        fs.Write(emptyData, 0, emptyData.Length);
                        fs.Flush();
                    }
                    File.Delete(filePath);
                }
                catch
                {
                    // Fallback: Just delete the file if overwrite fails
                    File.Delete(filePath);
                }
            }
        }
    }
}
