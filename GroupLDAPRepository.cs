using Core.Helpers;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using DocumentFormat.OpenXml.Spreadsheet;
using static Nupco.Core.Helpers.JWTHelper;
using GroupLDAP = Nupco.DAL.Models.GroupLDAP;
using DocumentFormat.OpenXml.InkML;
namespace Nupco.EF.Repositories
{
    internal class GroupLDAPRepository : BaseRepository<GroupLDAP>, IGroupLDAPRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly GroupLDAP_Assembler _GroupLDAP_Assembler;

        private string[] stringArray = { "GroupLDAP" };


        public GroupLDAPRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _GroupLDAP_Assembler = new GroupLDAP_Assembler();
            _logger = logger;
            _context = context;

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

        public async Task<OperationOutput> AddNewAsync(GroupLDAPDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                var _model = _GroupLDAP_Assembler.WriteDal(entity);
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
                var spec = Specification<GroupLDAP>.All.And(new GroupLDAPSpecification(_filter));

                var Group = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                var _Group = _GroupLDAP_Assembler.WriteListDto(Group);
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
                var _entities = _GroupLDAP_Assembler.WriteListDto(entities);

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
                    var _Group = _GroupLDAP_Assembler.WriteDto(Group);
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


        public async Task<OperationOutput> UpdateEntity(GroupLDAPDto entity)
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



        public async Task<int?> GroupIsFound(string groupName)
        {
            var group = await _context.GroupsLDAP.FirstOrDefaultAsync(f => f.NameEn.ToLower() == groupName.ToLower());
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


        public async Task<OperationOutput> GetAllGroupUsersById(int? groupId)
        {
            try
            {
                var groups = await _context.GroupUsers.Where(i => i.GroupId == groupId)
                    .Include(i => i.Group).Include(i => i.User).ToListAsync();

                var groupUsersList = groups.Select(s => new
                {
                    Id = s.Id,
                    userName = s.User.UserName,
                    groupNameAr = s.Group.NameAr,
                    groupNameEn = s.Group.NameEn,
                    GroupId = s.GroupId,
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

        public async Task<OperationOutput> GetAllUserLDAPWithOutGroupUser(int? groupId)
        {
            try
            {
                // All users NOT in the selected group
                var usersWithoutGroup = await _context.Users
                    .Where(u => !_context.GroupUsers
                        .Any(gu => gu.GroupId == groupId && gu.UserId == u.Id)).Select(s => new
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
                if (group != null)
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

        public Task<OperationOutput> GetAllGroupLDAPUsersById(int? groupId)
        {
            throw new NotImplementedException();
        }

        public Task<OperationOutput> GetAllUserWithOutGroupLDAPUser(int? groupId)
        {
            throw new NotImplementedException();
        }

        public Task<OperationOutput> DeleteGroupLDAPUserByUserId(int? groupId, string? userId)
        {
            throw new NotImplementedException();
        }
    }
}
