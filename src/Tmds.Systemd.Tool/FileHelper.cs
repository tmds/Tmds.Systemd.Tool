using System.IO;

namespace Tmds.Systemd.Tool
{
    static class FileHelper
    {
        public static void CreateFileForRoot(string filename)
        {
            // Create a new empty file.
            using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                { }
            }
            bool deleteFile = true;
            try
            {
                if (!ProcessHelper.ExecuteSuccess("chmod", $"644 {filename}") ||
                    !ProcessHelper.ExecuteSuccess("chown", $"root:root {filename}"))
                {
                    throw new IOException($"Failed to set permissions and ownership of {filename}");
                }
                deleteFile = false;
            }
            finally
            {
                try
                {
                    if (deleteFile)
                    {
                        File.Delete(filename);
                    }
                }
                catch
                {}
            }
        }
    }
}