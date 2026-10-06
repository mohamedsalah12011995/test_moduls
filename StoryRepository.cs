using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto.Story;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;

namespace Nupco.EF.Repositories
{
    internal class StoryRepository : BaseRepository<Story>, IStoryRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Story_Assembler _Assembler;

        private string[] stringArray = { "CreatedByUsers" };


        public StoryRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Assembler = new Story_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(StoryDto entity)
        {
            try
            {

                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(entity);
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

                DateTime todayStart = DateTime.Today;
                DateTime tomorrowStart = todayStart.AddDays(1);

                // 2. Define criteria to capture the full 24-hour window of the calendar day
                Expression<Func<Story, bool>> criteria = i =>
                    i.IsDeleted == false &&
                    i.CreatedDate >= todayStart &&
                    i.CreatedDate < tomorrowStart;

                var skip = (int)_filter.pageNumber * (int)_filter.pageSize;
                var take = (int)_filter.pageSize;

                var Stories = await FindAllAsync(criteria, skip, take, stringArray);
                var _entities = _Assembler.WriteListDto(Stories);

                foreach (var _entity in _entities)
                {
                    _entity.LikeNo = await _context.InterActions.CountAsync(f =>
                        f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                    _entity.CommentNo = await _context.Comments.CountAsync(f =>
                        f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                }

                var counts = await CountAsync(criteria);

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(_entities, counts);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetAllByPagenationAsyncForAdmin(FiltersBy _filter)
        {
            try
            {
                var spec = Specification<Story>.All.And(new StorySpecification(_filter));

                var Stories = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize,stringArray);

                var _entities = _Assembler.WriteListDto(Stories);

                foreach (var _entity in _entities)
                {
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                }

                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_entities, counts);
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
                var _entities = _Assembler.WriteListDto(entities);

                foreach (var _entity in _entities)
                {
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                }
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

        public async Task<OperationOutput> GetAllStoriesByUserId(string userId)
        {
            try
            {
                var entities = await FindAllAsync(f => f.CreatedBy == userId && f.IsDeleted == false);
                var _entities = _Assembler.WriteListDto(entities);

                foreach (var _entity in _entities)
                {
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                }

                ResultOutputData resultOutput = new ResultOutputData();
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

        public async Task<OperationOutput> GetAllStoriesWithUserIsLike(string userId)
        {
            try
            {
                var entities = await FindAllAsync(f => f.IsDeleted == false);
                var _entities = _Assembler.WriteListDto(entities);

                foreach (var _entity in _entities)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == userId);
                    _entity.IsLiked = userIsLiked != 0 ? true : false;
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                }

                ResultOutputData resultOutput = new ResultOutputData();
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
                var Story = await GetByIdAsync(id);
                if (Story != null)
                {
                    var _Story = _Assembler.WriteDto(Story);
                    _Story.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _Story.Id.ToString() && f.EntityId == _Story.EntityId);
                    _Story.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _Story.Id.ToString() && f.EntityId == _Story.EntityId);

                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Story, 1);
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

        public async Task<OperationOutput> UpdateEntity(StoryDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.TitleAr = entity.TitleAr;
                    _entity.TitleEn = entity.TitleEn;
                    _entity.OriginalPic = entity.OriginalPic;
                    _entity.ThumpPic = entity.ThumpPic;
                    _entity.UpdatedDate = DateTime.Now;
                    _entity.EntityId = entity.EntityId;
                    _entity.ReferenceId = entity.ReferenceId;
                    _entity.UpdatedBy = entity.UpdatedBy;

                    var _model =  Update(_entity);
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

        public async Task<OperationOutput> AssigndStoryAllowedUsers(List<string> userIds)
        {
            try
            {
                if (userIds == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var oldRecords = await _context.StoryAllowedUsers.ToListAsync();
                if (oldRecords.Any())
                {
                    _context.StoryAllowedUsers.RemoveRange(oldRecords);
                }

                var newUsers = userIds.Distinct().Select(userId => new StoryAllowedUser
                {
                    UserId = userId,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }).ToList();

                if (newUsers.Any())
                {
                    await _context.StoryAllowedUsers.AddRangeAsync(newUsers);
                }

                await _context.SaveChangesAsync();

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<OperationOutput> GetAllStoryAllowedUsers()
        {
            try
            {
                var entities = await _context.StoryAllowedUsers
                    .Include(i => i.User)
                    .ToListAsync();

                var resultData = entities.Select(s => new
                {
                    userId = s.UserId,
                    userName = s.User.UserName
                }).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(resultData, resultData.Count);

                return result;
            }
            catch
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetCommentsAndInterActionsByStoryId(int id)
        {
            try
            {
                var itemId = id.ToString();
                var entityId = (int)Enums.Entities.Stories;

                var likesList = await _context.InterActions
                    .AsNoTracking()
                    .Where(x =>
                        x.ItemId == itemId &&
                        x.EntityId == entityId &&
                        x.Type == "Like")
                    .GroupBy(x => x.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        UserName = g.First().User.UserName,
                        UserImage = g.First().User.OriginalPic,
                        LastLikeAt = g.Max(x => x.CreatedDate)
                    })
                    .OrderByDescending(x => x.LastLikeAt)
                    .ToListAsync();

                var commentsList = await _context.Comments
                    .AsNoTracking()
                    .Where(x =>
                        x.ItemId == itemId &&
                        x.EntityId == entityId)
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(x => new
                    {
                        CommentId = x.Id,
                        CommentText = x.Message,
                        CreatedAt = x.CreatedDate,
                        UserName = x.CreatedUser.UserName,
                        UserImage = x.CreatedUser.OriginalPic
                    })
                    .ToListAsync();

                var viewsList = await _context.UserViews
                    .AsNoTracking()
                    .Where(x =>
                        x.ItemId == itemId &&
                        x.EntityId == entityId)
                    .GroupBy(x => x.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        UserName = g.First().User.UserName,
                        UserImage = g.First().User.OriginalPic,

                        ViewsCount = g.Count(),
                        LastViewedAt = g.Max(x => x.CreatedAt)
                    })
                    .OrderByDescending(x => x.LastViewedAt)
                    .ToListAsync();


                var likesUsersCount = likesList.Count;
                var commentsCount = commentsList.Count;
                var viewsUsersCount = viewsList.Count;

                var viewsTotalCount = viewsList.Sum(x => x.ViewsCount);


                var result = new
                {
                    Likes = likesList,
                    Comments = commentsList,
                    Views = viewsList,

                    LikesUsersCount = likesUsersCount,
                    CommentsCount = commentsCount,
                    ViewsUsersCount = viewsUsersCount,

                    ViewsTotalCount = viewsTotalCount
                };

                return new ResultOutputData().GenearetResultOutput(result, 1);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
    }
}
