using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Posts;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;

namespace Nupco.EF.Repositories
{
    public class PostRepository : BaseRepository<Post>, IPostRepository
    {
        private readonly Post_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "postData";


        public PostRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new Post_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;


        }

        public async Task<OperationOutput> CreateNewPostWithMultipleImages(PostDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;
                var _entity = _Assembler.WriteDal(entityDto);


                var _entityDto = await AddAsync(_entity);
                await SaveChangesAsync();

                var responseDto = _Assembler.WriteDto(_entity);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(responseDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> CreateNewPost(PostDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;
                var _entity = _Assembler.WriteDal(entityDto);


                var _entityDto = await AddAsync(_entity);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);

                var responseDto = _Assembler.WriteDto(_entity);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(responseDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewAsync(PostDto entityDto)
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
                var _entity = _Assembler.WriteDal(entityDto);


                var _entityDto = await AddAsync(_entity);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);

                var responseDto = _Assembler.WriteDto(_entity);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(responseDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<PostDto> entitiesDto)
        {
            try
            {
                var _entities = _Assembler.WriteListDal(entitiesDto);
                var _entitiesDto = await AddRangeAsync(_entities);

                var responseDtos = _Assembler.WriteListDto(_entitiesDto);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(responseDtos, 1);
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


        //private async Task<List<PostDto>> GetPostDataFromCashOrDb(FiltersBy _filter)
        //{
        //    List<PostDto> CashPost = new List<PostDto>();
        //    List<PostDto> _Posts = new List<PostDto>();

        //    if (!_cache.TryGetValue(cacheKey, out CashPost))
        //    {
        //        _filter.pageSize = 7;
        //        var spec = Specification<Post>.All.And(new PostSpecification(_filter));
        //        var Posts = new List<Post>();
        //        if (_filter.IsActive is true)
        //        {
        //            Posts = await _context.Posts.Where(spec.ToExpression()).Where(i => i.IsDeleted == false && i.IsActive == true)
        //                   .Include(i => i.CreatedByUsers)
        //                   .Include(i => i.UpdatedByUsers)
        //                   .OrderByDescending(o => o.Id)
        //                   .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
        //                   .ToListAsync();
        //        }
        //        else
        //        {

        //            Posts = await _context.Posts.Where(spec.ToExpression()).Where(i => i.IsDeleted == false)
        //                   .Include(i => i.CreatedByUsers)
        //                   .Include(i => i.UpdatedByUsers)
        //                   .OrderByDescending(o => o.Id)
        //                   .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
        //                   .ToListAsync();
        //        }
        //        _Posts = _Assembler.WriteListDto(Posts);
        //        _cache.Set(cacheKey, _Posts);
        //    }
        //    else
        //    {
        //        _Posts = (List<PostDto>)_cache.Get(cacheKey);
        //    }
        //    return _Posts;
        //}

        public async Task<OperationOutput> GetAllPostByFilterKey(FilterByPost _filter)
        {
            try
            {
                List<PostDto> _Posts = new List<PostDto>();
                var Posts = new List<Post>();
                if (!string.IsNullOrEmpty(_filter.searchBy))
                    _filter.searchBy = _filter.searchBy.ToLower().Trim();



                Posts = await _context.Posts.Where(i =>
                                                 i.TitleAr.ToLower().Contains(_filter.searchBy) || i.TitleEn.ToLower().Contains(_filter.searchBy) ||
                                                 i.PostBriefeContentAr.ToLower().Contains(_filter.searchBy) || i.PostBriefeContentEn.ToLower().Contains(_filter.searchBy) ||
                                                 i.PostContentAr.ToLower().Contains(_filter.searchBy) || i.PostContentEn.ToLower().Contains(_filter.searchBy))
                 .Where(i => i.IsDeleted == false && i.IsActive == true && i.EntityId == _filter.entityId)
                 .Include(i => i.CreatedByUsers)
                 .Include(i => i.UpdatedByUsers).OrderByDescending(o => o.Id)
                 .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)

                 .ToListAsync();





                _Posts = _Assembler.WriteListDto(Posts);

                foreach (var _entity in _Posts)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _filter.userId);
                    _entity.IsLiked = userIsLiked != 0 ? true : false;
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.PostImageThumps = HandleImageAndThump(_entity);

                }
                int counts = await CountAsync(i => i.IsDeleted == false && i.EntityId == _filter.entityId);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Posts, counts);
                return _result;

            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetAllByPagenationAsync in Post : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }


        }

        public async Task<OperationOutput> GetAllPostByFilterKeyWithMultipleImages(FilterByPost _filter)
        {
            try
            {
                // Normalize search text once
                var searchBy = _filter.searchBy?.ToLower().Trim();

                // Step 1: Filter and project posts with only needed fields
                var postsQuery = _context.Posts
                    .AsNoTracking()
                    .Where(p => p.IsDeleted != true && p.IsActive == true && p.EntityId == _filter.entityId);
                if (_filter.id.HasValue) // if id is nullable (int?)
                {
                    postsQuery = postsQuery.Where(p => p.Id == _filter.id.Value);
                }

                if (!string.IsNullOrEmpty(searchBy))
                {
                    postsQuery = postsQuery.Where(i =>
                        i.TitleAr.ToLower().Contains(searchBy) ||
                        i.TitleEn.ToLower().Contains(searchBy) ||
                        i.PostBriefeContentAr.ToLower().Contains(searchBy) ||
                        i.PostBriefeContentEn.ToLower().Contains(searchBy) ||
                        i.PostContentAr.ToLower().Contains(searchBy) ||
                        i.PostContentEn.ToLower().Contains(searchBy));
                }

                var pagedPosts = await postsQuery
                    .Include(p => p.CreatedByUsers)
                    .Include(p => p.UpdatedByUsers)
                    .OrderByDescending(p => p.Id)
                    .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                    .Take((int)_filter.pageSize)
                    .ToListAsync();

                // Step 2: Adapt post entities to DTOs (use Assembler or direct projection)
                var postDtos = _Assembler.WriteListWithImageThumpDto(pagedPosts);

                var postIds = postDtos.Select(p => p.Id.ToString()).ToList();

                // Step 3: Batch load likes/comments to avoid N+1 problem
                var likes = await _context.InterActions
                    .Where(i => postIds.Contains(i.ItemId) && i.EntityId == _filter.entityId)
                    .GroupBy(i => i.ItemId)
                    .Select(g => new { ItemId = g.Key, Count = g.Count() })
                    .ToListAsync();

                //var userLikes = await _context.InterActions
                //    .Where(i => postIds.Contains(i.ItemId) && i.EntityId == _filter.entityId && i.UserId == _filter.userId)
                //    .Select(i => i.ItemId)
                //    .ToListAsync();

                var comments = await _context.Comments
                    .Where(c => postIds.Contains(c.ItemId) && c.EntityId == _filter.entityId)
                    .GroupBy(c => c.ItemId)
                    .Select(g => new { ItemId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Step 4: Map likes/comments to DTOs efficiently
                foreach (var post in postDtos)
                {
                    post.IsLiked = await _context.InterActions.AnyAsync(f => f.ItemId == post.Id.ToString() && f.EntityId == post.EntityId && f.UserId == _filter.userId);
                    post.LikeNo = likes.FirstOrDefault(l => l.ItemId == post.Id.ToString())?.Count ?? 0;
                    post.CommentNo = comments.FirstOrDefault(c => c.ItemId == post.Id.ToString())?.Count ?? 0;
                    post.PostImageThumps = HandleImageAndThump(post);
                    if (_filter.entityId == 1)
                    {
                        post.CreatedByName = null;

                    }
                }

                // Step 5: Get total count only once
                var totalCount = await _context.Posts
                    .CountAsync(p => p.IsDeleted != true && p.EntityId == _filter.entityId);

                var result = new ResultOutputData();
                var output = result.GenearetResultOutput(postDtos, totalCount);
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in GetAllPostByFilterKeyWithMultipleImages: " + ex.Message);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetAllPostByFilterKeyWithMultipleImagesWithUserId(FilterByPost _filter)
        {
            try
            {
                List<PostWithImageThumpDto> _Posts = new List<PostWithImageThumpDto>();
                var Posts = new List<Post>();
                if (!string.IsNullOrEmpty(_filter.searchBy))
                    _filter.searchBy = _filter.searchBy.ToLower().Trim();



                Posts = await _context.Posts.Where(i =>
                                                 i.TitleAr.ToLower().Contains(_filter.searchBy) || i.TitleEn.ToLower().Contains(_filter.searchBy) ||
                                                 i.PostBriefeContentAr.ToLower().Contains(_filter.searchBy) || i.PostBriefeContentEn.ToLower().Contains(_filter.searchBy) ||
                                                 i.PostContentAr.ToLower().Contains(_filter.searchBy) || i.PostContentEn.ToLower().Contains(_filter.searchBy))
                 .Where(i => i.IsDeleted == false && i.IsActive == true && i.EntityId == _filter.entityId && i.CreatedBy == _filter.UserProfileId)
                 .Include(i => i.CreatedByUsers)
                 .Include(i => i.UpdatedByUsers).OrderByDescending(o => o.Id)
                 .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)

                 .ToListAsync();




                _Posts = _Assembler.WriteListWithImageThumpDto(Posts);

                foreach (var _entity in _Posts)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _filter.userId);
                    _entity.IsLiked = userIsLiked != 0 ? true : false;
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.PostImageThumps = HandleImageAndThump(_entity);

                }
                int counts = await CountAsync(i => i.IsDeleted == false && i.EntityId == _filter.entityId);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Posts, counts);
                return _result;

            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetAllByPagenationAsync in Post : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }


        }


        //public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        //{
        //    try
        //    {
        //        List<PostDto> _Posts = new List<PostDto>();

        //        // Check if data is already in cache
        //        //if (_filter.pageNumber == 0)
        //        //{
        //        //    _Posts =  await GetPostDataFromCashOrDb(_filter);

        //        //}
        //        //else
        //        //{
        //           // _filter.pageSize = 7;
        //            var spec = Specification<Post>.All.And(new PostSpecification(_filter));
        //            var Posts = new List<Post>();
        //            if (_filter.IsActive is true)
        //            {
        //                Posts = await _context.Posts.Where(spec.ToExpression()).Where(i => i.IsDeleted == false && i.IsActive == true)
        //                       .Include(i => i.CreatedByUsers)
        //                       .Include(i => i.UpdatedByUsers)
        //                       .OrderByDescending(o => o.Id)
        //                       .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
        //                       .ToListAsync();
        //            }
        //            else
        //            {

        //                Posts = await _context.Posts.Where(spec.ToExpression()).Where(i => i.IsDeleted == false)
        //                       .Include(i => i.CreatedByUsers)
        //                       .Include(i => i.UpdatedByUsers)
        //                       .OrderByDescending(o => o.Id)
        //                       .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
        //                       .ToListAsync();
        //            }



        //            _Posts = _Assembler.WriteListDto(Posts);
        //        //}

        //        foreach (var _entity in _Posts)
        //        {
        //            var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _filter.UserId);
        //            _entity.IsLiked = userIsLiked != 0 ? true : false;
        //            _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
        //            _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

        //        }
        //        var counts = await CountAsync(i => i.IsDeleted == false && i.EntityId == _filter.entityId);
        //        ResultOutputData result = new ResultOutputData();
        //        var _result = result.GenearetResultOutput(_Posts, counts);
        //        return _result;

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("this Error in GetAllByPagenationAsync in Post : " + ex.Message);
        //        var Result = ResultOutputData.GenearetResultOutputCatch();
        //        return Result;
        //    }


        //}
        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                List<PostDto> _Posts;

                var spec = Specification<Post>.All.And(new PostSpecification(_filter));
                var baseQuery = _context.Posts
                    .Include(i => i.CreatedByUsers)
                    .Include(i => i.UpdatedByUsers)
                    .Where(spec.ToExpression())
                    .Where(i => i.IsDeleted == false);

                if (_filter.IsActive is true)
                {
                    baseQuery = baseQuery.Where(i => i.IsActive == true);
                }

                // Projection to DTO and pagination
                //var query = baseQuery
                //    .OrderByDescending(o => o.Id)
                //    .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                //    .Take((int)_filter.pageSize);

                var posts = await baseQuery
                    .OrderByDescending(o => o.Id)
                    .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                    .Take((int)_filter.pageSize).AsNoTracking().ToListAsync();

                _Posts = _Assembler.WriteListDto(posts);


                // Batch fetch likes and comments
                var postIds = _Posts.Select(p => p.Id.ToString()).ToList();
                var entityId = _filter.entityId;
                var userId = _filter.UserId;

                // Fetch all relevant interactions in one query
                var interactionsQuery = _context.InterActions
                    .Where(f => postIds.Contains(f.ItemId) && f.EntityId == entityId);

                var userLikesDict = await interactionsQuery
                    .Where(f => f.UserId == userId)
                    .GroupBy(f => f.ItemId)
                    .Select(g => new { ItemId = g.Key, IsLiked = g.Any() })
                    .ToDictionaryAsync(x => x.ItemId, x => x.IsLiked);

                var totalLikesDict = await interactionsQuery
                    .GroupBy(f => f.ItemId)
                    .Select(g => new { ItemId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ItemId, x => x.Count);

                var commentsCountDict = await _context.Comments
                    .Where(f => postIds.Contains(f.ItemId) && f.EntityId == entityId)
                    .GroupBy(f => f.ItemId)
                    .Select(g => new { ItemId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ItemId, x => x.Count);

                // Map results to DTOs
                foreach (var post in _Posts)
                {
                    var postIdStr = post.Id.ToString();
                    post.IsLiked = userLikesDict.GetValueOrDefault(postIdStr, false);
                    post.LikeNo = totalLikesDict.GetValueOrDefault(postIdStr, 0);
                    post.CommentNo = commentsCountDict.GetValueOrDefault(postIdStr, 0);
                }

                // Total count (consider caching this)
                var counts = await _context.Posts
                    .Where(i => i.IsDeleted == false && i.EntityId == _filter.entityId)
                    .CountAsync();

                var result = new ResultOutputData().GenearetResultOutput(_Posts, counts);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllByPagenationAsync in Post: {ex.Message}");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            try
            {
                var Posts = await FindAllAsync(i => i.IsDeleted == false);
                var _Posts = _Assembler.WriteListDto(Posts);

                foreach (var _entity in _Posts)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == userId);
                    _entity.IsLiked = userIsLiked != 0 ? true : false;
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                }
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Posts, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(PostDto entityDto)
        {
            try
            {
                if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64))
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPic;
                }

                var _entity = _Assembler.WriteDal(entityDto);
                _entity.UpdatedDate = DateTime.Now;
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = Update(_entity);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);


                var objDto = _Assembler.WriteDto(_entity);

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
        public async Task<OperationOutput> UpdateEntityWithMultipleImages(PostDto entityDto)
        {
            try
            {

                var _entity = _Assembler.WriteDal(entityDto);
                _entity.UpdatedDate = DateTime.Now;
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = Update(_entity);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);


                var objDto = _Assembler.WriteDto(_entity);

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

        public async Task<OperationOutput> UpdateEntityAsync(PostDto entityDto, object key)
        {
            try
            {
                var _Post = await GetByIdAsync(entityDto.Id.Value);
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
                var entity = UpdateAsync(_entity, _Post);
                await SaveChangesAsync();

                var responseDto = _Assembler.WriteDto(_entity);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(responseDto, 1);

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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<PostDto> entities)
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





        public async Task<OperationOutput> GetAllByPagenation(FiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "PostsUsers" };
                var spec = Specification<Post>.All.And(new PostSpecification(_filter));
                var Posts = new List<Post>();
                if (_filter.InterestsId > 0)
                {
                    var _Posts = await FindAllAsync(spec.ToExpression(), stringArray);
                    Posts = _Posts.Where(i => i.IsDeleted == false).ToList();
                }
                else
                {
                    var _PostList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize);
                    Posts = _PostList.Where(i => i.IsDeleted == false).ToList();
                }

                var _PostsDto = _Assembler.WriteListDto(Posts);
                foreach (var _entity in _PostsDto)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _filter.UserId);
                    _entity.IsLiked = userIsLiked != 0 ? true : false;
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                }
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_PostsDto, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<OperationOutput> GetDataById_WithImageThump(int id)
        {
            string[] stringArray = { "CreatedByUsers" };

            var _Post = await FindAsync(f => f.Id == id, stringArray);
            if (_Post is not null)
            {
                var _entity = _Assembler.WritePostWithImageThumpDto(_Post);
                var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _entity.CreatedBy);
                _entity.IsLiked = userIsLiked != 0 ? true : false;
                _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                _entity.PostImageThumps = HandleImageAndThump(_entity);



                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithOutJWT(_entity, 1);
                return _result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
        }

        private List<PostImageThump> HandleImageAndThump(PostWithImageThumpDto _entity)
        {
            var PostImageThumps = new List<PostImageThump>();

            if (!string.IsNullOrEmpty(_entity.OriginalPic))
            {
                var originalPics = _entity.OriginalPic?.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
                var thumpPics = _entity.ThumpPic?.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();


                for (int i = 0; i < Math.Max(originalPics.Count, thumpPics.Count); i++)
                {
                    var postImageThump = new PostImageThump
                    {
                        OriginalPic = i < originalPics.Count ? originalPics[i] : null,
                        ThumpPic = i < originalPics.Count ? "ico_" + originalPics[i] : originalPics[i],

                        // Optional: dummy sizes or real image dimensions (see note below)
                        OriginalPicWidth = originalPics.Any() ? Images.GetWidthAndHight(originalPics[i]).GetAwaiter().GetResult().width : 0,
                        OriginalPicHight = originalPics.Any() ? Images.GetWidthAndHight(originalPics[i]).GetAwaiter().GetResult().height : 0,
                        ThumpPicWidth = originalPics.Any() ? Images.GetWidthAndHight("ico_" + originalPics[i]).GetAwaiter().GetResult().width : 0,
                        ThumpPicHight = originalPics.Any() ? Images.GetWidthAndHight("ico_" + originalPics[i]).GetAwaiter().GetResult().height : 0
                    };

                    PostImageThumps.Add(postImageThump);
                }
            }

            return PostImageThumps;
        }

        private List<PostImageThump> HandleImageAndThump(PostDto _entity)
        {
            var PostImageThumps = new List<PostImageThump>();

            if (!string.IsNullOrEmpty(_entity.OriginalPic))
            {
                var originalPics = _entity.OriginalPic?.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
                var thumpPics = _entity.ThumpPic?.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();


                for (int i = 0; i < Math.Max(originalPics.Count, thumpPics.Count); i++)
                {
                    var postImageThump = new PostImageThump
                    {
                        OriginalPic = i < originalPics.Count ? originalPics[i] : null,
                        ThumpPic = i < originalPics.Count ? "ico_" + originalPics[i] : originalPics[i],

                        // Optional: dummy sizes or real image dimensions (see note below)
                        OriginalPicWidth = originalPics.Any() ? Images.GetWidthAndHight(originalPics[i]).GetAwaiter().GetResult().width : 0,
                        OriginalPicHight = originalPics.Any() ? Images.GetWidthAndHight(originalPics[i]).GetAwaiter().GetResult().height : 0,
                        ThumpPicWidth = originalPics.Any() ? Images.GetWidthAndHight("ico_" + originalPics[i]).GetAwaiter().GetResult().width : 0,
                        ThumpPicHight = originalPics.Any() ? Images.GetWidthAndHight("ico_" + originalPics[i]).GetAwaiter().GetResult().height : 0
                    };

                    PostImageThumps.Add(postImageThump);
                }
            }

            return PostImageThumps;
        }


        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            string[] stringArray = { "CreatedByUsers" };

            var _Post = await FindAsync(f => f.Id == id, stringArray);
            if (_Post is not null)
            {
                var _entity = _Assembler.WriteDto(_Post);
                var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _entity.CreatedBy);
                _entity.IsLiked = userIsLiked != 0 ? true : false;
                _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                _entity.PostImageThumps = HandleImageAndThump(_entity);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithOutJWT(_entity, 1);
                return _result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
        }


        public async Task<OperationOutput> GetDataByIdAndEntityIdAsync(PostFilter postFilter)
        {
            string[] stringArray = { "CreatedByUsers" };

            var _Post = await FindAsync(f => f.Id == postFilter.id && f.EntityId == postFilter.entityId, stringArray);
            if (_Post is not null)
            {
                var _entity = _Assembler.WriteDto(_Post);
                var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == postFilter.id.ToString() && f.EntityId == postFilter.entityId && f.UserId == postFilter.userId);
                _entity.IsLiked = userIsLiked != 0 ? true : false;
                _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == postFilter.id.ToString() && f.EntityId == postFilter.entityId);
                _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == postFilter.id.ToString() && f.EntityId == postFilter.entityId);
                _entity.PostImageThumps = HandleImageAndThump(_entity);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutputWithOutJWT(_entity, 1);
                return _result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
        }

        public async Task<OperationOutput> Reported(int id, bool isReported)
        {
            try
            {
                var entity = await _context.Posts.FirstOrDefaultAsync(f => f.Id == id);
                if (entity is not null)
                {
                    entity.IsReported = isReported;
                    _context.Posts.Update(entity);
                    await _context.SaveChangesAsync();

                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutputWithOutJWT(entity, 1);
                    return _result;
                }


                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<byte[]> GetByteArray(int entityId)
        {
            string[] stringArray = { "CreatedByUsers", "UpdatedByUsers" };

            var posts = await FindAllAsync(i => i.IsDeleted == false && i.EntityId == entityId, stringArray);
            var _posts = _Assembler.WriteListEportDto(posts);

            var Data = _posts.Select(s => new PostExport
            {
                Id = s.Id,
                TitleAr = s.TitleAr,
                TitleEn = s.TitleEn,
                PostBriefeContentAr = s.PostBriefeContentAr,
                PostBriefeContentEn = s.PostBriefeContentEn,
                CreatedByName = s.CreatedByName,
                UpdatedByName = s.UpdatedByName,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate,
                LikeNo = _context.InterActions.Count(f => f.ItemId == s.Id.ToString() && f.EntityId == entityId),
                CommentNo = _context.Comments.Count(f => f.ItemId == s.Id.ToString() && f.EntityId == entityId),

            }).ToList();

            var dataTable = DataTableConverter.CreateDataTable(Data);
            return GetFileArray(dataTable); ;
        }


        public async Task<OperationOutput> GetAllByPagenationPost(FiltersBy _filter, CancellationToken cancellationToken)
        {
            try
            {
                var spec = Specification<Post>.All.And(new PostSpecification(_filter));
                var Posts = new List<Post>();
                if (_filter.IsActive is true)
                {
                    Posts = await _context.Posts.Where(spec.ToExpression()).Where(i => i.IsDeleted == false && i.IsActive == true)
                           .Include(i => i.CreatedByUsers)
                           .Include(i => i.UpdatedByUsers)
                           .OrderByDescending(o => o.Id)
                           .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                           .ToListAsync();
                }
                else
                {

                    Posts = await _context.Posts.Where(spec.ToExpression()).Where(i => i.IsDeleted == false)
                           .Include(i => i.CreatedByUsers)
                           .Include(i => i.UpdatedByUsers)
                           .OrderByDescending(o => o.Id)
                           .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                           .ToListAsync();
                }



                var _Posts = _Assembler.WriteListDto(Posts);

                foreach (var _entity in _Posts)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _filter.UserId);
                    _entity.IsLiked = userIsLiked != 0 ? true : false;
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                }
                var counts = await CountAsync(i => i.IsDeleted == false && i.EntityId == _filter.entityId);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Posts, counts);
                return _result;
            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetAllByPagenationPost : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }



        }

    }
}
