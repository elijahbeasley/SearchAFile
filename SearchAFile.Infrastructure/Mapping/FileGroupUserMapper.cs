using SearchAFile.Core.Domain.Entities;

namespace SearchAFile.Infrastructure.Mappers;
public static class FileGroupUserMapper
{
    public static void MapUserNamesToFileGroups(List<FileGroup> fileGroups, List<UserDto> users)
    {
        try
        {
            var userLookup = users.ToDictionary(u => u.UserId, u => u.FullName);

            foreach (var fg in fileGroups)
            {
                if (userLookup.TryGetValue(fg.CreatedByUserId, out var fullName))
                {
                    fg.CreatedByUserFullName = fullName;
                }
            }
        }
        catch
        {
            throw;
        }
    }
}