using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nupco.DAL.Models;
using System.DirectoryServices;
using System.Reflection.PortableExecutable;
using DirectoryEntry = System.DirectoryServices.DirectoryEntry;
using Microsoft.Extensions.Options;
using Nupco.Core.Dto;
using Nupco.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Nupco.DAL;
using Nupco.Core.Assembler;
using Nupco.Core;
using Microsoft.EntityFrameworkCore;
using Nupco.Core.Dto.GroupLDAP;
using Nupco.DAL.Models.Users;

namespace Nupco.EF.Repositories
{
    public class LDAPRepository : ILDAPRepository
    {
        private readonly ApplicationDbContext _context;

        private readonly ILogger _logger;
        private readonly IUnitOfWork _unitOfWork;

        private readonly string _ldapServer;
        private readonly string _ldapAccountlogin;
        private readonly string _ldapPassword;
        private readonly string _ldapDistinguishedName;

        public LDAPRepository(ApplicationDbContext context, ILogger logger, IOptions<LdapDto> appSettings)
        {
            _logger = logger;
            _context = context;
            _ldapServer = appSettings.Value.LdapServer;
            _ldapAccountlogin = appSettings.Value.LdapAccountLogin;
            _ldapPassword = appSettings.Value.LdapPassword;
            _ldapDistinguishedName = appSettings.Value.LdapDistinguishedName;
        }
        public User GetUserDetails(string username, string _ldapServer, string _ldapAccountlogin, string _ldapPassword, string _ldapDistinguishedName)
        {
            // -- Code to get current address of the LDAP----  
            DirectoryEntry rootDSE = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword);
            var defaultNamingContext = rootDSE.Properties["defaultNamingContext"].Value;

            //--- Code to use the current address for the LDAP and query it for the user---

            DirectorySearcher dssearch = new DirectorySearcher(_ldapDistinguishedName);
            dssearch.Filter = "(sAMAccountName=" + username + ")";
            SearchResult sresult = dssearch.FindOne();
            if (sresult == null)
            {
                return null;
            }
            DirectoryEntry dsresult = sresult.GetDirectoryEntry();

            //---  Code for getting the properties of the logged in user from AD  
            User UserInfo = new User();

            UserInfo.UserName = username;

            if (dsresult.Properties["employeeID"] != null && dsresult.Properties["employeeID"].Count > 0)
            {
                UserInfo.Code = dsresult.Properties["employeeID"][0].ToString();
            }

            if (dsresult.Properties["givenName"] != null && dsresult.Properties["givenName"].Count > 0)
            {
                UserInfo.FirstName = dsresult.Properties["givenName"][0].ToString();
            }

            if (dsresult.Properties["sn"] != null && dsresult.Properties["sn"].Count > 0)
            {
                UserInfo.LastName = dsresult.Properties["sn"][0].ToString();
            }

            if (dsresult.Properties["mail"] != null && dsresult.Properties["mail"].Count > 0)
            {
                UserInfo.Email = dsresult.Properties["mail"][0].ToString();
            }

            if (dsresult.Properties["Mobile"] != null && dsresult.Properties["Mobile"].Count > 0)
            {
                UserInfo.PhoneNumber = dsresult.Properties["Mobile"][0].ToString();
            }

            if (dsresult.Properties["Department"] != null && dsresult.Properties["Department"].Count > 0)
            {
                UserInfo.DepartmentName = dsresult.Properties["Department"][0].ToString();
            }

            // Similarly, perform null checks for other properties...

            if (dsresult.Properties["title"] != null && dsresult.Properties["title"].Count > 0)
            {
                UserInfo.PositionName = dsresult.Properties["title"][0].ToString();
            }

            if (dsresult.Properties["whenCreated"] != null && dsresult.Properties["whenCreated"].Count > 0)
            {
                UserInfo.HireDate = Convert.ToDateTime(dsresult.Properties["whenCreated"][0].ToString());
            }

            // Disabled?
            if (dsresult.Properties["userAccountControl"].Count > 0)
            {
                int uac = (int)dsresult.Properties["userAccountControl"][0];
                UserInfo.IsDeleted = (uac & 0x0002) != 0; // ACCOUNTDISABLE
            }


            return UserInfo;
        }

        public List<User> GetSuggestedUsers(string username, string _ldapServer, string _ldapAccountlogin, string _ldapPassword, string _ldapDistinguishedName)
        {
            // -- Code to get current address of the LDAP----  
            DirectoryEntry rootDSE = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword);
            var defaultNamingContext = rootDSE.Properties["defaultNamingContext"].Value;

            //--- Code to use the current address for the LDAP and query it for the user---

            DirectorySearcher dssearch = new DirectorySearcher(_ldapDistinguishedName);
            dssearch.Filter = "(|(givenName=*" + username + "*)(sn=*" + username + "*)(cn=*" + username + "*)(sAMAccountName=*" + username + "*))";
            var sresults = dssearch.FindAll();

            if (sresults==null || sresults.Count ==0 )
            {
                return null;
            }

            List<User> Users = new List<User>();

            foreach (SearchResult item in sresults)
            {
                DirectoryEntry dsresult = item.GetDirectoryEntry();
                
                //---  Code for getting the properties of the logged in user from AD  
                User UserInfo = new User();


                if (dsresult.Properties["sAMAccountName"] != null && dsresult.Properties["sAMAccountName"].Count > 0)
                {
                    UserInfo.UserName = dsresult.Properties["sAMAccountName"][0].ToString();
                }

                if (dsresult.Properties["employeeID"] != null && dsresult.Properties["employeeID"].Count > 0)
                {
                    UserInfo.Code = dsresult.Properties["employeeID"][0].ToString();
                }

                if (dsresult.Properties["givenName"] != null && dsresult.Properties["givenName"].Count > 0)
                {
                    UserInfo.FirstName = dsresult.Properties["givenName"][0].ToString();
                }

                if (dsresult.Properties["sn"] != null && dsresult.Properties["sn"].Count > 0)
                {
                    UserInfo.LastName = dsresult.Properties["sn"][0].ToString();
                }

                if (dsresult.Properties["mail"] != null && dsresult.Properties["mail"].Count > 0)
                {
                    UserInfo.Email = dsresult.Properties["mail"][0].ToString();
                }

                if (dsresult.Properties["Mobile"] != null && dsresult.Properties["Mobile"].Count > 0)
                {
                    UserInfo.PhoneNumber = dsresult.Properties["Mobile"][0].ToString();
                }

                if (dsresult.Properties["Department"] != null && dsresult.Properties["Department"].Count > 0)
                {
                    UserInfo.DepartmentName = dsresult.Properties["Department"][0].ToString();
                }

                // Similarly, perform null checks for other properties...

                if (dsresult.Properties["title"] != null && dsresult.Properties["title"].Count > 0)
                {
                    UserInfo.PositionName = dsresult.Properties["title"][0].ToString();
                }

                if (dsresult.Properties["whenCreated"] != null && dsresult.Properties["whenCreated"].Count > 0)
                {
                    UserInfo.HireDate = Convert.ToDateTime(dsresult.Properties["whenCreated"][0].ToString());
                }

                Users.Add(UserInfo);
            }





            return Users;
        }

        public User GetHQUserDetails(string username, string _ldapServer, string _ldapAccountlogin, string _ldapPassword, string _ldapDistinguishedName)
        {
            // -- Code to get current address of the LDAP----  
            DirectoryEntry rootDSE = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword);
            var defaultNamingContext = rootDSE.Properties["defaultNamingContext"].Value;

            //--- Code to use the current address for the LDAP and query it for the user---

            DirectorySearcher dssearch = new DirectorySearcher(_ldapDistinguishedName);
            dssearch.Filter = "(&(objectClass=user)(sAMAccountName=" + username + ")" +"(memberOf=CN=All-HQ,OU=Others,OU=Service Accounts,DC=nupco,DC=com))";
            SearchResult sresult = dssearch.FindOne();
           
            if (sresult == null)
            {
                return null;
            }
            DirectoryEntry dsresult = sresult.GetDirectoryEntry();

            //---  Code for getting the properties of the logged in user from AD  
            User UserInfo = new User();

            UserInfo.UserName = username;

            if (dsresult.Properties["employeeID"] != null && dsresult.Properties["employeeID"].Count > 0)
            {
                UserInfo.Code = dsresult.Properties["employeeID"][0].ToString();
            }

            if (dsresult.Properties["givenName"] != null && dsresult.Properties["givenName"].Count > 0)
            {
                UserInfo.FirstName = dsresult.Properties["givenName"][0].ToString();
            }

            if (dsresult.Properties["sn"] != null && dsresult.Properties["sn"].Count > 0)
            {
                UserInfo.LastName = dsresult.Properties["sn"][0].ToString();
            }

            if (dsresult.Properties["mail"] != null && dsresult.Properties["mail"].Count > 0)
            {
                UserInfo.Email = dsresult.Properties["mail"][0].ToString();
            }

            if (dsresult.Properties["Mobile"] != null && dsresult.Properties["Mobile"].Count > 0)
            {
                UserInfo.PhoneNumber = dsresult.Properties["Mobile"][0].ToString();
            }

            if (dsresult.Properties["Department"] != null && dsresult.Properties["Department"].Count > 0)
            {
                UserInfo.DepartmentName = dsresult.Properties["Department"][0].ToString();
            }

            // Similarly, perform null checks for other properties...

            if (dsresult.Properties["title"] != null && dsresult.Properties["title"].Count > 0)
            {
                UserInfo.PositionName = dsresult.Properties["title"][0].ToString();
            }

            if (dsresult.Properties["whenCreated"] != null && dsresult.Properties["whenCreated"].Count > 0)
            {
                UserInfo.HireDate = Convert.ToDateTime(dsresult.Properties["whenCreated"][0].ToString());
            }



            return UserInfo;
        }

        public async Task<bool> CheckHQ(string userName, string _ldapServer, string _ldapAccountlogin, string _ldapPassword, string _ldapDistinguishedName)
        {
            
            var _user = await _context.Users.FirstOrDefaultAsync(f => f.UserName.ToLower() == userName.ToLower());


            if (_user.IsCheckHQ == null)
            {
                User userInfo = new User();
                userInfo = GetHQUserDetails(userName, _ldapServer, _ldapAccountlogin, _ldapPassword, _ldapDistinguishedName);

                if (userInfo is not null)
                {
                    _user.IsCheckHQ = true;
                    _context.Users.Update(_user);
                    await _context.SaveChangesAsync();
                    return await Task.FromResult(true);

                }
                else
                {
                    _user.IsCheckHQ = false;
                    _context.Users.Update(_user);
                    await _context.SaveChangesAsync();
                    return await Task.FromResult(false);

                }



            }
            else
            {
                return await Task.FromResult(_user.IsCheckHQ.Value);

            }
        }

        public List<string> GetAllGroups(string _ldapServer, string _ldapAccountlogin, string _ldapPassword, string _ldapDistinguishedName)
        {
            List<string> groups = new List<string>();

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = "(objectClass=group)";
                        searcher.PropertiesToLoad.Add("sAMAccountName");

                        SearchResultCollection results = searcher.FindAll();
                        try
                        {
                            foreach (SearchResult result in results)
                            {
                                if (result.Properties["sAMAccountName"].Count > 0)
                                {
                                    groups.Add(result.Properties["sAMAccountName"][0].ToString());
                                }
                            }
                        }
                        finally
                        {
                            results.Dispose();
                        }
                    }
                }

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving groups from LDAP");
                return groups;
            }
        }

        public bool IsUserInGroups(string username, List<string> groupNames, string _ldapServer, string _ldapAccountlogin, string _ldapPassword, string _ldapDistinguishedName)
        {
            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword))
                {
                    string userDn = null;

                    // Get user's distinguished name
                    using (DirectorySearcher userSearcher = new DirectorySearcher(entry))
                    {
                        userSearcher.Filter = $"(sAMAccountName={username})";
                        userSearcher.PropertiesToLoad.Add("distinguishedName");

                        SearchResult userResult = userSearcher.FindOne();
                        if (userResult == null) return false;
                        userDn = userResult.Properties["distinguishedName"][0].ToString();
                    }

                    // Check each group for membership
                    foreach (var groupName in groupNames)
                    {
                        using (DirectorySearcher groupSearcher = new DirectorySearcher(entry))
                        {
                            groupSearcher.Filter = $"(&(objectClass=group)(sAMAccountName={groupName}))";
                            groupSearcher.PropertiesToLoad.Add("member");

                            SearchResult groupResult = groupSearcher.FindOne();
                            if (groupResult != null && groupResult.Properties["member"].Contains(userDn))
                                return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user group membership");
                return false;
            }
        }

        public List<string> GetUserGroups(string username, string _ldapServer, string _ldapAccountlogin, string _ldapPassword, string _ldapDistinguishedName)
        {
            List<string> groups = new List<string>();

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = $"(sAMAccountName={username})";
                        searcher.PropertiesToLoad.Add("memberOf");

                        SearchResult result = searcher.FindOne();
                        if (result == null) return groups;

                        // Extract group names from the memberOf property
                        foreach (string groupDn in result.Properties["memberOf"])
                        {
                            // Extract CN from the DN (Distinguished Name)
                            // DN format: CN=GroupName,OU=...,DC=...
                            int cnIndex = groupDn.IndexOf("CN=", StringComparison.OrdinalIgnoreCase);
                            if (cnIndex >= 0)
                            {
                                int commaIndex = groupDn.IndexOf(',', cnIndex);
                                if (commaIndex > 0)
                                {
                                    string groupName = groupDn.Substring(cnIndex + 3, commaIndex - cnIndex - 3);
                                    groups.Add(groupName);
                                }
                                else
                                {
                                    string groupName = groupDn.Substring(cnIndex + 3);
                                    groups.Add(groupName);
                                }
                            }
                        }
                    }
                }

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving groups for user {username}");
                return groups;
            }
        }

        public async Task<bool> CreateOrUpdateGroupUserEntities(
                                                        int entityId,
                                                        string itemId,
                                                        List<int> customGroupIds,
                                                        List<int> ldapGroupIds)
        {
            // ===== 1) Handle Custom Groups =====
            var existingCustomGroups = await _context.GroupUsersEntities
                .Where(x => x.EntityId == entityId && x.ItemId == itemId)
                .ToListAsync();

            // remove groups not in the new list
            var toRemoveCustom = existingCustomGroups
                .Where(x => !customGroupIds.Contains((int)x.GroupId))
                .ToList();

            _context.GroupUsersEntities.RemoveRange(toRemoveCustom);

            // add new ones
            var toAddCustom = customGroupIds
                .Where(gid => !existingCustomGroups.Any(e => e.GroupId == gid))
                .Select(gid => new GroupUsersEntities
                {
                    EntityId = entityId,
                    ItemId = itemId,
                    GroupId = gid,
                    IsActive = true,
                    IsDelete = false
                }).ToList();

            await _context.GroupUsersEntities.AddRangeAsync(toAddCustom);

            // ===== 2) Handle LDAP Groups =====
            var existingLdapGroups = await _context.GroupLDAPUsersEntities
                .Where(x => x.EntityId == entityId && x.ItemId == itemId)
                .ToListAsync();

            var toRemoveLdap = existingLdapGroups
                .Where(x => !ldapGroupIds.Contains((int)x.GroupLDAPId))
                .ToList();

            _context.GroupLDAPUsersEntities.RemoveRange(toRemoveLdap);

            var toAddLdap = ldapGroupIds
                .Where(gid => !existingLdapGroups.Any(e => e.GroupLDAPId == gid))
                .Select(gid => new GroupLDAPUsersEntities
                {
                    EntityId = entityId,
                    ItemId = itemId,
                    GroupLDAPId = gid,
                    IsActive = true,
                    IsDelete = false
                }).ToList();

            await _context.GroupLDAPUsersEntities.AddRangeAsync(toAddLdap);

            // Save all changes once
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsUserInAnyGroup(string userId, int entityId, string itemId)
        {
            // Check custom groups
            var customMatch = await (from g in _context.GroupUsersEntities
                                     join ug in _context.GroupUsers on g.GroupId equals ug.GroupId
                                     where g.EntityId == entityId &&
                                           g.ItemId == itemId &&
                                           ug.UserId == userId
                                     select g).AnyAsync();

            if (customMatch)
                return true;

            // Check LDAP groups

            // 1. Get the username of the user
            var username = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(username))
                return false;


            var ldapGroups = await (from g in _context.GroupLDAPUsersEntities
                                    join lg in _context.GroupsLDAP on g.GroupLDAPId equals lg.Id
                                    where g.EntityId == entityId &&
                                          g.ItemId == itemId
                                    select g.GroupLDAPId.ToString()).ToListAsync();

            var ldapMatch = IsUserInGroups(username, ldapGroups, _ldapServer, _ldapAccountlogin, _ldapPassword, _ldapDistinguishedName);


            return ldapMatch;
        }


        public async Task<List<int>> GetUserEntityItemIdsAsync(string userId, int entityId)
        {
            var result = new List<int>();

            try
            {
                _logger.LogInformation("Start GetUserEntityItemIdsAsync for user " + userId);
                // -------------------------------
                // 1. Get custom group permissions
                // -------------------------------
                var customItemIds = await (
                    from gu in _context.GroupUsers
                    join gue in _context.GroupUsersEntities
                        on gu.GroupId equals gue.GroupId
                    where gu.UserId == userId && gue.EntityId == entityId
                    select gue.ItemId
                ).ToListAsync();

                _logger.LogInformation("customItemIds count : " + customItemIds.Count);

                // Parse to int (if ItemId stored as string)
                result.AddRange(customItemIds.Select(id => int.Parse(id)));

                // -------------------------------
                // 2. Get LDAP group permissions
                // -------------------------------
                var username = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.UserName)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(username))
                {
                    // Get all LDAP groups for this user
                    var userLdapGroups = GetUserGroups(username, _ldapServer, _ldapAccountlogin, _ldapPassword, _ldapDistinguishedName);

                    if (userLdapGroups?.Any() == true)
                    {
                        // Map group names -> IDs in GroupsLDAP
                        var ldapGroupIds = await _context.GroupsLDAP
                            .Where(lg => userLdapGroups.Contains(lg.NameEn))
                            .Select(lg => lg.Id)
                            .ToListAsync();

                        if (ldapGroupIds.Any())
                        {
                            var ldapItemIds = await _context.GroupLDAPUsersEntities
                                .Where(g => g.EntityId == entityId && ldapGroupIds.Contains((int)g.GroupLDAPId))
                                .Select(g => g.ItemId)
                                .ToListAsync();
                            _logger.LogInformation("ldapItemIds count : " + ldapItemIds.Count);

                            result.AddRange(ldapItemIds.Select(id => int.Parse(id)));
                        }
                    }
                }
                // -------------------------------
                // 3. Return distinct ItemIds
                // -------------------------------
                _logger.LogInformation("end GetUserEntityItemIdsAsync for user " + userId + " items count " + result.Distinct().ToList().Count);

                return result.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(" GetUserEntityItemIdsAsync error  " + ex.Message);
                return result;
            }

        }


        public async Task<GroupAssignmentsDto> GetGroupUserEntitiesAsync(int entityId, string itemId)
        {
            var result = new GroupAssignmentsDto();

            // Get custom groups
            result.CustomGroupIds = await _context.GroupUsersEntities
                .Where(x => x.EntityId == entityId && x.ItemId == itemId)
                .Select(x => x.GroupId ?? 0) // safeguard in case GroupId is nullable
                .ToListAsync();

            // Get LDAP groups
            result.LdapGroupIds = await _context.GroupLDAPUsersEntities
                .Where(x => x.EntityId == entityId && x.ItemId == itemId)
                .Select(x => x.GroupLDAPId ?? 0)
                .ToListAsync();

            return result;
        }


            public List<LdapUserDto> GetAllLdapUsers(string[] groupNames = null)
            {
                var users = new List<LdapUserDto>();

                try
                {
                    using (var entry = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword))
                    {
                        using (var searcher = new DirectorySearcher(entry))
                        {
                            // Build the base filter
                            string filter = "(objectClass=user)";

                            // Add group filtering if specified
                            if (groupNames != null && groupNames.Length > 0)
                            {
                                var groupFilter = string.Join("", groupNames.Select(g =>
                                    $"(memberOf=CN={g},OU=Groups,DC=nupco,DC=com)"));
                                filter = $"(&{filter}(|{groupFilter}))";
                            }

                            searcher.Filter = filter;

                            // Request only the properties we need
                            searcher.PropertiesToLoad.AddRange(new[] {
                        "sAMAccountName",
                        "givenName",
                        "sn",
                        "mail",
                        "department"
                    });

                            // Handle pagination for large directories
                            searcher.PageSize = 1000;

                            var results = searcher.FindAll();
                            try
                            {
                                foreach (SearchResult result in results)
                                {
                                    var user = new LdapUserDto
                                    {
                                        UserName = GetPropertyValue(result, "sAMAccountName"),
                                        FirstName = GetPropertyValue(result, "givenName"),
                                        LastName = GetPropertyValue(result, "sn"),
                                        Email = GetPropertyValue(result, "mail"),
                                        Department = GetPropertyValue(result, "department")
                                    };

                                    if (!string.IsNullOrEmpty(user.UserName))
                                        users.Add(user);
                                }
                            }
                            finally
                            {
                                results.Dispose();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving users from LDAP");
                }

                return users;
            }

        private List<string> GetGroupDistinguishedNames(string[] groupNames, DirectoryEntry entry)
        {
            var dns = new List<string>();

            using (DirectorySearcher groupSearcher = new DirectorySearcher(entry))
            {
                groupSearcher.Filter = "(objectClass=group)";
                groupSearcher.PropertiesToLoad.Add("distinguishedName");
                groupSearcher.PropertiesToLoad.Add("sAMAccountName");

                foreach (string groupName in groupNames)
                {
                    groupSearcher.Filter = $"(&(objectClass=group)(sAMAccountName={groupName}))";
                    SearchResult result = groupSearcher.FindOne();
                    if (result != null)
                    {
                        dns.Add(GetPropertyValue(result, "distinguishedName"));
                    }
                }
            }

            return dns;
        }

        private string GetPropertyValue(SearchResult result, string propertyName)
        {
            if (result.Properties.Contains(propertyName) &&
                result.Properties[propertyName].Count > 0)
            {
                return result.Properties[propertyName][0].ToString();
            }
            return null;
        }



        public List<LdapUserDto> GetUsersManagedBy(string managerIdentifier)
        {
            var employees = new List<LdapUserDto>();

            try
            {
                using (var entry = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword))
                {
                    // STEP 1: Find Manager DN
                    string managerDN = GetUserDistinguishedName(entry, managerIdentifier);
                    if (managerDN == null)
                    {
                        _logger.LogWarning($"Manager not found: {managerIdentifier}");
                        return employees;
                    }

                    using (var searcher = new DirectorySearcher(entry))
                    {
                        // STEP 2: Query all users that have this manager
                        searcher.Filter = $"(&(objectClass=user)(manager={managerDN}))";

                        searcher.PropertiesToLoad.AddRange(new[] {
                    "sAMAccountName",
                    "givenName",
                    "sn",
                    "mail",
                    "department",
                    "employeeID"
                });

                        searcher.PageSize = 1000;

                        var results = searcher.FindAll();

                        try
                        {
                            foreach (SearchResult result in results)
                            {
                                var user = new LdapUserDto
                                {
                                    UserName = GetPropertyValue(result, "sAMAccountName"),
                                    FirstName = GetPropertyValue(result, "givenName"),
                                    LastName = GetPropertyValue(result, "sn"),
                                    Email = GetPropertyValue(result, "mail"),
                                    Department = GetPropertyValue(result, "department"),
                                    Code = GetPropertyValue(result, "employeeID")
                                };

                                if (!string.IsNullOrEmpty(user.UserName))
                                    employees.Add(user);
                            }
                        }
                        finally
                        {
                            results.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users managed by LDAP user");
            }

            return employees;
        }

        public List<LdapUserDto> GetUsersOutSourceManagedBy(string managerIdentifier)
        {
            var employees = new List<LdapUserDto>();

            try
            {
                using (var entry = new DirectoryEntry(_ldapServer, _ldapAccountlogin, _ldapPassword))
                {
                    // STEP 1: Get Manager Distinguished Name
                    string managerDN = GetUserDistinguishedName(entry, managerIdentifier);
                    if (string.IsNullOrWhiteSpace(managerDN))
                    {
                        _logger.LogWarning($"Manager not found: {managerIdentifier}");
                        return employees;
                    }

                    using (var searcher = new DirectorySearcher(entry))
                    {
                        // Get all users managed by this manager (no email filtering in LDAP)
                        searcher.Filter = $"(&(objectClass=user)(manager={managerDN}))";

                        searcher.PropertiesToLoad.AddRange(new[]
                        {
                    "sAMAccountName",
                    "givenName",
                    "sn",
                    "mail",
                    "department",
                    "employeeID"
                });

                        searcher.PageSize = 1000;

                        using (var results = searcher.FindAll())
                        {
                            foreach (SearchResult result in results)
                            {
                                employees.Add(new LdapUserDto
                                {
                                    UserName = GetPropertyValue(result, "sAMAccountName"),
                                    FirstName = GetPropertyValue(result, "givenName"),
                                    LastName = GetPropertyValue(result, "sn"),
                                    Email = GetPropertyValue(result, "mail"),
                                    Department = GetPropertyValue(result, "department"),
                                    Code = GetPropertyValue(result, "employeeID")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving LDAP users managed by user");
            }

            // FILTER IN MEMORY: Internal + Email contains "-"
            var _emplyess = employees
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Email) &&
                    x.Email.EndsWith("@nupco.com", StringComparison.OrdinalIgnoreCase) &&
                    x.Email.Contains("-c"))
                .ToList();

            return _emplyess;
        }

        private string GetUserDistinguishedName(DirectoryEntry entry, string identifier)
        {
            using (var searcher = new DirectorySearcher(entry))
            {
                // Match either username OR email
                searcher.Filter = $"(|(sAMAccountName={identifier})(mail={identifier}))";
                searcher.PropertiesToLoad.Add("distinguishedName");

                var result = searcher.FindOne();
                if (result != null)
                {
                    return GetPropertyValue(result, "distinguishedName");
                }
            }

            return null;
        }

    }

}

