using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models;
using System.Reflection.Emit;
using System.Security.Cryptography;

namespace Nupco.EF.Repositories
{
    public class ActivityRepository :BaseRepository<Activity> , IActivityRepository
    {
        private readonly Activity_Assembler _Assembler;
        private readonly ActivitiesUserAttendance_Assembler _Assembler_Attendance;
        private readonly IConfiguration _configuration;
        private readonly User_Assembler _Assembler_User;
        private readonly ILDAPRepository _iLDAPRepository;

        public ActivityRepository(ApplicationDbContext context, ILogger logger, ILDAPRepository iLDAPRepository) : base(context, logger)
        {
            _Assembler = new Activity_Assembler();
            _Assembler_Attendance = new ActivitiesUserAttendance_Assembler();
            _Assembler_User = new User_Assembler();

            _logger = logger;
            _context = context;
            _iLDAPRepository = iLDAPRepository;
        }


        public async Task<OperationOutput> AddNewAsync(ActivityDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;

                if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64))
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPic;
                }
                if (entityDto.OriginalPicBase64Attchments.Count > 0)
                {


                    entityDto.Attchments = new List<string>();
                    foreach (var item in entityDto.OriginalPicBase64Attchments)
                    {
                        var _attch = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(item) ?
                                                    Images.SaveSingleImageOnServer(item, null, entityDto.pathToSave, false, 0, entityDto.pathToSave) : null;
                        entityDto.Attchments.Add(_attch);
                    }
                }

                var _timeStart = DateTime.Parse(entityDto.TimeStartString);
                var _timeEnd = DateTime.Parse(entityDto.TimeEndString);
                var _entity = _Assembler.WriteDal(entityDto);

                _entity.TimeStart = _timeStart;
                _entity.TimeEnd = _timeEnd;

                _entity.Code = await GenerateUniqueActivityCodeAsync();

                _entity.PicQRCode = Nupco.Core.Helpers.Strings.GenereteQRCode(_entity.Code);

                var _entityDto = await AddAsync(_entity);
                await SaveChangesAsync();
                entityDto.Id = _entityDto.Id;

                await _iLDAPRepository.CreateOrUpdateGroupUserEntities((int)_entityDto.EntityId, _entityDto.Id.ToString(), entityDto.customGroupIds, entityDto.ldapGroupIds);


                foreach (var _att in entityDto.Attchments)
                {
                    var att = new DAL.Models.Attachment();
                    att.NameAr = _entityDto.TitleAr;
                    att.NameEn = _entityDto.TitleEn;
                    att.Url = _att;
                    att.EntityId = _entityDto.EntityId;
                    att.ItemId = _entityDto.Id;
                    att.Extention = _att.Split(".")[1];
                    att.CreatedDate = DateTime.Now;
                    _context.Attachments.Add(att);
                    _context.SaveChanges();

                }

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entity, 1);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<string> GenerateUniqueActivityCodeAsync()
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

                bool exists = await _context.Activities.AnyAsync(a => a.Code == code);
                if (!exists)
                {
                    return code;
                }
            }

            throw new Exception("Unable to generate a unique activity code after multiple attempts.");
        }



        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<ActivityDto> entitiesDto)
        {
            try
            {
                var _entities = _Assembler.WriteListDal(entitiesDto);
                var _entitiesDto = await AddRangeAsync(_entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entitiesDto, 1);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {
                var _entities = await FindAllAsync(i => i.IsDeleted == false);
                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                var spec = Specification<Activity>.All.And(new ActivitySpecification(_filter));

                var Activitys =await _context.Activities.Where(spec.ToExpression())
                                             .Where(i => i.IsDeleted == false).OrderByDescending(o => o.Id)
                                             .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                                             .AsNoTrackingWithIdentityResolution()
                                             .ToListAsync();

                var _Activitys = _Assembler.WriteListDto(Activitys);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Activitys, counts);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            try
            {
                var Activitys = await FindAllAsync(f=> f.CreatedBy==userId);
                var _Activitys = _Assembler.WriteListDto(Activitys);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Activitys, 1);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(ActivityDto entityDto)
        {
            try
            {
                if (entityDto.isPicChanged == true)
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPic;
                }
                if (entityDto.isAttcChanged == true && entityDto.OriginalPicBase64Attchments.Count > 0)
                {


                    entityDto.Attchments = new List<string>();
                    foreach (var item in entityDto.OriginalPicBase64Attchments)
                    {
                        var _attch = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(item) ?
                                                    Images.SaveSingleImageOnServer(item, null, entityDto.pathToSave, false, 0, entityDto.pathToSave) : null;
                        entityDto.Attchments.Add(_attch);
                    }
                }

                if (entityDto.Attchments.Count > 0)
                {
                    var findAttch = await _context.Attachments.Where(f => f.EntityId == entityDto.EntityId && f.ItemId == entityDto.Id).ToListAsync();
                    if (findAttch.Count > 0)
                    {
                        _context.Attachments.RemoveRange(findAttch);
                        _context.SaveChanges();

                    }
                }

                var _timeStart = DateTime.Parse(entityDto.TimeStartString);
                var _timeEnd = DateTime.Parse(entityDto.TimeEndString);


                var _entity = _Assembler.WriteDal(entityDto);
                _entity.UpdatedDate = DateTime.Now;
                _entity.TimeStart = _timeStart;
                _entity.TimeEnd = _timeEnd;
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                if (_entity.Code is null)
                {
                    _entity.Code = await GenerateUniqueActivityCodeAsync();
                    _entity.PicQRCode = Nupco.Core.Helpers.Strings.GenereteQRCode(_entity.Code);
                }
                var entity = Update(_entity);

                await _iLDAPRepository.CreateOrUpdateGroupUserEntities((int)entity.EntityId, entity.Id.ToString(), entityDto.customGroupIds, entityDto.ldapGroupIds);

                await SaveChangesAsync();




                foreach (var _att in entityDto.Attchments)
                {
                    var att = new DAL.Models.Attachment();
                    att.NameAr = entity.TitleAr;
                    att.NameEn = entity.TitleEn;
                    att.Url = _att;
                    att.EntityId = entity.EntityId;
                    att.ItemId = entity.Id;
                    att.Extention = _att.Split(".")[1];
                    att.CreatedDate = DateTime.Now;
                    _context.Attachments.Add(att);
                    _context.SaveChanges();

                }

                var objDto = _Assembler.WriteDto(entity);
                objDto.Attchments = await _context.Attachments.Where(i => i.EntityId == objDto.EntityId && i.ItemId == objDto.Id).Select(s => s.Url).ToListAsync();


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(objDto, 1);

                return _result;

            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<bool> CreateOrUpdateGroupUserEntities(ActivityDto entityDto)
        {
            var groupUsersEntity = await _context.GroupUsersEntities.Where(i => i.EntityId == entityDto.EntityId && i.ItemId == entityDto.Id.ToString()).ToListAsync();

            if (groupUsersEntity.Count == 0 && entityDto.GroupId != null)
            {
                var newGroupUserEntity = new GroupUsersEntities
                {
                    EntityId = entityDto.EntityId,
                    GroupId = entityDto.GroupId,
                    ItemId = entityDto.Id.ToString(),
                    IsActive = true,
                    IsDelete = false
                };
                await _context.GroupUsersEntities.AddAsync(newGroupUserEntity);
            }

            else if (entityDto.GroupId == null && groupUsersEntity.Count == 0)
            {
                var _listGroupUsersEntities = new List<GroupUsersEntities>();
                var _groups = await _context.Groups.Select(s => new GroupUsersEntities
                {
                    GroupId = s.Id,
                    EntityId = entityDto.EntityId,
                    ItemId = entityDto.Id.ToString()
                }).ToListAsync();


                _listGroupUsersEntities = _groups;
                _context.GroupUsersEntities.AddRange(_listGroupUsersEntities);

            }

            else if (entityDto.GroupId == null && groupUsersEntity.Count > 0)
            {

                _context.GroupUsersEntities.RemoveRange(groupUsersEntity);
                //await SaveChangesAsync();


                var _listGroupUsersEntities = new List<GroupUsersEntities>();
                var _groups = await _context.Groups.Select(s => new GroupUsersEntities
                {
                    GroupId = s.Id,
                    EntityId = entityDto.EntityId,
                    ItemId = entityDto.Id.ToString()
                }).ToListAsync();


                _listGroupUsersEntities = _groups;
                _context.GroupUsersEntities.AddRange(_listGroupUsersEntities);

            }

            else if (entityDto.GroupId != null && groupUsersEntity.Count > 0)
            {
                _context.GroupUsersEntities.RemoveRange(groupUsersEntity);
                var newGroupUserEntity = new GroupUsersEntities
                {
                    EntityId = entityDto.EntityId,
                    GroupId = entityDto.GroupId,
                    ItemId = entityDto.Id.ToString(),
                    IsActive = true,
                    IsDelete = false
                };
                await _context.GroupUsersEntities.AddAsync(newGroupUserEntity);

            }

            return true;
        }
        public async Task<bool> CreateOrUpdateGroupUserEntity(ActivityDto entityDto)
        {

            if (entityDto.GroupId != null)
            {
                var groupUsersEntity = await _context.GroupUsersEntities.SingleOrDefaultAsync(i => i.EntityId == entityDto.EntityId && i.ItemId == entityDto.Id.ToString());
                if(groupUsersEntity != null)
                {
                    groupUsersEntity.GroupId = entityDto.GroupId;
                    _context.GroupUsersEntities.Update(groupUsersEntity);

                }
                else
                {
                    var newGroupUserEntity = new GroupUsersEntities
                    {
                        EntityId = entityDto.EntityId,
                        GroupId = entityDto.GroupId,
                        ItemId = entityDto.Id.ToString(),
                        IsActive = true,
                        IsDelete = false
                    };
                    await _context.GroupUsersEntities.AddAsync(newGroupUserEntity);
                    await _context.SaveChangesAsync();

                }

            }
            if (entityDto.GroupId == null)
            {
                var groupUsersEntity = await _context.GroupUsersEntities.SingleOrDefaultAsync(i => i.EntityId == entityDto.EntityId && i.ItemId == entityDto.Id.ToString());
                if (groupUsersEntity is not null)
                {
                    _context.GroupUsersEntities.Remove(groupUsersEntity);
                    await _context.SaveChangesAsync();
                }

            }
            return true;
        }


        public async Task<OperationOutput> UpdateEntityAsync(ActivityDto entityDto, object key)
        {
            try
            {
                var _Activity =await GetByIdAsync(entityDto.Id.Value);
                if (entityDto.IsActive == true)
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPic;
                }

                var _entity = _Assembler.WriteDal(entityDto);
                _entity.UpdatedDate = DateTime.Now;
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _Activity);
                await SaveChangesAsync();


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entity, 1);

                return _result;

            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> DeleteEntity(int id,string userId)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsDeleted = true;
                Update(find);

                var notificationHistory = await _context.NotificationHistories.Where(f => f.RecordId == id.ToString() && f.EntityId == find.EntityId).ToListAsync();
                if (notificationHistory.Any())
                    _context.NotificationHistories.RemoveRange(notificationHistory);

                await SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<ActivityDto> entities)
        {
            try
            {
                foreach (var item in entities)
                {
                    var find = await FindAsync(f => f.Id == item.Id);
                    find.IsDeleted = true;
                    Update(find);
                }

                await SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

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
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }



        public async Task<OperationOutput> GetAllActivitylsByUserJoind(string userId)
        {

            try
            {
                var _ActivitysUsers = await _context.ActivitiesUsers.ToListAsync();

                var _entities = await _context.Activities.Where(f => f.IsDeleted == false).Include(i => i.ActivityUsers).Select(s => new ActivityDto
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    OriginalPic = s.OriginalPic,
                    CreatedDate = s.CreatedDate,
                    IsJoin = s.ActivityUsers != null && s.ActivityUsers.FirstOrDefault().UserId == userId ? true : false

                }).AsNoTrackingWithIdentityResolution().ToListAsync();

                var entities = _entities.OrderByDescending(s => s.IsJoin).DistinctBy(s => s.Id).Select(s => new ActivityResult
                {
                    Activity = s,
                    Count = _ActivitysUsers.Count(c => c.ActivitiesId == s.Id)

                });

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(entities, count);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetActivityJoindById(int activityId)
        {

            try
            {
                var entities = await _context.ActivitiesUsers
                    .Where(i => i.ActivitiesId == activityId)
                    .Include(i => i.User)
                    .Include(i => i.Activities)
                    .ThenInclude(i => i.Entity)
                    .Select(s => new
                    {
                        Email = s.User.Email,
                        userName = s.User.UserName,
                        FullName = s.User.FirstName + " " + s.User.LastName,
                        entityAr = s.Activities.Entity.NameAr,
                        entityEn = s.Activities.Entity.NameEn,
                        ActivitiyAr = s.Activities.TitleAr,
                        ActivitiyEn = s.Activities.TitleEn,
                        BriefeContentAr = s.Activities.BriefeContentAr,
                        BriefeContentEn = s.Activities.BriefeContentEn

                    }).AsNoTrackingWithIdentityResolution().ToListAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(entities, count);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetAllActivitysByUserId(string userId)
        {

            try
            {
                var _ActivitysUsers = await _context.ActivitiesUsers.ToListAsync();
                List<ActivityResult> Activitys = new List<ActivityResult>();
                var _ActivitysByUsers = await _context.Activities.Include(c => c.ActivityUsers).Where(f => f.ActivityUsers.FirstOrDefault().UserId == userId).AsNoTrackingWithIdentityResolution().ToListAsync();
                var ActivitysByUsers = _ActivitysByUsers.Select(s => new ActivityResult
                {
                    Activity = _Assembler.WriteDto(s),
                    Count = _ActivitysUsers.Count(c => c.ActivitiesId == s.Id)
                }).Distinct().ToList();

                Activitys.AddRange(ActivitysByUsers);
                foreach (var item in Activitys)
                {
                    item.Activity.IsJoin = true;
                }

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(Activitys, Activitys.Count);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllByPagenation(FiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "ActivityUsers" };
                var spec = Specification<Activity>.All.And(new ActivitySpecification(_filter));
                var Activitys = new List<Activity>();
                if (_filter.InterestsId > 0)
                {
                    var _Activitys = await FindAllAsync(spec.ToExpression(), stringArray);      
                    Activitys = _Activitys.Where(i => i.IsDeleted == false).ToList();
                }
                else
                {
                    var _ActivityList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray, o => o.ActivityStartDate, OrderBy.Descending);
                    Activitys = _ActivityList.Where(i => i.IsDeleted == false).ToList();
                }


                var _ActivitysDto = _Assembler.WriteListDto(Activitys);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_ActivitysDto, counts);
                return _result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> JoinActivityByUser(ActivitiesUserDto model)
        {
            try
            {
                // var _Activity = _Assembler.WriteDal(model);
                var _Activity = new ActivityUser()
                {
                    ActivitiesId = model.ActivitiesId,
                    UserId = model.UserId,
                    IsDeleted = false,
                    
                };

                await _context.ActivitiesUsers.AddAsync(_Activity);
                await _context.SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> LeaveActivityByUser(ActivitiesUserDto model)
        {
            try
            {


                var _Activity = await _context.ActivitiesUsers.FirstOrDefaultAsync(i => i.ActivitiesId == model.ActivitiesId && i.UserId == model.UserId);
                if (_Activity is not null)
                {
                    _context.ActivitiesUsers.Remove(_Activity);
                    await _context.SaveChangesAsync();
                    var _Result = ResultOutputData.GenearetResultOutputSuccess();
                    return _Result;
                }

                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            var _activity = await FindAsync(f => f.Id == id);
            if (_activity is not null)
            {

                var activityDto = _Assembler.WriteDto(_activity);
                var GroupUsersEntity = await _context.GroupUsersEntities.FirstOrDefaultAsync(f => f.EntityId == activityDto.EntityId && f.ItemId == id.ToString());
                activityDto.GroupId = GroupUsersEntity !=null ? GroupUsersEntity.GroupId : null;
                activityDto.Attchments = _context.Attachments.Where(i => i.EntityId == activityDto.EntityId && i.ItemId == activityDto.Id).Select(s => s.Url).ToList();
                var groups = await _iLDAPRepository.GetGroupUserEntitiesAsync((int)activityDto.EntityId, id.ToString());
                activityDto.customGroupIds = groups.CustomGroupIds;
                activityDto.ldapGroupIds = groups.LdapGroupIds;
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithOutJWT(activityDto, 1);
                return _result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
        }


        public async Task<OperationOutput> GetListUserByActivityId(int? activityId)
        {
            try
            {
                var _users = await _context.ActivitiesUsers.Where(i => i.ActivitiesId == activityId).Include(i => i.User).AsNoTrackingWithIdentityResolution().Select(s => s.User).ToListAsync();
                var _usersListDto = _Assembler_User.WriteListDto(_users);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_usersListDto, 1);
                return _result;

            }
            catch (Exception )
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> CreateActivitiesUsersAttendance(ActivitiesUsersAttendanceObjDto model)
        {
            try
            {
                var eventObj = await _context.ActivitiesUsersAttendances.AnyAsync(i => i.UserId == model.UserId && i.ActivitiesId == model.ActivitiesId);
                if (eventObj == false)
                {
                    var entity = new ActivitiesUsersAttendance
                    {
                        ActivitiesId = model.ActivitiesId,
                        UserId = model.UserId,
                        CreatedDate = DateTime.Now,
                        IsDeleted = false
                    };

                    var _entity = await _context.ActivitiesUsersAttendances.AddAsync(entity);
                    await _context.SaveChangesAsync();

                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutputWithOutJWT(_entity.Entity, 1);
                    return _result;
                }
                else
                {
                    var Result = ResultOutputData.GenearetResultOutputIsRegester();
                    return Result;

                }

            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllActivitiesUsersAttendancePagenation(PagenationBy _filter)
        {
            try
            {
                var spec = Specification<ActivitiesUsersAttendance>.All.And(new ActivityAttendanceSpecification(_filter));
                var ActivitiesUserAttendance = new List<ActivitiesUsersAttendance>();
                if (_filter.pageNumber is null)
                {
                    ActivitiesUserAttendance = await _context.ActivitiesUsersAttendances
                                                       .Where(i => i.IsDeleted == false)
                                                       .Include(i => i.Activity)
                                                       .Include(i => i.User)
                                                       .AsNoTrackingWithIdentityResolution()
                                                       .ToListAsync();
                }
                else
                {
                    ActivitiesUserAttendance = await _context.ActivitiesUsersAttendances
                                                       .Where(spec.ToExpression())
                                                       .Where(i => i.IsDeleted == false)
                                                       .Include(i => i.Activity)
                                                       .Include(i => i.User)
                                                       .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                                                       .Take((int)_filter.pageSize)
                                                       .AsNoTrackingWithIdentityResolution()
                                                       .ToListAsync();
                }



                var _ActivitiesUserAttendance = _Assembler_Attendance.WriteListDto(ActivitiesUserAttendance);
                var counts = await _context.ActivitiesUsersAttendances.CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_ActivitiesUserAttendance, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllUserActivitiesAttendance()
        {
            var Users = await _context.ActivitiesUsersAttendances
                     .Include(i => i.User).AsNoTrackingWithIdentityResolution()
                     .Select(s => new { s.User.Id, s.User.UserName }).Distinct().ToListAsync();

            ResultOutputData result = new ResultOutputData();
            var _result = result.GenearetResultOutputWithEmptyData(Users, Users.Count);
            return _result;

        }


    }
}
