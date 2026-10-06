using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models.Users;
using System.Data;
using System.Linq.Dynamic.Core;
using static Core.Helpers.Enums;

namespace Nupco.EF.Repositories
{
    internal class UserViewsRepository : BaseRepository<UserView>, IUserViewRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly IGenericEntityResolver _genericEntityResolver;
        private readonly UserView_Assembler _UserView_Assembler;

        public UserViewsRepository(ApplicationDbContext context, ILogger logger, IGenericEntityResolver genericEntityResolver) : base(context, logger)
        {
            _UserView_Assembler = new UserView_Assembler();
            _logger = logger;
            _context = context;
            _genericEntityResolver = genericEntityResolver;
        }

        public async Task<OperationOutput> ActivatedOrUnActivated(int id, bool activate)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                //find.IsActive = activate;
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

        public async Task<OperationOutput> AddNewAsync(UserViewDto entity)
        {
            try
            {
                //entity.IsActive = true;
                //entity.IsDelete = false;
                //entity.CreatedDate = DateTime.Now;
                var _model = _UserView_Assembler.WriteDal(entity);
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
                //find.IsDelete = true;
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
                string[] stringArray = { "Entity", "User" };
                var spec = Specification<UserView>.All.And(new UserViewSpecification(_filter));
                var UserViews = new List<UserView>();
                if (_filter.entityId > 0)
                {

                    var _UserView = await FindAllAsync(spec.ToExpression(), stringArray);
                    UserViews = _UserView.ToList();
                }
                else
                {

                    var _UserViews = await FindAllAsync(i => i.EntityId != null, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize, stringArray);
                    UserViews = _UserViews.ToList();
                }
                var _UserViewsDto = _UserView_Assembler.WriteListDto(UserViews);
                //var counts = Count(i => i.IsDelete == false);
                var counts = Count();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_UserViewsDto, counts);
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
                //var entities = await FindAllAsync(i => i.IsDelete == false);
                var entities = await GetAllAsync();
                var _entities = _UserView_Assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                //var count = await CountAsync(i => i.IsDelete == false);
                var count = await CountAsync();
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
                var UserView = await GetByIdAsync(id);
                if (UserView != null)
                {
                    var _UserView = _UserView_Assembler.WriteDto(UserView);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_UserView, 1);
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

        public async Task<OperationOutput> UpdateEntity(UserViewDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.EntityId = entity.EntityId;
                    _entity.UserId = entity.UserId;

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

        public async Task<OperationOutput> GetEntitiesWithViews(int entityTypeId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (entityTypeId <= 0)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var entityList = await _genericEntityResolver.GetEntitiesWithEntityTable(entityTypeId, pageNumber, pageSize);
                var totalCount = await _genericEntityResolver.GetEntityTotalCount(entityTypeId);

                if (entityList == null || !entityList.Any())
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var itemIds = entityList
                    .Select(e => (e.Id as object)?.ToString())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList();

                if (!itemIds.Any())
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var views = await _context.UserViews
                    .Where(v => v.EntityId == entityTypeId && itemIds.Contains(v.ItemId))
                    .Include(v => v.User)
                    .ToListAsync();

                var allViewsPerItemWeb = views.Where(i=> i.Platform=="web")
                    .GroupBy(v => v.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => new ViewUserModel
                        {
                            UserId = v.UserId,
                            UserName = v.User?.UserName,
                            Email = v.User?.Email
                        }).ToList()
                    );
                var allViewsPerItemMobile = views.Where(i => i.Platform == "mobile")
    .GroupBy(v => v.ItemId)
    .ToDictionary(
        g => g.Key,
        g => g.Select(v => new ViewUserModel
        {
            UserId = v.UserId,
            UserName = v.User?.UserName,
            Email = v.User?.Email
        }).ToList()
    );

                var uniqueViewsPerItemWeb = views.Where(i => i.Platform == "web")
                    .GroupBy(v => new { v.ItemId, v.UserId })
                    .GroupBy(g => g.Key.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => new ViewUserModel
                        {
                            UserId = x.Key.UserId,
                            UserName = x.First().User?.UserName,
                            Email = x.First().User?.Email
                        }).ToList()
                    );
                var uniqueViewsPerItemMobile = views.Where(i => i.Platform == "mobile")
                    .GroupBy(v => new { v.ItemId, v.UserId })
                    .GroupBy(g => g.Key.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => new ViewUserModel
                        {
                            UserId = x.Key.UserId,
                            UserName = x.First().User?.UserName,
                            Email = x.First().User?.Email
                        }).ToList()
                    );

                var resultList = entityList.Select(entity =>
                {
                    string itemId = entity.Id.ToString();
                    string titleAr = TryGetProperty(entity, "TitleAr")?.ToString() ?? "";
                    string titleEn = TryGetProperty(entity, "TitleEn")?.ToString() ?? "";
                    var entityObject = TryGetProperty(entity, "Entity");
                    string entityNameAr = TryGetProperty(entityObject, "NameAr")?.ToString() ?? "";
                    string entityNameEn = TryGetProperty(entityObject, "NameEn")?.ToString() ?? "";

                    var allViewersWeb = allViewsPerItemWeb.TryGetValue(itemId, out var allWeb) ? allWeb : new List<ViewUserModel>();
                    var allViewersMobile = allViewsPerItemMobile.TryGetValue(itemId, out var allMobile) ? allMobile : new List<ViewUserModel>();
                   
                    var uniqueViewersWeb = uniqueViewsPerItemWeb.TryGetValue(itemId, out var uniqWeb) ? uniqWeb : new List<ViewUserModel>();
                    var uniqueViewersMobile = uniqueViewsPerItemMobile.TryGetValue(itemId, out var uniqMobile) ? uniqMobile : new List<ViewUserModel>();

                    return new EntityViewModel
                    {
                        Id = itemId,
                        TitleAr = titleAr,
                        TitleEn = titleEn,
                        EntityNameAr = entityNameAr,
                        EntityNameEn = entityNameEn,
                        ViewsCountWeb = allViewersWeb.Count,
                        ViewsCountMobile = allViewersMobile.Count,

                        UniqueViewsCountWeb = uniqueViewersWeb.Count,
                        UniqueViewsCountMobile = uniqueViewersMobile.Count,
                        AllViewers = allViewersWeb.Concat(allViewersMobile).ToList(),
                        UniqueViewers = uniqueViewersWeb.Concat(uniqueViewersMobile).ToList(),
                    };
                })
                .OrderByDescending(e => e.Id)
                .ToList();

                return new ResultOutputData().GenearetResultOutput(resultList, totalCount);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        // 🧠 Helper method to get property value dynamically
        private object? TryGetProperty(object? obj, string propertyName)
        {
            if (obj == null)
                return null;

            var propertyInfo = obj.GetType().GetProperty(propertyName);
            return propertyInfo != null ? propertyInfo.GetValue(obj) : null;
        }






        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(UserViewDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<UserViewDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<UserViewDto> entities)
        {
            throw new NotImplementedException();
        }


        public async Task<OperationOutput> GetNewsStatistic(int entityId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var _items = new
                {
                    totalNews = await _context.Posts.CountAsync(i =>
                        i.EntityId == (int)Enums.EntityType.News &&
                        i.IsDeleted == false &&
                        i.IsActive == true &&
                        i.CreatedDate >= fromDate &&
                        i.CreatedDate <= toDate
                    ),

                    totalNewsComment = await _context.Comments.CountAsync(i =>
                        i.EntityId == (int)Enums.EntityType.News &&
                        i.CreatedDate >= fromDate &&
                        i.CreatedDate <= toDate
                    ),

                    totalNewsInterActionsComment = await _context.InterActions.CountAsync(i =>
                        i.EntityId == (int)Enums.EntityType.News &&
                        i.CreatedDate >= fromDate &&
                        i.CreatedDate <= toDate
                    )
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GetSpaceStatistic(int entityId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var _items = new
                {
                    totalNews = await _context.Posts.CountAsync(i =>
                        i.EntityId == (int)Enums.EntityType.Spaces &&
                        i.IsDeleted == false &&
                        i.IsActive == true &&
                        i.CreatedDate >= fromDate &&
                        i.CreatedDate <= toDate
                    ),

                    totalNewsComment = await _context.Comments.CountAsync(i =>
                        i.EntityId == (int)Enums.EntityType.Spaces &&
                        i.CreatedDate >= fromDate &&
                        i.CreatedDate <= toDate
                    ),

                    totalNewsInterActionsComment = await _context.InterActions.CountAsync(i =>
                        i.EntityId == (int)Enums.EntityType.Spaces &&
                        i.CreatedDate >= fromDate &&
                        i.CreatedDate <= toDate
                    )
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GetVideoStatistic(int entityId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var _items = new
                {
                    totalVideo = await _context.Videos.CountAsync(v =>
                        v.EntityId == (int)Enums.EntityType.Videos &&
                        v.CreatedDate >= fromDate &&
                        v.CreatedDate <= toDate
                    ),

                    totalVideoComment = await _context.Comments.CountAsync(c =>
                        c.EntityId == (int)Enums.EntityType.Videos &&
                        c.CreatedDate >= fromDate &&
                        c.CreatedDate <= toDate
                    ),

                    totalVideoInterActionsComment = await _context.InterActions.CountAsync(i =>
                        i.EntityId == (int)Enums.EntityType.Videos &&
                        i.CreatedDate >= fromDate &&
                        i.CreatedDate <= toDate
                    )
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }



        public async Task<OperationOutput> GetContentStatistics(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var result = new List<object>();

                var supportedTypes = new[]
                {
            EntityType.News,
            EntityType.Spaces,
            EntityType.AdvCompany,
            EntityType.Events,
            EntityType.Activity,
            EntityType.Channels,
            EntityType.Videos
        };

                var entityNamesAr = new Dictionary<EntityType, string>
        {
            { EntityType.News, "الأخبار" },
            { EntityType.Spaces, "المساحات" },
            { EntityType.AdvCompany, "الاعلانات" },
            { EntityType.Events, "الفعاليات" },
            { EntityType.Activity, "الأنشطة" },
            { EntityType.Channels, "القنوات" },
            { EntityType.Videos, "الفيديوهات" }
        };

                foreach (var type in supportedTypes)
                {
                    int typeId = (int)type;

                    int total = type switch
                    {
                        EntityType.News => await _context.Posts.CountAsync(p =>
                            p.EntityId == (int)Enums.EntityType.News &&
                            p.IsDeleted == false && p.IsActive == true &&
                            p.CreatedDate >= fromDate && p.CreatedDate <= toDate),

                        EntityType.Spaces => await _context.Posts.CountAsync(p =>
                            p.EntityId == (int)Enums.EntityType.Spaces &&
                            p.IsDeleted == false && p.IsActive == true &&
                            p.CreatedDate >= fromDate && p.CreatedDate <= toDate),

                        EntityType.AdvCompany => await _context.Posts.CountAsync(p =>
                            p.EntityId == (int)Enums.EntityType.AdvCompany &&
                            p.IsDeleted == false && p.IsActive == true &&
                            p.CreatedDate >= fromDate && p.CreatedDate <= toDate),

                        EntityType.Events => await _context.Events.CountAsync(e =>
                            e.EntityId == (int)Enums.EntityType.Events &&
                            e.IsDeleted == false && e.IsActive == true &&
                            e.CreatedDate >= fromDate && e.CreatedDate <= toDate),

                        EntityType.Activity => await _context.Activities.CountAsync(a =>
                            a.EntityId == (int)Enums.EntityType.Activity &&
                            a.IsDeleted == false && a.IsActive == true &&
                            a.CreatedDate >= fromDate && a.CreatedDate <= toDate),

                        EntityType.Channels => await _context.Channels.CountAsync(c =>
                            c.EntityId == (int)Enums.EntityType.Channels &&
                            c.IsDeleted == false && c.IsActive == true &&
                            c.CreatedDate >= fromDate && c.CreatedDate <= toDate),

                        EntityType.Videos => await _context.Videos.CountAsync(v =>
                            v.EntityId == (int)Enums.EntityType.Videos &&
                            v.IsDeleted == false && v.IsActive == true &&
                            v.CreatedDate >= fromDate && v.CreatedDate <= toDate),

                        _ => 0
                    };

                    int totalComments = await _context.Comments.CountAsync(c =>
                        c.EntityId == typeId &&
                        c.CreatedDate >= fromDate && c.CreatedDate <= toDate);

                    int totalInteractions = await _context.InterActions.CountAsync(i =>
                        i.EntityId == typeId &&
                        i.CreatedDate >= fromDate && i.CreatedDate <= toDate);

                    // ✅ DISTINCT user views
                    int uniqueUserViewsWeb = await _context.UserViews
                        .Where(v => v.EntityId == typeId && v.Platform=="web" &&
                                    v.CreatedAt >= fromDate && v.CreatedAt <= toDate)
                        .Select(v => new { v.UserId })
                        .Distinct()
                        .CountAsync();
                    int uniqueUserViewsMobile = await _context.UserViews
                        .Where(v => v.EntityId == typeId && v.Platform == "mobile" &&
                                    v.CreatedAt >= fromDate && v.CreatedAt <= toDate)
                        .Select(v => new { v.UserId })
                        .Distinct()
                        .CountAsync();


                    result.Add(new
                    {
                        entity = type.ToString(),
                        entityAr = entityNamesAr[type],
                        entityEn = type.ToString(),
                        total = total,
                        totalComments = total > 0 ? totalComments:0,
                        totalInteractions = total > 0 ? totalInteractions:0,
                        uniqueUserViewsWeb = total > 0 ? uniqueUserViewsWeb : 0,
                        uniqueUserViewsMobile = total > 0 ? uniqueUserViewsMobile : 0
                    });
                }

                var resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(result, 1);
                return _result;
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


    }
}
