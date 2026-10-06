using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.GroupLDAP;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System.DirectoryServices;
using Group = Nupco.DAL.Models.Group;
namespace Nupco.EF.Repositories
{
    internal class GroupRepository : BaseRepository<Group>, IGroupRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly ILDAPRepository _lDAPRepository;
        private readonly IUserRepository _userRepository;
        private readonly Group_Assembler _Group_Assembler;

        private string[] stringArray = { "Group" };


        public GroupRepository(ApplicationDbContext context, ILogger logger, ILDAPRepository lDAPRepository, IUserRepository userRepository) : base(context, logger)
        {
            _Group_Assembler = new Group_Assembler();
            _logger = logger;
            _context = context;
            _lDAPRepository = lDAPRepository;
            _userRepository = userRepository;
        }

        public async Task<OperationOutput> ActivatedOrUnActivated(int id, bool activate)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsActive = activate;
                Update(find);
                await SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewAsync(GroupDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                var _model = _Group_Assembler.WriteDal(entity);
                var _entity = await AddAsync(_model);
                await _context.SaveChangesAsync();


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entity, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> DeleteEntity(int id, string userId)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsDeleted = true;
                Update(find);
                await SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                var spec = Specification<Group>.All.And(new GroupSpecification(_filter));

                var Group = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                var _Group = _Group_Assembler.WriteListDto(Group);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Group, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {

                var entities = await FindAllAsync(i => i.IsDeleted == false);
                var _entities = _Group_Assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var Group = await GetByIdAsync(id);
                if (Group != null)
                {
                    var _Group = _Group_Assembler.WriteDto(Group);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Group, 1);
                    return _result;
                }
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(GroupDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.NameAr = entity.NameAr;
                    _entity.NameEn = entity.NameEn;

                    var _model = Update(_entity);
                    await SaveChangesAsync();

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_model, 1);
                    return _result;
                }

                else
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(GroupDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<GroupDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<GroupDto> entities)
        {
            throw new NotImplementedException();
        }

        ///// Group Users ///////////////   
        public async Task<OperationOutput> CreateGroupWithGroupUsers(GroupWithUsersObjDto dto, LdapDto ldapDto)
        {
            try
            {
                if (dto?.GroupsUsers == null || !dto.GroupsUsers.Any())
                {
                    return ResultOutputData.GenearetResultOutputSuccess();
                }

                ResultOutputData result = new ResultOutputData();

                foreach (var user in dto.GroupsUsers)
                {
                    string _username = user.UserName.Split('@')[0];
                    var userId = await UserIsFound(_username);

                    if (string.IsNullOrEmpty(userId))
                    {
                        var _userInfo = _lDAPRepository.GetUserDetails(_username, ldapDto.LdapServer, ldapDto.LdapAccountLogin, ldapDto.LdapPassword, ldapDto.LdapDistinguishedName);
                        var _user = await _userRepository.CreateUserFromImportSheet(_userInfo);
                        userId = _user.Id;
                    }

                    if (!string.IsNullOrEmpty(userId))
                    {
                        if (dto.GroupId != null)
                        {
                            bool? isFoundGroupItem = await UserIsFoundGroupUser(userId, dto.GroupId);
                            if (isFoundGroupItem == false)
                            {
                                var _entity = new GroupUsers { GroupId = dto.GroupId.Value, UserId = userId };
                                await _context.GroupUsers.AddAsync(_entity);
                            }
                        }
                        else if (!string.IsNullOrEmpty(user.GroupName))
                        {
                            var _listGroups = user.GroupName.Split(',').Select(g => g.Trim()).ToList();
                            foreach (var groupName in _listGroups)
                            {
                                var _groupId = await GroupIsFound(groupName);
                                if(_groupId == null)
                                {
                                    var _newGroup = new Group {  NameAr = groupName , NameEn= groupName,IsActive=true,IsDeleted=false };
                                   var _groupSaved =  await _context.Groups.AddAsync(_newGroup);
                                    await _context.SaveChangesAsync();

                                    _groupId = _groupSaved.Entity.Id;
                                }

                                if (_groupId != null)
                                {
                                    bool? isFoundGroupItem = await UserIsFoundGroupUser(userId, _groupId);

                                    if (isFoundGroupItem == false)
                                    {
                                        var _entity = new GroupUsers { GroupId = _groupId.Value, UserId = userId };
                                        await _context.GroupUsers.AddAsync(_entity);
                                    }
                                }
                            }
                        }
                    }
                }

                if (dto.GroupsUsers.Any())
                    await _context.SaveChangesAsync();

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        private async Task<int?> GroupIsFound(string groupName)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(f => f.NameEn.ToLower() == groupName.ToLower());
            return group != null ? group.Id : null;
        }

        private async Task<string?> UserIsFound(string UserName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(f => f.UserName.ToLower() == UserName.ToLower());
            return user != null ? user.Id : null;
        }

        private async Task<bool?> UserIsFoundGroupUser(string? userId, int? groupId)
        {
            var groupIsFound = await _context.GroupUsers.FirstOrDefaultAsync(f => f.GroupId == groupId && f.UserId == userId);
            return groupIsFound != null ? true : false;
        }

        public List<LdapUserDto> GetUsersByGroupUsingUserScan(string groupName, LdapDto ldapDto)
        {
            var users = new List<LdapUserDto>();

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(ldapDto.LdapServer, ldapDto.LdapAccountLogin, ldapDto.LdapPassword))
                using (DirectorySearcher searcher = new DirectorySearcher(entry))
                {
                    // 🔥 Get ALL users
                    searcher.Filter = "(objectClass=user)";

                    searcher.PropertiesToLoad.AddRange(new[]
                    {
                "sAMAccountName",
                "givenName",
                "sn",
                "mail",
                "department",
                "memberOf"
            });

                    searcher.PageSize = 1000;

                    var results = searcher.FindAll();

                    try
                    {
                        foreach (SearchResult result in results)
                        {
                            if (result.Properties["sAMAccountName"].Count == 0)
                                continue;

                            var userGroups = new List<string>();

                            // 🔥 Extract groups same as your method
                            foreach (string groupDn in result.Properties["memberOf"])
                            {
                                var group = ExtractCn(groupDn);
                                if (!string.IsNullOrEmpty(group))
                                    userGroups.Add(group);
                            }

                            // 🔥 Check if user belongs to target group
                            if (userGroups.Any(g => g.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
                            {
                                users.Add(new LdapUserDto
                                {
                                    UserName = result.Properties["sAMAccountName"][0]?.ToString(),
                                    FirstName = GetProp(result, "givenName"),
                                    LastName = GetProp(result, "sn"),
                                    Email = GetProp(result, "mail"),
                                    Department = GetProp(result, "department")
                                });
                            }
                        }
                    }
                    finally
                    {
                        results.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving users for group {groupName}");
            }

            return users;
        }

        private string ExtractCn(string dn)
        {
            int cnIndex = dn.IndexOf("CN=", StringComparison.OrdinalIgnoreCase);
            if (cnIndex < 0) return null;

            int commaIndex = dn.IndexOf(',', cnIndex);
            return commaIndex > 0
                ? dn.Substring(cnIndex + 3, commaIndex - cnIndex - 3)
                : dn.Substring(cnIndex + 3);
        }

        private string GetProp(SearchResult result, string prop)
        {
            return result.Properties[prop]?.Count > 0
                ? result.Properties[prop][0]?.ToString()
                : null;
        }

        public List<string> GetUsersFromLdapGroup(
    string groupName, LdapDto ldapDto)
        {
            var members = new List<string>();

            using (var entry = new DirectoryEntry(ldapDto.LdapServer, ldapDto.LdapAccountLogin, ldapDto.LdapPassword))
            {
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=group)(sAMAccountName={groupName}))";
                    searcher.PropertiesToLoad.Add("member");

                    var result = searcher.FindOne();
                    if (result == null)
                        return members;

                    foreach (var member in result.Properties["member"])
                    {
                        members.Add(member.ToString()); // DN
                    }
                }
            }

            return members;
        }
        //public List<LdapUserDto> GetUsersFromLdapGroup(string groupName, LdapDto ldapDto)
        //{
        //    var users = new List<LdapUserDto>();

        //    try
        //    {
        //        using (DirectoryEntry entry = new DirectoryEntry(ldapDto.LdapServer, ldapDto.LdapAccountLogin, ldapDto.LdapPassword))
        //        {
        //            using (DirectorySearcher searcher = new DirectorySearcher(entry))
        //            {
        //                // ✅ IMPORTANT: use sAMAccountName (same as your group method)
        //                searcher.Filter = $"(&(objectClass=user)(memberOf=CN={groupName},{ldapDto.LdapDistinguishedName}))";

        //                searcher.PropertiesToLoad.AddRange(new[]
        //                {
        //            "sAMAccountName",
        //            "givenName",
        //            "sn",
        //            "mail",
        //            "department"
        //        });

        //                searcher.PageSize = 1000;

        //                var results = searcher.FindAll();

        //                try
        //                {
        //                    foreach (SearchResult result in results)
        //                    {
        //                        if (result.Properties["sAMAccountName"].Count == 0)
        //                            continue;

        //                        users.Add(new LdapUserDto
        //                        {
        //                            UserName = result.Properties["sAMAccountName"][0]?.ToString(),
        //                            FirstName = result.Properties["givenName"].Count > 0
        //                                ? result.Properties["givenName"][0]?.ToString()
        //                                : null,
        //                            LastName = result.Properties["sn"].Count > 0
        //                                ? result.Properties["sn"][0]?.ToString()
        //                                : null,
        //                            Email = result.Properties["mail"].Count > 0
        //                                ? result.Properties["mail"][0]?.ToString()
        //                                : null,
        //                            Department = result.Properties["department"].Count > 0
        //                                ? result.Properties["department"][0]?.ToString()
        //                                : null
        //                        });
        //                    }
        //                }
        //                finally
        //                {
        //                    results.Dispose();
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error retrieving users from group: {groupName}");
        //    }

        //    return users;
        //}


        // 🔥 MAIN METHOD
        //public async Task<List<LdapUserDto>> GetUsersFromLdapGroup(string groupName, LdapDto ldapDto)
        //{
        //    return await Task.Run(() =>
        //    {
        //        var users = new List<LdapUserDto>();

        //        try
        //        {
        //            using var entry = new DirectoryEntry(ldapDto.LdapServer, ldapDto.LdapAccountLogin, ldapDto.LdapPassword);

        //            using var searcher = new DirectorySearcher(entry);

        //            // 🔥 STEP 1: Resolve group DNs
        //            var groupDns = new List<string>();

        //            var dn = GetGroupDn(groupName, ldapDto);
        //            //foreach (var groupName in groupNames)
        //            //{
        //            //    if (!string.IsNullOrEmpty(dn))
        //            //        groupDns.Add(dn);
        //            //    else
        //            //        _logger.LogWarning($"Group not found in LDAP: {groupName}");
        //            //}
        //            groupDns.Add(dn);

        //            if (!groupDns.Any())
        //            {
        //                _logger.LogWarning("No valid group DNs found.");
        //                return users;
        //            }

        //            // 🔥 STEP 2: Build recursive filter (handles nested groups)
        //            var groupFilter = string.Join("", groupDns.Select(dn =>
        //                $"(memberOf:1.2.840.113556.1.4.1941:={dn})"));

        //            var filter = $"(&(objectClass=user)(|{groupFilter}))";

        //            searcher.Filter = filter;

        //            _logger.LogInformation($"LDAP Filter: {filter}");

        //            // 🔥 STEP 3: Load required properties
        //            searcher.PropertiesToLoad.AddRange(new[]
        //            {
        //            "sAMAccountName",
        //            "givenName",
        //            "sn",
        //            "mail",
        //            "department"
        //        });

        //            // 🔥 STEP 4: Pagination
        //            searcher.PageSize = 1000;

        //            using var results = searcher.FindAll();

        //            foreach (SearchResult result in results)
        //            {
        //                var userName = GetProperty(result, "sAMAccountName");

        //                if (string.IsNullOrEmpty(userName))
        //                    continue;

        //                users.Add(new LdapUserDto
        //                {
        //                    UserName = userName,
        //                    FirstName = GetProperty(result, "givenName"),
        //                    LastName = GetProperty(result, "sn"),
        //                    Email = GetProperty(result, "mail"),
        //                    Department = GetProperty(result, "department")
        //                });
        //            }

        //            _logger.LogInformation($"LDAP Users Retrieved: {users.Count}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "LDAP Error while fetching users.");
        //        }

        //        return users;
        //    });
        //}

        // 🔥 GET GROUP DN DYNAMICALLY
        private string GetGroupDn(string groupName, LdapDto ldapDto)
        {
            try
            {
                using var entry = new DirectoryEntry(ldapDto.LdapServer, ldapDto.LdapAccountLogin, ldapDto.LdapPassword);

                using var searcher = new DirectorySearcher(entry);

                searcher.Filter = $"(&(objectClass=group)(cn={groupName}))";
                searcher.PropertiesToLoad.Add("distinguishedName");

                var result = searcher.FindOne();

                return result?.Properties["distinguishedName"]?.Count > 0
                    ? result.Properties["distinguishedName"][0].ToString()
                    : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting DN for group: {groupName}");
                return null;
            }
        }

        // 🔥 SAFE PROPERTY ACCESS
        private string GetProperty(SearchResult result, string propertyName)
        {
            if (result.Properties.Contains(propertyName) &&
                result.Properties[propertyName].Count > 0)
            {
                return result.Properties[propertyName][0]?.ToString();
            }

            return null;
        }
        public async Task<OperationOutput> GetAllGroupUsersById(int? groupId)
        {
            try
            {
                var groups = await _context.GroupUsers.Where(i => i.GroupId == groupId)
                    .Include(i=> i.Group).Include(i=> i.User).ToListAsync();

                var groupUsersList = groups.Select(s => new
                {
                    Id=s.Id,
                    userName = s.User.UserName,
                    groupNameAr = s.Group.NameAr,
                    groupNameEn = s.Group.NameEn,
                    GroupId=s.GroupId,
                    UserId = s.UserId
                }).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(groupUsersList, groupUsersList.Count());
                return _result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;

            }

        }
        
        public async Task<OperationOutput> GetUllUserWithOutGroupUser(int? groupId)
        {
            try
            {
                // All users NOT in the selected group
                var usersWithoutGroup = await _context.Users
                    .Where(u => !_context.GroupUsers
                        .Any(gu => gu.GroupId == groupId && gu.UserId == u.Id)).Select(s=> new
                        {
                            s.Id,
                            s.UserName
                        })
                    .ToListAsync();


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(usersWithoutGroup, usersWithoutGroup.Count());
                return _result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;

            }

        }

       public async Task<OperationOutput> DeleteGroupUserByUserId(int? groupId, string? userId)
        {
            try
            {
                var group = await _context.GroupUsers.FirstOrDefaultAsync(i => i.GroupId == groupId && i.UserId == userId);
                if(group != null)
                {
                     _context.GroupUsers.Remove(group);
                    await _context.SaveChangesAsync();

                    var Result = ResultOutputData.GenearetResultOutputSuccess();
                    return Result;
                }
                else
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        //public async Task<OperationOutput> UpdateGroupWithGroupUsers(GroupWithGroupUsers groupWithGroupUsers)
        //{
        //    try
        //    {
        //        var group =await _context.Groups.FirstOrDefaultAsync(i => i.Id == groupWithGroupUsers.group.Id.Value);

        //        if (group is not null)
        //        {
        //            group.Id = groupWithGroupUsers.group.Id.Value;
        //            group.NameAr = groupWithGroupUsers.group.NameAr;
        //            group.NameEn = groupWithGroupUsers.group.NameEn;
        //            _context.Groups.Update(group);


        //            var Result = await UpdateGroupUsers(group.Id, groupWithGroupUsers.groupUsers);
        //            return Result;

        //        }

        //        var _Result = ResultOutputData.GenearetResultOutputNoDataReturned();
        //        return _Result;


        //    }
        //    catch (Exception)
        //    {
        //        var Result = ResultOutputData.GenearetResultOutputCatch();
        //        return Result;
        //    }

        //}

        //public async Task<OperationOutput> UpdateGroupUsers(int groupId ,List<GroupUserObjDto> groupUserObjDto)
        //{

        //    var groupUser = _context.GroupUsers.Any(i => i.GroupId == groupId);
        //    if (groupUser)
        //    {
        //        _context.GroupUsers.RemoveRange(await _context.GroupUsers.Where(i => i.GroupId == groupId).ToListAsync());
        //        await _context.SaveChangesAsync();
        //    }
        //    List<GroupUsers> groupUsers = groupUserObjDto.Select(s => new GroupUsers
        //    {
        //        GroupId = s.GroupId.Value,
        //        UserId = s.UserId,
        //    }).ToList();
        //    _context.GroupUsers.AddRange(groupUsers);
        //    await _context.SaveChangesAsync();



        //    var Result = ResultOutputData.GenearetResultOutputSuccess();
        //    return Result;
        //}


    }
}
