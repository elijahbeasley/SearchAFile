using SearchAFile.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchAFile.Infrastructure.Mapping;
public static class UserCompanyMapper
{
    public static void MapCompaniesToUsers(List<User> users, List<Company> companies)
    {
        try
        {
            var companyLookup = companies.ToDictionary(c => c.CompanyId, u => u);

            foreach (var user in users)
            {
                if (companyLookup.TryGetValue(user.CompanyId, out var company))
                {
                    user.Company = company;
                }
            }
        }
        catch
        {
            throw;
        }
    }
}
