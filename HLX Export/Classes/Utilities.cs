using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace HLXExport
{
    public static class Utilities
    {
        public class ZippedFileCollection
        {
            private readonly string uuid;
            private readonly string referenceFolderPath;

            public readonly string CollectionLocation;

            private List<string> files = new List<string>();

            public ZippedFileCollection(string referenceFolderPath)
            {
                this.uuid = System.Guid.NewGuid().ToString();
                this.referenceFolderPath = referenceFolderPath;

                CollectionLocation = Path.Combine(referenceFolderPath, uuid);

                Directory.CreateDirectory(CollectionLocation);
            }

            public void CopyFileToCollection(string sourcePath, string filename)
            {

                string destinationPath = Path.Combine(CollectionLocation, filename);
                // This will error if a duplicate filename is present
                File.Copy(sourcePath, destinationPath, false);

                files.Add(destinationPath);
            }

            public List<string> GetFiles()
            {
                return files;
            }

            public List<string> GetFiles(string extension)
            {
                return files.Where(f => f.EndsWith(extension)).ToList();
            }

            public void DestroyCollection() {
                Directory.Delete(CollectionLocation, true);
            }

            public void RegisterFile(string filePath)
            {
                files.Add(Path.Combine(CollectionLocation,filePath));
            }

            public IEnumerable<FileStream> Filter(string fileExtension)
            {
                foreach (string file in files) {
                    if (file.EndsWith(fileExtension)) {
                        yield return new FileStream(file, FileMode.Open);
                    }
                }
            }

            public string GetFilePath(string filename) {
                return Path.Combine(CollectionLocation, filename);
            }
            public static ZippedFileCollection Open(string filename, string destinationPath) {
                ZippedFileCollection collection = new ZippedFileCollection(destinationPath);

                using StreamReader sr = new(filename);
                using (ZipArchive zip = new(sr.BaseStream)) {

                    zip.ExtractToDirectory(collection.CollectionLocation);

                    foreach (ZipArchiveEntry entry in zip.Entries) {

                        if (entry.FullName.EndsWith('/'))
                            continue;

                        Debug.Log("OpenZipFile: found: " + entry.FullName);
                        collection.RegisterFile(entry.FullName);
                    }

                }

                return collection;
            }
        }
    }
}
