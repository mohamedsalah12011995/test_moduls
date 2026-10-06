using Core.Helpers;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.EntityFrameworkCore;
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
using System.Security.Cryptography;

namespace Nupco.EF.Repositories
{
    public class EventRepository : BaseRepository<Event>, IEventRepository
    {
        private readonly Event_Assembler _Assembler;
        private readonly EventsUserAttendance_Assembler _Assembler_Attendance;
        private readonly Activity_Assembler _Assembler_Activity;
        private readonly User_Assembler _Assembler_User;
        private const int EntityEvent = 3;
        private const int EntityActivity = 6;

        private readonly IConfiguration _configuration;
        private readonly ILDAPRepository _iLDAPRepository;

        public EventRepository(ApplicationDbContext context, ILogger logger, ILDAPRepository iLDAPRepository) : base(context, logger)
        {
            _Assembler = new Event_Assembler();
            _Assembler_Attendance = new EventsUserAttendance_Assembler();
            _Assembler_Activity = new Activity_Assembler();
            _Assembler_User = new User_Assembler();
            _logger = logger;
            _context = context;
            _iLDAPRepository = iLDAPRepository;
        }


        public async Task<OperationOutput> AddNewAsync(EventDto entityDto)
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

                if (entityDto.IsQRCode == true)
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
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<EventDto> entitiesDto)
        {
            try
            {
                var _entities = _Assembler.WriteListDal(entitiesDto);
                var _entitiesDto = await AddRangeAsync(_entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entitiesDto, 1);
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
                var _entities = await FindAllAsync(i => i.IsDeleted == false);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
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
                var spec = Specification<Event>.All.And(new EventSpecification(_filter));

                var Events = await _context.Events.Where(spec.ToExpression())
                                           .Where(i => i.IsDeleted == false).OrderByDescending(o => o.Id)
                                           .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                                           .AsNoTrackingWithIdentityResolution().ToListAsync();

                var _Events = _Assembler.WriteListDto(Events);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Events, counts);
                return _result;
            }
            catch (Exception)
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



        public async Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            try
            {

                var Events = await FindAllAsync(f => f.CreatedBy == userId);
                var _Events = _Assembler.WriteListDto(Events);
                foreach (var ev in _Events)
                {
                    ev.Attchments = await _context.Attachments.Where(i => i.EntityId == ev.EntityId && i.ItemId == ev.Id).AsNoTrackingWithIdentityResolution().Select(s => s.Url).ToListAsync();
                }
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Events, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(EventDto entityDto)
        {
            try
            {
                if (entityDto.isPicChanged == true && !String.IsNullOrEmpty(entityDto.OriginalPicBase64))
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
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;

                _entity.TimeStart = _timeStart;
                _entity.TimeEnd = _timeEnd;
                if (_entity.Code is null)
                {
                    _entity.Code = await GenerateUniqueActivityCodeAsync();

                    if (entityDto.IsQRCode == true)
                        _entity.PicQRCode = Nupco.Core.Helpers.Strings.GenereteQRCode(_entity.Code);
                }



                var entity = Update(_entity);

                await _iLDAPRepository.CreateOrUpdateGroupUserEntities((int)entityDto.EntityId, entityDto.Id.ToString(), entityDto.customGroupIds, entityDto.ldapGroupIds);


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
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<bool> CreateOrUpdateGroupUserEntities(EventDto entityDto)
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
        public async Task<bool> CreateOrUpdateGroupUserEntity(EventDto entityDto)
        {

            if (entityDto.GroupId != null)
            {
                var groupUsersEntity = await _context.GroupUsersEntities.SingleOrDefaultAsync(i => i.EntityId == entityDto.EntityId && i.ItemId == entityDto.Id.ToString());
                if (groupUsersEntity != null)
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
                if(groupUsersEntity is not null)
                {
                    _context.GroupUsersEntities.Remove(groupUsersEntity);
                    await _context.SaveChangesAsync();
                }

            }
            return true;
        }

        public async Task<OperationOutput> UpdateEntityAsync(EventDto entityDto, object key)
        {
            try
            {
                var _event = await GetByIdAsync(entityDto.Id.Value);
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
                var entity = UpdateAsync(_entity, _event);
                await SaveChangesAsync();


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

                var notificationHistory = await _context.NotificationHistories.Where(f => f.RecordId == id.ToString() && f.EntityId == find.EntityId).ToListAsync();
                if (notificationHistory.Any())
                    _context.NotificationHistories.RemoveRange(notificationHistory);

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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<EventDto> entities)
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
            catch (Exception)
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
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllEventByUserJoind(string userId)
        {

            try
            {
                var _EventsUsers = await _context.EventsUsers.AsNoTrackingWithIdentityResolution().ToListAsync();

                var _entities = await _context.Events.Where(f => f.IsDeleted == false).Include(i => i.EventsUsers)
                    .AsNoTrackingWithIdentityResolution().Select(s => new EventDto
                    {
                        Id = s.Id,
                        TitleAr = s.TitleAr,
                        TitleEn = s.TitleEn,
                        BriefeContentAr = s.BriefeContentAr,
                        BriefeContentEn = s.BriefeContentEn,
                        OriginalPic = s.OriginalPic,
                        CreatedDate = s.CreatedDate,
                        IsJoin = s.EventsUsers != null && s.EventsUsers.FirstOrDefault().UserId == userId ? true : false

                    }).ToListAsync();

                var entities = _entities.OrderByDescending(s => s.IsJoin).DistinctBy(s => s.Id).Select(s => new EventResult
                {
                    Event = s,
                    Count = _EventsUsers.Count(c => c.EventsId == s.Id)

                });

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetJoindEventById(int eventId)
        {

            try
            {
                var entities = await _context.EventsUsers
                    .Where(i => i.EventsId == eventId)
                    .Include(i => i.User)
                    .Include(i => i.Events)
                    .ThenInclude(i => i.Entity)
                    .AsNoTrackingWithIdentityResolution()
                    .Select(s => new
                    {
                        Email = s.User.Email,
                        userName = s.User.UserName,
                        FullName = s.User.FirstName + " " + s.User.LastName,
                        entityAr = s.Events.Entity.NameAr,
                        entityEn = s.Events.Entity.NameEn,
                        EventAr = s.Events.TitleAr,
                        EventEn = s.Events.TitleEn,
                        BriefeContentAr = s.Events.BriefeContentAr,
                        BriefeContentEn = s.Events.BriefeContentEn

                    }).ToListAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        /// <summary>
        /// logic Event and Activites
        /// </summary>

        public async Task<OperationOutput> GetAllEventsAndActivitesByUserId(string userId)
        {

            try
            {
                var _EventsUsers = await _context.EventsUsers.ToListAsync();
                List<EventResult> Events = new List<EventResult>();

                var _eventsByUsers = await _context.Events.Include(c => c.EventsUsers)
                    .Where(f => f.EventsUsers.FirstOrDefault().UserId == userId).AsNoTrackingWithIdentityResolution().ToListAsync();
                var eventsByUsers = _eventsByUsers.Select(s => new EventResult
                {
                    Event = _Assembler.WriteDto(s),
                    Count = _EventsUsers.Count(c => c.EventsId == s.Id)
                }).Distinct().ToList();


                Events.AddRange(eventsByUsers);
                foreach (var item in Events)
                {
                    item.Event.Attchments = await _context.Attachments.Where(i => i.EntityId == item.Event.EntityId && i.ItemId == item.Event.Id).Select(s => s.Url).ToListAsync();
                    item.Event.IsJoin = true;
                }


                //////////////////////////
                List<ActivityResult> Activities = new List<ActivityResult>();
                var _ActivitiesUsers = await _context.ActivitiesUsers.ToListAsync();
                var _activitiesByUsers = await _context.Activities.Include(c => c.ActivityUsers).Where(f => f.ActivityUsers.FirstOrDefault().UserId == userId).ToListAsync();
                var activitiesByUsers = _activitiesByUsers.Select(s => new ActivityResult
                {
                    Activity = _Assembler_Activity.WriteDto(s),
                    Count = _EventsUsers.Count(c => c.EventsId == s.Id)
                }).Distinct().ToList();

                Activities.AddRange(activitiesByUsers);
                foreach (var item in Activities)
                {
                    item.Activity.Attchments = await _context.Attachments.Where(i => i.EntityId == item.Activity.EntityId && i.ItemId == item.Activity.Id).Select(s => s.Url).ToListAsync();
                    item.Activity.IsJoin = true;
                }


                List<object> list = new List<object>();
                list.Add(Events);
                list.Add(Activities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(list, list.Count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetActivitiesAndEventIncoming(PagenationBy _filter, CancellationToken cancellationToken)
        {
            try
            {
                var _IsPinCode = await _context.Settings.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(f => f.Key == "AllowPinCode");

                var _EventList = await _context.Events.Where(i => i.IsDeleted == false && i.IsActive == true && i.EventEndDate.Value.Date >= DateTime.Now.Date).OrderBy(o => o.EventStartDate).Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize).AsNoTrackingWithIdentityResolution().ToListAsync();
                var GroupsUser = await _context.GroupUsers.Where(i => i.UserId == _filter.UserId).Distinct().AsNoTrackingWithIdentityResolution().ToListAsync();
                var GroupsEntity = await _context.GroupUsersEntities.Where(i => i.EntityId == 3 || i.EntityId == 6).Distinct().AsNoTrackingWithIdentityResolution().ToListAsync();

                var eventIds = _EventList.Select(e => e.Id).ToList();
                // Get counts from EventsUsers per event.
                var eventsUsersCounts = await _context.EventsUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(c => eventIds.Contains((int)c.EventsId))
                    .GroupBy(c => c.EventsId)
                    .Select(g => new { EventId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.EventId, x => x.Count, cancellationToken);


                // Get attendance info: which events has this user attended.
                var eventsAttendance = await _context.EventsUsersAttendances
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => eventIds.Contains((int)i.EventsId) && i.UserId == _filter.UserId)
                    .Select(i => i.EventsId)
                    .ToListAsync(cancellationToken);

                // Get join info: which events has this user joined.
                var eventsJoin = await _context.EventsUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => eventIds.Contains((int)i.EventsId) && i.UserId == _filter.UserId)
                    .Select(i => i.EventsId)
                    .ToListAsync(cancellationToken);

                // Get all attachments for events.
                var eventAttachments = await _context.Attachments
                    .AsNoTrackingWithIdentityResolution()
                    .Where(a => eventIds.Contains((int)a.ItemId))  // assuming ItemId corresponds to Event.Id
                    .Select(a => new { a.ItemId, a.Url, a.EntityId })
                    .ToListAsync(cancellationToken);

                // Group attachments by event ID.
                var eventAttachmentsGrouped = eventAttachments
                    .GroupBy(a => a.ItemId)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.Url).ToList());


                var Events = _EventList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    EventContentAr = s.EventContentAr,
                    EventContentEn = s.EventContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    EventStartDate = s.EventStartDate,
                    EventEndDate = s.EventEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsQRCode = s.IsQRCode,
                    IsPinCode = _IsPinCode != null ? Convert.ToBoolean(_IsPinCode.Value) : false,
                    Count = eventsUsersCounts.ContainsKey(s.Id) ? eventsUsersCounts[s.Id] : 0,
                    IsAttendance = eventsAttendance.Contains(s.Id),
                    IsJoin = eventsJoin.Contains(s.Id),
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityEvent),
                    Attchments = eventAttachmentsGrouped.ContainsKey(s.Id) ? eventAttachmentsGrouped[s.Id] : new List<string>()

                }).ToList();

                var _ActivityList = await _context.Activities.Where(i => i.IsDeleted == false && i.IsActive == true && i.ActivityEndDate.Value.Date >= DateTime.Now.Date).OrderByDescending(o => o.ActivityStartDate).Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize).AsNoTrackingWithIdentityResolution().ToListAsync();

                var activityIds = _ActivityList.Select(a => a.Id).ToList();

                var activitiesUsersCounts = await _context.ActivitiesUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(c => activityIds.Contains((int)c.ActivitiesId))
                    .GroupBy(c => c.ActivitiesId)
                    .Select(g => new { ActivitiesId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ActivitiesId, x => x.Count, cancellationToken);

                var activitiesAttendance = await _context.ActivitiesUsersAttendances
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => activityIds.Contains((int)i.ActivitiesId) && i.UserId == _filter.UserId)
                    .Select(i => i.ActivitiesId)
                    .ToListAsync(cancellationToken);

                var activitiesJoin= await _context.ActivitiesUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => activityIds.Contains((int)i.ActivitiesId) && i.UserId == _filter.UserId)
                    .Select(i => i.ActivitiesId)
                    .ToListAsync(cancellationToken);

                var activityAttachments = await _context.Attachments
                    .AsNoTrackingWithIdentityResolution()
                    .Where(a => activityIds.Contains((int)a.ItemId))
                    .Select(a => new { a.ItemId, a.Url, a.EntityId })
                    .ToListAsync(cancellationToken);

                var activityAttachmentsGrouped = activityAttachments
                    .GroupBy(a => a.ItemId)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.Url).ToList());


                var Activities = _ActivityList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    ActivityContentAr = s.ActivityContentAr,
                    ActivityContentEn = s.ActivityContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    ActivityStartDate = s.ActivityStartDate,
                    ActivityEndDate = s.ActivityEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsPinCode = _IsPinCode != null ? Convert.ToBoolean(_IsPinCode.Value) : false,
                    Count = activitiesUsersCounts.ContainsKey(s.Id) ? activitiesUsersCounts[s.Id] : 0,
                    IsAttendance = activitiesAttendance.Contains(s.Id),
                    IsJoin = activitiesJoin.Contains(s.Id),
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityActivity),
                    Attchments = activityAttachmentsGrouped.ContainsKey(s.Id) ? activityAttachmentsGrouped[s.Id] : new List<string>()

                }).ToList();


                var _res = new
                {
                    events = Events.Where(i=> i.IsJoinGroup==true).ToList(),
                    activities = Activities.Where(i => i.IsJoinGroup == true).ToList(),
                };

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithEmptyData(_res, _res.events.Count + _res.activities.Count);
                return _result;
            }
            catch (OperationCanceledException)
            {
                // Handle the case when the client cancels the request
                _logger.LogWarning("Client disconnected or request was canceled from GetActivitiesAndEventIncoming.");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetActivitiesAndEventIncoming : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetActivitiesAndEventIncomingWithInternalAndExternal(PagenationBy _filter, CancellationToken cancellationToken)
        {
            try
            {
                var _IsPinCode = await _context.Settings.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(f => f.Key == "AllowPinCode");

                var _EventList = await _context.Events.Where(i => i.IsDeleted == false && i.IsActive == true && i.EventEndDate.Value.Date >= DateTime.Now.Date).OrderBy(o => o.EventStartDate).Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize).AsNoTrackingWithIdentityResolution().ToListAsync();
                var GroupsUser = await _context.GroupUsers.Where(i => i.UserId == _filter.UserId).Distinct().AsNoTrackingWithIdentityResolution().ToListAsync();
                var GroupsEntity = await _context.GroupUsersEntities.Where(i => i.EntityId == 3 || i.EntityId == 6).Distinct().AsNoTrackingWithIdentityResolution().ToListAsync();

                var eventIds = _EventList.Select(e => e.Id).ToList();
                // Get counts from EventsUsers per event.
                var eventsUsersCounts = await _context.EventsUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(c => eventIds.Contains((int)c.EventsId))
                    .GroupBy(c => c.EventsId)
                    .Select(g => new { EventId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.EventId, x => x.Count, cancellationToken);


                // Get attendance info: which events has this user attended.
                var eventsAttendance = await _context.EventsUsersAttendances
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => eventIds.Contains((int)i.EventsId) && i.UserId == _filter.UserId)
                    .Select(i => i.EventsId)
                    .ToListAsync(cancellationToken);

                // Get join info: which events has this user joined.
                var eventsJoin = await _context.EventsUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => eventIds.Contains((int)i.EventsId) && i.UserId == _filter.UserId)
                    .Select(i => i.EventsId)
                    .ToListAsync(cancellationToken);

                // Get all attachments for events.
                var eventAttachments = await _context.Attachments
                    .AsNoTrackingWithIdentityResolution()
                    .Where(a => eventIds.Contains((int)a.ItemId))  // assuming ItemId corresponds to Event.Id
                    .Select(a => new { a.ItemId, a.Url, a.EntityId })
                    .ToListAsync(cancellationToken);

                // Group attachments by event ID.
                var eventAttachmentsGrouped = eventAttachments
                    .GroupBy(a => a.ItemId)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.Url).ToList());


                var Events = _EventList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    EventContentAr = s.EventContentAr,
                    EventContentEn = s.EventContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    EventStartDate = s.EventStartDate,
                    EventEndDate = s.EventEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsPinCode = _IsPinCode != null ? Convert.ToBoolean(_IsPinCode.Value) : false,
                    Count = eventsUsersCounts.ContainsKey(s.Id) ? eventsUsersCounts[s.Id] : 0,
                    IsAttendance = eventsAttendance.Contains(s.Id),
                    IsJoin = eventsJoin.Contains(s.Id),
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity, EntityEvent),
                    Attchments = eventAttachmentsGrouped.ContainsKey(s.Id) ? eventAttachmentsGrouped[s.Id] : new List<string>()

                }).ToList();

                var _ActivityList = await _context.Activities.Where(i => i.IsDeleted == false && i.IsActive == true && i.ActivityEndDate.Value.Date >= DateTime.Now.Date).OrderByDescending(o => o.ActivityStartDate).Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize).AsNoTrackingWithIdentityResolution().ToListAsync();

                var activityIds = _ActivityList.Select(a => a.Id).ToList();

                var activitiesUsersCounts = await _context.ActivitiesUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(c => activityIds.Contains((int)c.ActivitiesId))
                    .GroupBy(c => c.ActivitiesId)
                    .Select(g => new { ActivitiesId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ActivitiesId, x => x.Count, cancellationToken);

                var activitiesAttendance = await _context.ActivitiesUsersAttendances
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => activityIds.Contains((int)i.ActivitiesId) && i.UserId == _filter.UserId)
                    .Select(i => i.ActivitiesId)
                    .ToListAsync(cancellationToken);

                var activitiesJoin = await _context.ActivitiesUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => activityIds.Contains((int)i.ActivitiesId) && i.UserId == _filter.UserId)
                    .Select(i => i.ActivitiesId)
                    .ToListAsync(cancellationToken);

                var activityAttachments = await _context.Attachments
                    .AsNoTrackingWithIdentityResolution()
                    .Where(a => activityIds.Contains((int)a.ItemId))
                    .Select(a => new { a.ItemId, a.Url, a.EntityId })
                    .ToListAsync(cancellationToken);

                var activityAttachmentsGrouped = activityAttachments
                    .GroupBy(a => a.ItemId)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.Url).ToList());


                var Activities = _ActivityList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    ActivityContentAr = s.ActivityContentAr,
                    ActivityContentEn = s.ActivityContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    ActivityStartDate = s.ActivityStartDate,
                    ActivityEndDate = s.ActivityEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsPinCode = _IsPinCode != null ? Convert.ToBoolean(_IsPinCode.Value) : false,
                    Count = activitiesUsersCounts.ContainsKey(s.Id) ? activitiesUsersCounts[s.Id] : 0,
                    IsAttendance = activitiesAttendance.Contains(s.Id),
                    IsJoin = activitiesJoin.Contains(s.Id),
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity, EntityActivity),
                    Attchments = activityAttachmentsGrouped.ContainsKey(s.Id) ? activityAttachmentsGrouped[s.Id] : new List<string>()

                }).ToList();


                var _res = new
                {
                    events = new 
                    { 
                        All = Events.Where(i => i.IsJoinGroup == true).ToList(),
                        Internal = Events.Where(i => i.IsJoinGroup == true && i.IsInternal==true).ToList(),
                        External = Events.Where(i => i.IsJoinGroup == true && i.IsExternal==true).ToList(),
                    } ,
                    activities = new
                    {
                        All = Activities.Where(i => i.IsJoinGroup == true).ToList(),
                        Internal = Activities.Where(i => i.IsJoinGroup == true && i.IsInternal == true).ToList(),
                        External = Activities.Where(i => i.IsJoinGroup == true && i.IsExternal == true).ToList(),
                    },
                    totals = new
                    {
                        All = Events.Count(i => i.IsJoinGroup) + Activities.Count(i => i.IsJoinGroup),
                        Internal = Events.Count(i => i.IsJoinGroup==true && i.IsInternal == true) + Activities.Count(i => i.IsJoinGroup == true && i.IsInternal == true),
                        External = Events.Count(i => i.IsJoinGroup == true && i.IsExternal == true) + Activities.Count(i => i.IsJoinGroup == true && i.IsExternal == true),
                    }
                };
                int count = _res.totals.All + _res.totals.Internal + _res.totals.External;
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithEmptyData(_res, count);
                return _result;
            }
            catch (OperationCanceledException)
            {
                // Handle the case when the client cancels the request
                _logger.LogWarning("Client disconnected or request was canceled from GetActivitiesAndEventIncoming.");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetActivitiesAndEventIncoming : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetActivitiesAndEventPreviewsWithInternalAndExternal(PagenationBy _filter, CancellationToken cancellationToken)
        {
            try
            {
                var _IsPinCode = await _context.Settings.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(f => f.Key == "AllowPinCode");

                var _EventList = await _context.Events.AsNoTrackingWithIdentityResolution().Where(i => i.IsDeleted == false && i.IsActive == true && i.EventEndDate.Value.Date < DateTime.Now.Date).OrderByDescending(o => o.EventStartDate).Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize).ToListAsync();

                var GroupsUser = await _context.GroupUsers.AsNoTrackingWithIdentityResolution().Where(i => i.UserId == _filter.UserId).Distinct().ToListAsync();
                var GroupsEntity = await _context.GroupUsersEntities.AsNoTrackingWithIdentityResolution().Where(i => i.EntityId == 3 || i.EntityId == 6).Distinct().ToListAsync();

                var eventIds = _EventList.Select(e => e.Id).ToList();
                // Get counts from EventsUsers per event.
                var eventsUsersCounts = await _context.EventsUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(c => eventIds.Contains((int)c.EventsId))
                    .GroupBy(c => c.EventsId)
                    .Select(g => new { EventId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.EventId, x => x.Count, cancellationToken);



                var Events = _EventList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    EventContentAr = s.EventContentAr,
                    EventContentEn = s.EventContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    EventStartDate = s.EventStartDate,
                    EventEndDate = s.EventEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsPinCode = _IsPinCode != null ? Convert.ToBoolean(_IsPinCode.Value) : false,
                    Count = _context.EventsUsers.AsNoTrackingWithIdentityResolution().Count(c => c.EventsId == s.Id),
                    IsAttendance = _context.EventsUsersAttendances.AsNoTrackingWithIdentityResolution().Count(i => i.UserId == _filter.UserId && i.EventsId == s.Id) > 0 ? true : false,
                    IsJoin = _context.EventsUsers.AsNoTrackingWithIdentityResolution().Count(i => i.UserId == _filter.UserId && i.EventsId == s.Id) > 0 ? true : false,
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityEvent),
                    Attchments = _context.Attachments.AsNoTrackingWithIdentityResolution().Where(i => i.EntityId == s.EntityId && i.ItemId == s.Id).Select(s => s.Url).ToList()

                }).ToList();

                var _ActivityList = await _context.Activities.Where(i => i.IsDeleted == false && i.IsActive == true && i.ActivityEndDate.Value.Date < DateTime.Now.Date).OrderByDescending(o => o.ActivityStartDate).Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize).AsNoTrackingWithIdentityResolution().ToListAsync();
                var Activities = _ActivityList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    ActivityContentAr = s.ActivityContentAr,
                    ActivityContentEn = s.ActivityContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    ActivityStartDate = s.ActivityStartDate,
                    ActivityEndDate = s.ActivityEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsPinCode = _IsPinCode != null ? Convert.ToBoolean(_IsPinCode.Value) : false,
                    Count = _context.ActivitiesUsers.AsNoTrackingWithIdentityResolution().Count(c => c.ActivitiesId == s.Id),
                    IsAttendance = _context.ActivitiesUsersAttendances.AsNoTrackingWithIdentityResolution().Count(i => i.UserId == _filter.UserId && i.ActivitiesId == s.Id) > 0 ? true : false,
                    IsJoin = _context.ActivitiesUsers.AsNoTrackingWithIdentityResolution().Count(i => i.UserId == _filter.UserId && i.ActivitiesId == s.Id) > 0 ? true : false,
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityActivity),
                    Attchments = _context.Attachments.AsNoTrackingWithIdentityResolution().Where(i => i.EntityId == s.EntityId && i.ItemId == s.Id).Select(s => s.Url).ToList()

                }).ToList();



                var _res = new
                {
                    events = new
                    {
                        All = Events.Where(i => i.IsJoinGroup == true).ToList(),
                        Internal = Events.Where(i => i.IsJoinGroup == true && i.IsInternal == true).ToList(),
                        External = Events.Where(i => i.IsJoinGroup == true && i.IsExternal == true).ToList(),
                    },
                    activities = new
                    {
                        All = Activities.Where(i => i.IsJoinGroup == true).ToList(),
                        Internal = Activities.Where(i => i.IsJoinGroup == true && i.IsInternal == true).ToList(),
                        External = Activities.Where(i => i.IsJoinGroup == true && i.IsExternal == true).ToList(),
                    },
                    totals = new
                    {
                        All = Events.Count(i => i.IsJoinGroup) + Activities.Count(i => i.IsJoinGroup),
                        Internal = Events.Count(i => i.IsJoinGroup == true && i.IsInternal == true) + Activities.Count(i => i.IsJoinGroup == true && i.IsInternal == true),
                        External = Events.Count(i => i.IsJoinGroup == true && i.IsExternal == true) + Activities.Count(i => i.IsJoinGroup == true && i.IsExternal == true),
                    }
                };
                int count = _res.totals.All + _res.totals.Internal + _res.totals.External;
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithEmptyData(_res, count);
                return _result;
            }
            catch (OperationCanceledException)
            {
                // Handle the case when the client cancels the request
                _logger.LogWarning("Client disconnected or request was canceled from GetActivitiesAndEventPreviews.");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetActivitiesAndEventPreviews : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }
        public async Task<OperationOutput> GetActivitiesAndEventPreviews(PagenationBy _filter, CancellationToken cancellationToken)
        {
            try
            {
                var _IsPinCode = await _context.Settings.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(f => f.Key == "AllowPinCode");

                var _EventList = await _context.Events.AsNoTrackingWithIdentityResolution().Where(i => i.IsDeleted == false && i.IsActive == true && i.EventEndDate.Value.Date < DateTime.Now.Date).OrderByDescending(o => o.EventStartDate).Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize).ToListAsync();

                var GroupsUser = await _context.GroupUsers.AsNoTrackingWithIdentityResolution().Where(i => i.UserId == _filter.UserId).Distinct().ToListAsync();
                var GroupsEntity = await _context.GroupUsersEntities.AsNoTrackingWithIdentityResolution().Where(i => i.EntityId == 3 || i.EntityId == 6).Distinct().ToListAsync();

                var eventIds = _EventList.Select(e => e.Id).ToList();
                // Get counts from EventsUsers per event.
                var eventsUsersCounts = await _context.EventsUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(c => eventIds.Contains((int)c.EventsId))
                    .GroupBy(c => c.EventsId)
                    .Select(g => new { EventId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.EventId, x => x.Count, cancellationToken);



                var Events = _EventList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    EventContentAr = s.EventContentAr,
                    EventContentEn = s.EventContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    EventStartDate = s.EventStartDate,
                    EventEndDate = s.EventEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsQRCode = s.IsQRCode,
                    IsPinCode = _IsPinCode != null ? Convert.ToBoolean(_IsPinCode.Value) : false,
                    Count = _context.EventsUsers.AsNoTrackingWithIdentityResolution().Count(c => c.EventsId == s.Id),
                    IsAttendance = _context.EventsUsersAttendances.AsNoTrackingWithIdentityResolution().Count(i => i.UserId == _filter.UserId && i.EventsId == s.Id) > 0 ? true : false,
                    IsJoin = _context.EventsUsers.AsNoTrackingWithIdentityResolution().Count(i => i.UserId == _filter.UserId && i.EventsId == s.Id) > 0 ? true : false,
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityEvent),
                    Attchments = _context.Attachments.AsNoTrackingWithIdentityResolution().Where(i => i.EntityId == s.EntityId && i.ItemId == s.Id).Select(s => s.Url).ToList()

                }).ToList();

                var _ActivityList = await _context.Activities.Where(i => i.IsDeleted == false && i.IsActive == true && i.ActivityEndDate.Value.Date < DateTime.Now.Date).OrderByDescending(o => o.ActivityStartDate).Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize).AsNoTrackingWithIdentityResolution().ToListAsync();
                var Activities = _ActivityList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    ActivityContentAr = s.ActivityContentAr,
                    ActivityContentEn = s.ActivityContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    ActivityStartDate = s.ActivityStartDate,
                    ActivityEndDate = s.ActivityEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsPinCode = _IsPinCode != null ? Convert.ToBoolean(_IsPinCode.Value) : false,
                    Count = _context.ActivitiesUsers.AsNoTrackingWithIdentityResolution().Count(c => c.ActivitiesId == s.Id),
                    IsAttendance = _context.ActivitiesUsersAttendances.AsNoTrackingWithIdentityResolution().Count(i => i.UserId == _filter.UserId && i.ActivitiesId == s.Id) > 0 ? true : false,
                    IsJoin = _context.ActivitiesUsers.AsNoTrackingWithIdentityResolution().Count(i => i.UserId == _filter.UserId && i.ActivitiesId == s.Id) > 0 ? true : false,
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityActivity),
                    Attchments = _context.Attachments.AsNoTrackingWithIdentityResolution().Where(i => i.EntityId == s.EntityId && i.ItemId == s.Id).Select(s => s.Url).ToList()

                }).ToList();



                var _res = new
                {
                    events = Events.Where(i => i.IsJoinGroup == true).ToList(),
                    activities = Activities.Where(i => i.IsJoinGroup == true).ToList(),
                };

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithEmptyData(_res, _res.events.Count + _res.activities.Count);
                return _result;
            }
            catch (OperationCanceledException)
            {
                // Handle the case when the client cancels the request
                _logger.LogWarning("Client disconnected or request was canceled from GetActivitiesAndEventPreviews.");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetActivitiesAndEventPreviews : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        private bool CheckIsGroup(string UserId, int id, List<GroupUsers> groupUser, List<GroupUsersEntities> groupUsersEntities,int entityId)
        {
            bool isJoinGroup = false;
            int? groupId = groupUsersEntities.FirstOrDefault(f => f.EntityId == entityId && f.ItemId == id.ToString())?.GroupId;

            if (groupId == null)
                isJoinGroup = true;
            else
                isJoinGroup = groupUser.Any(a => a.UserId == UserId && a.GroupId == groupId);

            return isJoinGroup;
        }



        public async Task<OperationOutput> GetActivitiesAndEventByCalender(CalendarFilter _filter)
        {
            try
            {
                var _EventList = new List<Event>();
                var _ActivityList = new List<Activity>();

                //var fromTime = Dates.ConvertToTimeOnly(_filter.FromTime);
                //var toTime = Dates.ConvertToTimeOnly(_filter.ToTime);


                _EventList = await _context.Events.AsNoTrackingWithIdentityResolution().Where(i => i.IsDeleted == false && i.IsActive == true && i.EventStartDate.Value.Date.Month == _filter.month && i.EventStartDate.Value.Year == _filter.year).OrderBy(o => o.EventStartDate).ToListAsync();
                var GroupsUser = await _context.GroupUsers.AsNoTrackingWithIdentityResolution().Where(i => i.UserId == _filter.UserId).Distinct().ToListAsync();
                var GroupsEntity = await _context.GroupUsersEntities.AsNoTrackingWithIdentityResolution().Where(i => i.EntityId == 3 || i.EntityId == 6).Distinct().ToListAsync();


                // 2. Batch queries for Events-related data.
                var eventIds = _EventList.Select(e => e.Id).ToList();

                // Group count for EventsUsers.
                var eventsUsersCounts = await _context.EventsUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(c => eventIds.Contains((int)c.EventsId))
                    .GroupBy(c => c.EventsId)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Count);

                // Get events where the user attended.
                var eventsAttendance = await  _context.EventsUsersAttendances
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => eventIds.Contains((int)i.EventsId) && i.UserId == _filter.UserId)
                    .Select(i => i.EventsId)
                    .ToListAsync();

                // Get events where the user joined.
                var eventsJoin = await _context.EventsUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => eventIds.Contains((int)i.EventsId) && i.UserId == _filter.UserId)
                    .Select(i => i.EventsId)
                    .ToListAsync();

                // Get attachments for events.
                var eventAttachments = await _context.Attachments
                    .AsNoTrackingWithIdentityResolution()
                    .Where(a => eventIds.Contains((int)a.ItemId))
                    .Select(a => new { a.ItemId, a.Url, a.EntityId })
                    .ToListAsync();

                var eventAttachmentsGrouped = eventAttachments
                .GroupBy(a => a.ItemId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Url).ToList());

                var Events = _EventList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    EventContentAr = s.EventContentAr,
                    EventContentEn = s.EventContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    EventStartDate = s.EventStartDate,
                    EventEndDate = s.EventEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    Count = eventsUsersCounts.ContainsKey(s.Id) ? eventsUsersCounts[s.Id] : 0,
                    IsAttendance = eventsAttendance.Contains(s.Id),
                    IsJoin = eventsJoin.Contains(s.Id),
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityEvent),
                    Attchments = eventAttachmentsGrouped.ContainsKey(s.Id) ? eventAttachmentsGrouped[s.Id] : new List<string>()
                }).ToList();

                _ActivityList = await _context.Activities.AsNoTrackingWithIdentityResolution().Where(i => i.IsDeleted == false && i.IsActive == true && i.ActivityStartDate.Value.Date.Month == _filter.month && i.ActivityStartDate.Value.Date.Year == _filter.year).OrderBy(o => o.ActivityStartDate).AsNoTrackingWithIdentityResolution().ToListAsync();
                
                var activityIds = _ActivityList.Select(a => a.Id).ToList();
                var activitiesUsersCounts = await _context.ActivitiesUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(c => activityIds.Contains((int)c.ActivitiesId))
                    .GroupBy(c => c.ActivitiesId)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Count);
                var activitiesAttendance = await _context.ActivitiesUsersAttendances
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => activityIds.Contains((int)i.ActivitiesId) && i.UserId == _filter.UserId)
                    .Select(i => i.ActivitiesId)
                    .ToListAsync();
                var activitiesJoin = await _context.ActivitiesUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => activityIds.Contains((int)i.ActivitiesId) && i.UserId == _filter.UserId)
                    .Select(i => i.ActivitiesId)
                    .ToListAsync();
                var activityAttachments = await _context.Attachments
                    .AsNoTrackingWithIdentityResolution()
                    .Where(a => activityIds.Contains((int)a.ItemId))
                    .Select(a => new { a.ItemId, a.Url, a.EntityId })
                    .ToListAsync();

                var activityAttachmentsGrouped = activityAttachments
                    .GroupBy(a => a.ItemId)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.Url).ToList());


                var Activities = _ActivityList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    ActivityContentAr = s.ActivityContentAr,
                    ActivityContentEn = s.ActivityContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    ActivityStartDate = s.ActivityStartDate,
                    ActivityEndDate = s.ActivityEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    Count = activitiesUsersCounts.ContainsKey(s.Id) ? activitiesUsersCounts[s.Id] : 0,
                    IsAttendance = activitiesAttendance.Contains(s.Id),
                    IsJoin = activitiesJoin.Contains(s.Id),
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity, EntityActivity),
                    Attchments = activityAttachmentsGrouped.ContainsKey(s.Id) ? activityAttachmentsGrouped[s.Id] : new List<string>()

                }).ToList();



                var _res = new
                {
                    events = Events.Where(i => i.IsJoinGroup == true).ToList(),
                    activities = Activities.Where(i => i.IsJoinGroup == true).ToList()
                };

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithEmptyData(_res, _res.events.Count + _res.activities.Count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<OperationOutput> GetActivitiesAndEventByCalenderDay(CalendarFilter _filter)
        {
            try
            {
                var _EventList = new List<Event>();
                var _ActivityList = new List<Activity>();

                _EventList = await _context.Events.Where(i => i.IsDeleted == false && i.IsActive == true && i.EventStartDate.Value.Date.Day == _filter.day && i.EventStartDate.Value.Date.Month == _filter.month && i.EventStartDate.Value.Year == _filter.year).OrderBy(o => o.EventStartDate).AsNoTrackingWithIdentityResolution().ToListAsync();

                var GroupsUser = await _context.GroupUsers.Where(i => i.UserId == _filter.UserId).Distinct().AsNoTrackingWithIdentityResolution().ToListAsync();
                var GroupsEntity = await _context.GroupUsersEntities.Where(i => i.EntityId == 3 || i.EntityId == 6).Distinct().AsNoTrackingWithIdentityResolution().ToListAsync();


                var Events = _EventList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    EventContentAr = s.EventContentAr,
                    EventContentEn = s.EventContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    EventStartDate = s.EventStartDate,
                    EventEndDate = s.EventEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsQRCode = s.IsQRCode,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    Count = _context.EventsUsers.Count(c => c.EventsId == s.Id),
                    IsAttendance = _context.EventsUsersAttendances.Count(i => i.UserId == _filter.UserId && i.EventsId == s.Id) > 0 ? true : false,
                    IsJoin = _context.EventsUsers.Count(i => i.UserId == _filter.UserId && i.EventsId == s.Id) > 0 ? true : false,
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityEvent),
                    Attchments = _context.Attachments.Where(i => i.EntityId == s.EntityId && i.ItemId == s.Id).Select(s => s.Url).ToList()

                }).ToList();

                _ActivityList = await _context.Activities.Where(i => i.IsDeleted == false && i.IsActive == true && i.ActivityStartDate.Value.Date.Day == _filter.day && i.ActivityStartDate.Value.Date.Month == _filter.month && i.ActivityStartDate.Value.Date.Year == _filter.year).OrderBy(o => o.ActivityStartDate).AsNoTrackingWithIdentityResolution().ToListAsync();
                var Activities = _ActivityList.Select(s => new
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    ActivityContentAr = s.ActivityContentAr,
                    ActivityContentEn = s.ActivityContentEn,
                    Location = s.Location,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    ActivityStartDate = s.ActivityStartDate,
                    ActivityEndDate = s.ActivityEndDate,
                    TimeStart = s.TimeStart,
                    TimeEnd = s.TimeEnd,
                    statusStringAr = s.IsActive == true ? "فعال" : "غير فعال",
                    statusStringEn = s.IsActive == true ? "Active" : "UnActiveate",
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    OriginalPic = s.OriginalPic,
                    Code = s.Code,
                    isShowHome = s.IsShowHome,
                    isJoinEvent = s.IsJoinEvent,
                    IsInternal = s.IsInternal,
                    IsExternal = s.IsExternal,
                    IsQRCode = s.IsQRCode,
                    Count = _context.ActivitiesUsers.Count(c => c.ActivitiesId == s.Id),
                    IsAttendance = _context.ActivitiesUsersAttendances.Count(i => i.UserId == _filter.UserId && i.ActivitiesId == s.Id) > 0 ? true : false,
                    IsJoin = _context.ActivitiesUsers.Count(i => i.UserId == _filter.UserId && i.ActivitiesId == s.Id) > 0 ? true : false,
                    IsJoinGroup = CheckIsGroup(_filter.UserId, s.Id, GroupsUser, GroupsEntity,EntityActivity),
                    Attchments = _context.Attachments.Where(i => i.EntityId == s.EntityId && i.ItemId == s.Id).Select(s => s.Url).ToList()

                }).ToList();



                var _res = new
                {
                    events = Events.Where(i => i.IsJoinGroup == true).ToList(),
                    activities = Activities.Where(i => i.IsJoinGroup == true).ToList()
                };

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithEmptyData(_res, _res.events.Count + _res.activities.Count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }




        public async Task<OperationOutput> JoinEventByUser(EventsUserDto model)
        {
            try
            {
                var _Activity = new EventsUser()
                {
                    EventsId = model.EventsId,
                    UserId = model.UserId,
                    IsDeleted = false,
                };

                await _context.EventsUsers.AddAsync(_Activity);
                await _context.SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> LeaveEventByUser(EventsUserDto model)
        {
            try
            {


                var _event = await _context.EventsUsers.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(i => i.EventsId == model.EventsId && i.UserId == model.UserId);
                if (_event is not null)
                {
                    _context.EventsUsers.Remove(_event);
                    await _context.SaveChangesAsync();
                    var _Result = ResultOutputData.GenearetResultOutputSuccess();
                    return _Result;
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

        public async Task<OperationOutput> GetListUserByEventId(int? eventId)
        {
            try
            {
                var _users = await _context.EventsUsers.Where(i => i.EventsId == eventId).Include(i => i.User).AsNoTrackingWithIdentityResolution().Select(s => s.User).ToListAsync();
                var _usersListDto = _Assembler_User.WriteListDto(_users);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_usersListDto, 1);
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }




        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            var _event = await FindAsync(f => f.Id == id);
            if (_event is not null)
            {
                var eventDto = _Assembler.WriteDto(_event);
                var GroupUsersEntity = await _context.GroupUsersEntities.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(f => f.EntityId == eventDto.EntityId && f.ItemId == id.ToString());
                eventDto.GroupId = GroupUsersEntity != null ? GroupUsersEntity.GroupId : null;
                eventDto.Attchments = _context.Attachments.Where(i => i.EntityId == _event.EntityId && i.ItemId == _event.Id).Select(s => s.Url).ToList();
                var groups = await _iLDAPRepository.GetGroupUserEntitiesAsync((int)eventDto.EntityId, id.ToString());
                eventDto.customGroupIds = groups.CustomGroupIds;
                eventDto.ldapGroupIds = groups.LdapGroupIds;
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithOutJWT(eventDto, 1);
                return _result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
        }

        public async Task<OperationOutput> CreateEventsUsersAttendance(EventsUserAttendanceObjDto model)
        {
            try
            {
                var eventObj = await _context.EventsUsersAttendances.AnyAsync(i => i.UserId == model.UserId && i.EventsId == model.EventsId);
                if (eventObj == false)
                {
                    var entity = new EventsUserAttendance
                    {
                        EventsId = model.EventsId,
                        UserId = model.UserId,
                        CreatedDate = DateTime.Now,
                        IsDeleted = false
                    };

                    var _entity = await _context.EventsUsersAttendances.AddAsync(entity);
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

        public async Task<OperationOutput> GetAllEventsUsersAttendancePagenation(PagenationBy _filter)
        {
            try
            {
                var spec = Specification<EventsUserAttendance>.All.And(new EventAttendanceSpecification(_filter));
                var EventsUserAttendance = new List<EventsUserAttendance>();
                if (_filter.pageNumber is null)
                {
                    EventsUserAttendance = await _context.EventsUsersAttendances
                                                       .Where(i => i.IsDeleted == false)
                                                       .Include(i => i.Events)
                                                       .Include(i => i.User)
                                                       .AsNoTrackingWithIdentityResolution()
                                                       .ToListAsync();
                }
                else
                {
                    EventsUserAttendance = await _context.EventsUsersAttendances
                                                       .Where(spec.ToExpression())
                                                       .Where(i => i.IsDeleted == false)
                                                       .Include(i => i.Events)
                                                       .Include(i => i.User)
                                                       .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                                                       .AsNoTrackingWithIdentityResolution()
                                                       .Take((int)_filter.pageSize).ToListAsync();
                }



                var _EventsUserAttendance = _Assembler_Attendance.WriteListDto(EventsUserAttendance);
                var counts = await _context.EventsUsersAttendances.CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_EventsUserAttendance, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllUserEventAttendance()
        {
            var Users = await _context.EventsUsersAttendances
                     .Include(i => i.User)
                     .AsNoTrackingWithIdentityResolution()
                     .Select(s => new { s.User.Id, s.User.UserName }).Distinct().ToListAsync();

            ResultOutputData result = new ResultOutputData();
            var _result = result.GenearetResultOutputWithEmptyData(Users, Users.Count);
            return _result;

        }

    }
}
