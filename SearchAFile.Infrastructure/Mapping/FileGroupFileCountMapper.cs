using SearchAFile.Core.Domain.Entities;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Infrastructure.Mapping;

public static class FileGroupFileCountMapper
{
    public static void MapFilesCountToFileGroups(List<FileGroup> fileGroups, List<File> files)
    {
        try
        {
            var fileCountLookup = files
                .GroupBy(f => f.FileGroupId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var fg in fileGroups)
            {
                if (fileCountLookup.TryGetValue(fg.FileGroupId, out var count))
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

    public static void MapFilesCountToFileGroup(FileGroup fileGroup, List<File> files)
    {
        try
        {
            fileGroup.FilesCount = files.Count(f => f.FileGroupId == fileGroup.FileGroupId);
        }
        catch
        {
            throw;
        }
    }

    public static void MapFilesToFileGroups(List<FileGroup> fileGroups, List<File> files)
    {
        try
        {
            var fileLookup = files
                .GroupBy(f => f.FileGroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var fg in fileGroups)
            {
                if (fileLookup.TryGetValue(fg.FileGroupId, out var matchingFiles))
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

    public static void MapFilesToFileGroup(FileGroup fileGroup, List<File> files)
    {
        try
        {
            var matchingFiles = files.Where(f => f.FileGroupId == fileGroup.FileGroupId).ToList();
            fileGroup.Files = matchingFiles;
            fileGroup.FilesCount = matchingFiles.Count;
        }
        catch
        {
            throw;
        }
    }
}