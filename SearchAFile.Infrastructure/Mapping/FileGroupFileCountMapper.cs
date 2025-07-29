using SearchAFile.Core.Domain.Entities;

namespace SearchAFile.Infrastructure.Mapping;

public static class FileGroupFileCountMapper
{
    public static void MapFilesCountToFileGroups(List<FileGroup> fileGroups, List<Core.Domain.Entities.File> files)
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
}