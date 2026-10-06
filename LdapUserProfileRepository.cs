using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core.Dto.LdapUsers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models.LdapUsers;

namespace Nupco.EF.Repositories
{
    internal class LdapUserProfileRepository : BaseRepository<LdapUserProfile>, ILdapUserProfileRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        public LdapUserProfileRepository(ApplicationDbContext context, ILogger logger)
            : base(context, logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LdapUserProfile?> GetByUserNameAsync(string userName)
        {
            return await _context.LdapUserProfiles
                .FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<List<LdapUserProfile>> GetDirectReportsAsync(string managerUserName)
        {
            return await _context.LdapUserProfiles
                .Where(u => u.ManagerUserName == managerUserName && !u.IsDeleted)
                .ToListAsync();
        }

        public async Task<OrgChartNodeDto?> GetOrgChartAsync(string rootUserName)
        {
            // Load all active profiles into memory once — avoids N+1 recursive queries.
            var allProfiles = await _context.LdapUserProfiles
                .Where(u => !u.IsDeleted)
                .ToListAsync();

            var lookup = allProfiles
                .GroupBy(u => u.ManagerUserName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key ?? "", g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var root = allProfiles.FirstOrDefault(u =>
                string.Equals(u.UserName, rootUserName, StringComparison.OrdinalIgnoreCase));

            if (root == null) return null;

            return BuildNode(root, lookup);
        }

        private static OrgChartNodeDto BuildNode(
            LdapUserProfile profile,
            Dictionary<string, List<LdapUserProfile>> lookup)
        {
            var node = new OrgChartNodeDto
            {
                UserName       = profile.UserName,
                FullName       = $"{profile.FirstName} {profile.LastName}".Trim(),
                Title          = profile.Title,
                Department     = profile.Department,
                Division       = profile.Division,
                Email          = profile.Email,
                Phone          = profile.Phone,
                Mobile         = profile.Mobile,
                OfficeLocation = profile.OfficeLocation,
                PhotoBase64    = profile.PhotoBase64,
                ManagerUserName = profile.ManagerUserName,
            };

            if (lookup.TryGetValue(profile.UserName ?? "", out var reports))
            {
                foreach (var report in reports)
                    node.DirectReports.Add(BuildNode(report, lookup));
            }

            return node;
        }

        public async Task UpsertRangeAsync(IEnumerable<LdapUserProfile> profiles, CancellationToken token)
        {
            var list = profiles.ToList();
            if (!list.Any()) return;

            var userNames = list.Select(p => p.UserName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var existing = await _context.LdapUserProfiles
                .Where(p => userNames.Contains(p.UserName))
                .ToListAsync(token);

            var existingMap = existing.ToDictionary(p => p.UserName!, StringComparer.OrdinalIgnoreCase);

            var toInsert = new List<LdapUserProfile>();
            var toUpdate = new List<LdapUserProfile>();

            foreach (var incoming in list)
            {
                if (existingMap.TryGetValue(incoming.UserName!, out var existing_))
                {
                    existing_.FirstName      = incoming.FirstName;
                    existing_.LastName       = incoming.LastName;
                    existing_.Email          = incoming.Email;
                    existing_.EmployeeId     = incoming.EmployeeId;
                    existing_.Title          = incoming.Title;
                    existing_.Department     = incoming.Department;
                    existing_.Division       = incoming.Division;
                    existing_.OfficeLocation = incoming.OfficeLocation;
                    existing_.Phone          = incoming.Phone;
                    existing_.Mobile         = incoming.Mobile;
                    existing_.Address        = incoming.Address;
                    existing_.ManagerUserName = incoming.ManagerUserName;
                    existing_.IsActive       = incoming.IsActive;
                    existing_.IsDeleted      = incoming.IsDeleted;
                    existing_.LastSyncedAt   = incoming.LastSyncedAt;

                    if (!string.IsNullOrWhiteSpace(incoming.PhotoBase64))
                        existing_.PhotoBase64 = incoming.PhotoBase64;

                    toUpdate.Add(existing_);
                }
                else
                {
                    toInsert.Add(incoming);
                }
            }

            if (toInsert.Any())
            {
                await _context.LdapUserProfiles.AddRangeAsync(toInsert, token);
                await _context.SaveChangesAsync(token);
            }

            if (toUpdate.Any())
            {
                _context.LdapUserProfiles.UpdateRange(toUpdate);
                await _context.SaveChangesAsync(token);
            }
        }
    }
}
