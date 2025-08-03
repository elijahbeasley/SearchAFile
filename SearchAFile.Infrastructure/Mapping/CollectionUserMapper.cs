using SearchAFile.Core.Domain.Entities;

namespace SearchAFile.Infrastructure.Mappers;
public static class CollectionUserMapper
{
    public static void MapUserNamesToCollections(List<Collection> collection, List<UserDto> users)
    {
        try
        {
            var userLookup = users.ToDictionary(u => u.UserId, u => u.FullName);

            foreach (var fg in collection)
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