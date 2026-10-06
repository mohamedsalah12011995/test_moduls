using Core.Helpers;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2019.Word.Cid;
using DocumentFormat.OpenXml.Office2021.PowerPoint.Comment;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Game;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Nupco.Core.Helpers.JWTHelper;
using Comment = Nupco.DAL.Models.Comment;

namespace Nupco.EF.Repositories
{
    internal class CommentsRepository : BaseRepository<Comment>, ICommentsRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;

        private readonly Comment_Assembler _Assembler = new Comment_Assembler();
        private readonly CommentReply_Assembler _Assembler_Reply = new CommentReply_Assembler();

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "CommentData";
        private string pathToSave = "";


        public CommentsRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
            var folderName = "Images/";
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            pathToSave = Path.Combine(sharedPath, folderName);
        }

        public async Task<OperationOutput> ActivateComment(int id, bool isAppreoved)
        {
            try
            {
                var entity = await FindAsync(f => f.Id == id);
                if (entity is not null)
                {
                    entity.IsApproved = isAppreoved;
                    Update(entity);
                    await SaveChangesAsync();

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

        public async Task<OperationOutput> ActivateCommentReply(int id, bool isAppreoved)
        {
            try
            {
                var entity = await _context.CommentsRplayes.FirstOrDefaultAsync(f => f.Id == id);
                if (entity is not null)
                {
                    entity.IsApproved = isAppreoved;
                    _context.CommentsRplayes.Update(entity);
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

        public async Task<OperationOutput> CreateComment(CommentObjDto dto)
        {
            ResultOutputData result = new ResultOutputData();

            try
            {
                var _createdDate = DateTime.Parse(dto.CreatedDate);
                
                var _model = _Assembler.WriteDal(dto);

                _model.CreatedDate = _createdDate;

                var _obj = await AddAsync(_model);
                await SaveChangesAsync();

                var _Comment = await _GetCommentById(_obj.Id);

                _cache.Remove(cacheKey);
                var _result = result.GenearetResultOutput(_Comment, 1);
                return _result;
            }

            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

          
        }

        public async Task<OperationOutput> UpdateComment(CommentObjDto entity)
        {
            try
            {
                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    if (!string.IsNullOrEmpty(entity.OriginalPicBase64))
                    {
                        _entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entity.OriginalPicBase64) ?
                                Images.SaveSingleImageOnServer(entity.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : entity.OriginalPic;
                    }

                    _entity.TitleAr = entity.TitleAr;
                    _entity.TitleEn = entity.TitleEn;
                    _entity.Message = entity.Message;

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

        public async Task<OperationOutput> UpdateCommentReply(CommentReplyObjDto entity)
        {
            try
            {


                var _entity = await _context.CommentsRplayes.FirstOrDefaultAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    if (!string.IsNullOrEmpty(entity.OriginalPicBase64))
                    {
                        _entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entity.OriginalPicBase64) ?
                                Images.SaveSingleImageOnServer(entity.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : _entity.OriginalPic;
                    }

                    _entity.CommentMessage = entity.CommentMessage;
                    _entity.Message = entity.Message;

                    var _model = _context.CommentsRplayes.Update(_entity);
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


        public async Task<OperationOutput> CreateCommentReply(CommentReplyObjDto dto)
        {
            try
            {
                ResultOutputData result = new ResultOutputData();

                var _createdDate = DateTime.Parse(dto.CreatedDate);
                var _commentReply = _Assembler_Reply.WriteDal(dto);
                _commentReply.CreatedDate = _createdDate;

                var obj = await _context.CommentsRplayes.AddAsync(_commentReply);
                await _context.SaveChangesAsync();

                var _CommentsRplayes = await _GetCommentReplies(11, (int)dto.CommentId, dto.CreatedBy);
                var _replay = _CommentsRplayes.FirstOrDefault(i => i.Id == obj.Entity.Id);
                var _result = result.GenearetResultOutput(_replay, 1);
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> CreateCommentWithAttchment(CommentObjDto dto)
        {
            ResultOutputData result = new ResultOutputData();

            try
            {
                var _createdDate = DateTime.Parse(dto.CreatedDate);
                var _model = _Assembler.WriteDal(dto);
                var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

                if (!String.IsNullOrEmpty(dto.OriginalPicBase64) && dto.fileType?.ToLower() == "image")
                {
                    _model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(dto.OriginalPicBase64, 1024,pathToSave, false, 0, pathToSave) : _model.OriginalPic;
                }
                if (!String.IsNullOrEmpty(dto.OriginalPicBase64) && dto.fileType?.ToLower() == "mp4")
                {
                    _model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                Images.UploadVideoFileNew(dto.OriginalPicBase64, sharedPath + "Videos/") : String.Empty;
                }


                _model.CreatedDate = _createdDate;

                var _obj = await AddAsync(_model);
                await SaveChangesAsync();

                var _Comment = await _GetCommentById(_obj.Id);

                _cache.Remove(cacheKey);
                var _result = result.GenearetResultOutput(_Comment, 1);
                return _result;
            }

            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }


        }

        public async Task<OperationOutput> UpdateCommentWithAttchment(CommentObjDto dto)
        {
            ResultOutputData result = new ResultOutputData();

            try
            {
                var _model = await FindAsync(f => f.Id == dto.Id);
                if (_model is null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var _createdDate = DateTime.Parse(dto.CreatedDate);
                var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

                if (!String.IsNullOrEmpty(dto.OriginalPicBase64) && dto.fileType?.ToLower() == "image")
                {
                    _model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(dto.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : _model.OriginalPic;
                }
                if (!String.IsNullOrEmpty(dto.OriginalPicBase64) && dto.fileType?.ToLower() == "mp4")
                {
                    _model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                Images.UploadVideoFileNew(dto.OriginalPicBase64, sharedPath + "Videos/") : String.Empty;
                }


                _model.TitleAr = dto.TitleAr;
                _model.TitleEn = dto.TitleEn;
                _model.Message = dto.Message;
                _model.OriginalPic = dto.IsAttachmentRemoved == false ? _model.OriginalPic :null;

                var _obj =  Update(_model);
                await SaveChangesAsync();

                var _Comment = await _GetCommentById(_obj.Id);

                _cache.Remove(cacheKey);
                var _result = result.GenearetResultOutput(_Comment, 1);
                return _result;
            }

            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<OperationOutput> CreateCommentReplyWithAttchment(CommentReplyObjDto dto)
        {
            try
            {
                ResultOutputData result = new ResultOutputData();
                var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

                var _createdDate = DateTime.Parse(dto.CreatedDate);
                var _commentReply = _Assembler_Reply.WriteDal(dto);


                if (!String.IsNullOrEmpty(dto.OriginalPicBase64) && dto.fileType?.ToLower() == "image")
                {
                    _commentReply.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(dto.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : _commentReply.OriginalPic;
                }
                if (!String.IsNullOrEmpty(dto.OriginalPicBase64) && dto.fileType?.ToLower() == "mp4")
                {
                    _commentReply.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                Images.UploadVideoFileNew(dto.OriginalPicBase64, sharedPath + "Videos/") : String.Empty;
                }

                _commentReply.CreatedDate = _createdDate;

                var obj = await _context.CommentsRplayes.AddAsync(_commentReply);
                await _context.SaveChangesAsync();

                var _CommentsRplayes = await _GetCommentReplies(11, (int)dto.CommentId, dto.CreatedBy);
                var _replay = _CommentsRplayes.FirstOrDefault(i => i.Id == obj.Entity.Id);
                var _result = result.GenearetResultOutput(_replay, 1);
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> UpdateCommentReplyWithAttchment(CommentReplyObjDto dto)
        {
            try
            {
               var _commentReply = await _context.CommentsRplayes.FirstOrDefaultAsync(f => f.Id == dto.Id);
                if (_commentReply is null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                ResultOutputData result = new ResultOutputData();
                var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

                if (!String.IsNullOrEmpty(dto.OriginalPicBase64) && dto.fileType?.ToLower() == "image")
                {
                    _commentReply.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(dto.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : _commentReply.OriginalPic;
                }
                if (!String.IsNullOrEmpty(dto.OriginalPicBase64) && dto.fileType?.ToLower() == "mp4")
                {
                    _commentReply.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                Images.UploadVideoFileNew(dto.OriginalPicBase64, sharedPath + "Videos/") : String.Empty;
                }

                _commentReply.Message = dto.Message;
                _commentReply.CommentMessage = dto.CommentMessage;
                _commentReply.OriginalPic = dto.IsAttachmentRemoved == false ? _commentReply.OriginalPic : null;

                var obj = _context.CommentsRplayes.Update(_commentReply);
                await _context.SaveChangesAsync();

                var _CommentsRplayes = await _GetCommentReplies(11, (int)dto.CommentId, dto.CreatedBy);
                var _replay = _CommentsRplayes.FirstOrDefault(i => i.Id == obj.Entity.Id);
                var _result = result.GenearetResultOutput(_replay, 1);
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> DeleteComment(int id)
        {
            try
            {
                var _entity = await FindAsync(i => i.Id == id);
                if (_entity is not null)
                {
                    var _commentsReplys = await _context.CommentsRplayes.Where(i => i.CommentId == id).ToListAsync();
                    if (_commentsReplys.Count() > 0)
                    {
                        _context.CommentsRplayes.RemoveRange(_commentsReplys);
                        await _context.SaveChangesAsync();
                    }
                    Delete(_entity);
                    await SaveChangesAsync();

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

        public async Task<OperationOutput> DeleteCommentReply(int id)
        {
            try
            {
                var _entity = await _context.CommentsRplayes.FirstOrDefaultAsync(i => i.Id == id);
                if (_entity is not null)
                {
                    _context.CommentsRplayes.Remove(_entity);
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

        public async Task<dynamic> Export(int entityId)
        {
            try
            {
                string[] stringArray = { "Entity", "CreatedUser" };
                var comments = new List<Comment>();
                if (entityId > 0)
                {
                    comments = (List<Comment>)await FindAllAsync(i => i.EntityId == entityId && i.IsApproved == true, stringArray);

                }
                else
                {
                    comments = (List<Comment>)await FindAllAsync(i => i.IsApproved == true, stringArray);
                }

                var _comments = _Assembler.WriteListEportDto(comments);

                var Data = _comments.Select(s => new CommentExport
                {
                    Id = s.Id,
                    EntityNameAr = s.EntityNameAr,
                    EntityNameEn = s.EntityNameEn,
                    CreatedByName = s.CreatedByName,
                    Message = s.Message,
                    UserName = s.UserName,
                    CreatedDate = s.CreatedDate,
                }).ToList();
                return Data;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
            

        }

        public async Task<OperationOutput> GetAllByPagenation(CommentBy _filter)
        {
            try
            {
                string[] includes = { "Entity", "CreatedUser", "CommentReplies" };
                var spec = Specification<Comment>.All.And(new CommentSpecification(_filter));

                List<Comment> comments;

                if (_filter.entityId == 0)
                {
                    comments = (await FindAllAsync(
                        spec.ToExpression(),
                        skip: _filter.pageSize * _filter.pageNumber,
                        take: _filter.pageSize,
                        includes: includes)).ToList();
                }
                else
                {
                    comments = (await FindAllAsync(
                        spec.ToExpression(),
                        includes)).ToList();
                }

                // Map to DTO
                var commentDtos = _Assembler.WriteListDto(comments);
                var commentIds = commentDtos.Select(c => c.Id.ToString()).ToList();

                // Step 1: Get replay count in batch
                var replyCounts = await _context.Comments
                    .Where(c => c.EntityId == _filter.entityComment && commentIds.Contains(c.ItemId))
                    .GroupBy(c => c.ItemId)
                    .Select(g => new { ItemId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Step 2: Get likes count in batch
                var likeCounts = await _context.InterActions
                    .Where(i => i.EntityId == _filter.entityComment && commentIds.Contains(i.ItemId))
                    .GroupBy(i => i.ItemId)
                    .Select(g => new { ItemId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Step 3: Assign counts efficiently
                foreach (var item in commentDtos)
                {
                    item.CountReplay = replyCounts.FirstOrDefault(r => r.ItemId == item.Id.ToString())?.Count ?? 0;
                    item.CountLikes = likeCounts.FirstOrDefault(l => l.ItemId == item.Id.ToString())?.Count ?? 0;
                }

                // Step 4: Get total approved comments count (filtered by entityId if needed)
                int totalCount = await CountAsync(c =>
                    c.IsApproved == true &&
                    (_filter.entityId == 0 || c.EntityId == _filter.entityId));

                var resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(commentDtos, totalCount);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetAllWithEntityAndItemId(CommentBy _filter)
        {
            try
            {
                string[] stringArray = { "CreatedUser" };

                List<Comment> Comments = new List<Comment>();
                var spec = Specification<Comment>.All.And(new CommentSpecification(_filter));

                Comments = (List<Comment>)await FindAllAsync(spec.ToExpression(), stringArray);
                Comments = Comments.Where(i => i.IsApproved == true).OrderBy(a => a.CreatedDate).ToList();
                var _Comments = _Assembler.WriteListDto(Comments);


                foreach (var item in _Comments)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == item.Id.ToString() && f.EntityId == 8 && f.UserId == _filter.UserId);
                    item.IsLiked = userIsLiked != 0 ? true : false;

                    item.CountReplay = await _context.CommentsRplayes.CountAsync(i => i.CommentId == item.Id && i.IsApproved == true);
                    item.CountLikes = await _context.InterActions.CountAsync(i => i.EntityId == _filter.entityComment && i.ItemId == item.Id.ToString());

                }

                var counts = Count(i => i.IsApproved == true);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Comments, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        private async Task<CommentDto> _GetCommentById(int id)
        {
            try
            {
                string[] stringArray = { "Entity", "CreatedUser", "CommentReplies" };

                List<Comment> Comments = new List<Comment>();

                var Comment = await FindAsync(i => i.IsApproved == true && i.Id == id, stringArray);

                var _Comment = _Assembler.WriteDto(Comment);

                _Comment.CommentReplies = _Assembler_Reply.WriteListDto(Comment.CommentReplies.ToList());


                _Comment.CountReplay = await CountAsync(i => i.EntityId == _Comment.EntityId && i.ItemId == _Comment.Id.ToString());
                _Comment.CountLikes = await _context.InterActions.CountAsync(i => i.EntityId == _Comment.EntityId && i.ItemId == _Comment.Id.ToString());

                
                return _Comment;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<OperationOutput> GetCommentById(int id)
        {
            try
            {
                var _Comment = await _GetCommentById(id);
                var counts = _Comment is not null ? Count(i => i.IsApproved == true) : 0;
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Comment, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetCommentsByUserId(string userId)
        {
            try
            {
                string[] stringArray = { "Entity", "CreatedUser", "CommentReplies" };
                var CommentList = new List<CommentListtDto>();
                
                var _Comments = await FindAllAsync(i=> i.CreatedBy == userId && i.EntityId==2 , stringArray);
                foreach (var comment in _Comments)
                {
                    var _userPosted = await _context.Posts.Where(i => i.EntityId == 2 && i.Id.ToString() == comment.ItemId&& i.IsDeleted == false).Include(i => i.CreatedByUsers).FirstOrDefaultAsync();
                    var _cpost = new CommentListtDto
                    {
                        PostCreatedByName = _userPosted?.CreatedByUsers?.UserName,
                        OriginalPicCreateedPost = _userPosted?.CreatedByUsers?.OriginalPic,
                        CreatedByName = comment?.CreatedUser?.UserName,
                        CreatedDate = comment?.CreatedDate,
                        Message = comment?.Message,
                        PostTitleAr = _userPosted?.TitleAr,
                        PostTitleEn = _userPosted?.TitleEn,
                        EntityNameAr = _userPosted?.Entity?.NameAr,
                        EntityNameEn = _userPosted?.Entity?.NameEn,

                    };
                    if (_cpost is not null)
                        CommentList.Add(_cpost);
                }


                var _CommentsVote = await FindAllAsync(i => i.CreatedBy == userId && i.EntityId == 22, stringArray);
                foreach (var comment in _CommentsVote)
                {
                    var _userVoted = await _context.Votes.Include(i => i.CreatedByUser).FirstOrDefaultAsync(i =>  i.Id.ToString() == comment.ItemId && i.IsDeleted == false);
                    var _cpost = new CommentListtDto
                    {
                        PostCreatedByName = _userVoted?.CreatedByUser?.UserName,
                        OriginalPicCreateedPost = _userVoted?.CreatedByUser?.OriginalPic,
                        CreatedByName = comment?.CreatedUser?.UserName,
                        CreatedDate = comment?.CreatedDate,
                        Message = comment?.Message,
                        PostTitleAr = _userVoted?.TitleAr,
                        PostTitleEn = _userVoted?.TitleEn,
                        EntityNameAr = _userVoted?.Entity?.NameAr,
                        EntityNameEn = _userVoted?.Entity?.NameEn,
                    };
                    if (_cpost is not null)
                        CommentList.Add(_cpost);
                }


                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(CommentList, CommentList.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetCommentReplies(int entityId, int commentId, string userId)
        {
            try
            {
                var _CommentsRplayes =await _GetCommentReplies(entityId, commentId, userId);

                var counts = _context.CommentsRplayes.Count(i => i.IsApproved == true && i.CommentId == commentId);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CommentsRplayes, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<List<CommentReplyDto>> _GetCommentReplies(int entityId, int commentId, string userId)
        {
            try
            {
                var CommentsRplayes = await _context.CommentsRplayes.Include(i => i.CreatedUser)
                    .Where(i => i.IsApproved == true && i.CommentId == commentId).ToListAsync();

                var _CommentsRplayes = _Assembler_Reply.WriteListDto(CommentsRplayes);

                foreach (var item in _CommentsRplayes)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == item.Id.ToString() && f.EntityId == entityId && f.UserId == userId);
                    item.IsLiked = userIsLiked != 0 ? true : false;
                    item.CountReplay = 0;
                    item.CountLikes = await _context.InterActions.CountAsync(i => i.EntityId == entityId && i.ItemId == item.Id.ToString());
                }

                return _CommentsRplayes;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return new List<CommentReplyDto>();
            }
        }


        public async Task<OperationOutput> GetCommentsByItemId(int entityId, string itemId)
        {
            try
            {
                List<CommentDto> CashComment = new List<CommentDto>();
                List<CommentDto> _Comments = new List<CommentDto>();
                ResultOutputData result = new ResultOutputData();

                //if (!_cache.TryGetValue(cacheKey, out CashComment))
                //{
                    string[] stringArray = { "Entity", "CreatedUser", "CommentReplies" };
                    var Comments = await FindAllAsync(i => i.EntityId == entityId && i.ItemId == itemId, stringArray);
                    _Comments = _Assembler.WriteListDto(Comments);
                    //_cache.Set(cacheKey, _Comments);

                //}
                //else
                //{
                //    _Comments = (List<CommentDto>)_cache.Get(cacheKey);
                //}
                var counts = _Comments.Count() > 0 ? Count() : 0;
                var _result = result.GenearetResultOutput(_Comments, counts);
                return _result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<OperationOutput> ReportedComment(int id, bool isReported)
        {
            try
            {
                var entity = await FindAsync(f => f.Id == id);
                if (entity is not null)
                {
                    entity.IsReported = isReported;
                    Update(entity);
                    await SaveChangesAsync();

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

        public async Task<OperationOutput> ReportedCommentReply(int id, bool isReported)
        {
            try
            {
                var entity = await _context.CommentsRplayes.FirstOrDefaultAsync(f => f.Id == id);
                if (entity is not null)
                {
                    entity.IsReported = isReported;
                    _context.CommentsRplayes.Update(entity);
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
    }
}
