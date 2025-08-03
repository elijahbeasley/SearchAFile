using SearchAFile.Core.Domain.Entities;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Infrastructure.Mapping;

public static class CollectionFileCountMapper
{
    public static void MapFilesCountToCollections(List<Collection> collection, List<File> files)
    {
        try
        {
            var fileCountLookup = files
                .GroupBy(f => f.CollectionId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var fg in collection)
            {
                if (fileCountLookup.TryGetValue(fg.CollectionId, out var count))
                {
                    fg.FilesCount = count;
                }
                else
                {
                    fg.FilesCount = 0;
                }
            }
        }
        catch
        {
            throw;
        }
    }

    public static void MapFilesCountToCollection(Collection collection, List<File> files)
    {
        try
        {
            collection.FilesCount = files.Count(f => f.CollectionId == collection.CollectionId);
        }
        catch
        {
            throw;
        }
    }

    public static void MapFilesToCollections(List<Collection> collection, List<File> files)
    {
        try
        {
            var fileLookup = files
                .GroupBy(f => f.CollectionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var fg in collection)
            {
                if (fileLookup.TryGetValue(fg.CollectionId, out var matchingFiles))
                {
                    fg.Files = matchingFiles;
                    fg.FilesCount = matchingFiles.Count;
                }
                else
                {
                    fg.Files = new List<File>();
                    fg.FilesCount = 0;
                }
            }
        }
        catch
        {
            throw;
        }
    }

    public static void MapFilesToCollection(Collection collection, List<File> files)
    {
        try
        {
            var matchingFiles = files.Where(f => f.CollectionId == collection.CollectionId).ToList();
            collection.Files = matchingFiles;
            collection.FilesCount = matchingFiles.Count;
        }
        catch
        {
            throw;
        }
    }
}