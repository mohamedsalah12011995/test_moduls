using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
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
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;

namespace Nupco.EF.Repositories
{
    internal class VideoRepository : BaseRepository<Video>, IVideoRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Video_Assembler _Assembler;

        private string[] stringArray = null;


        public VideoRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Assembler = new Video_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(VideoDto entity)
        {
            try
            {

                entity.IsActive = true;
                entity.IsDeleted = false;
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

        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                var spec = Specification<Video>.All.And(new VideoSpecification(_filter));

                var Videos = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize,stringArray);

                var _entities = _Assembler.WriteListDto(Videos);

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

        public async Task<OperationOutput> GetAllDataByYear(string year , string userId)
        {
            try
            {
                int _year = 0;
                if (!string.IsNullOrEmpty(year))
                     _year = int.Parse(year);

                var entities = await FindAllAsync(i =>  i.CreatedDate.Value.Year==_year);
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

        public async Task<OperationOutput> GetAllVideosByUserId(string userId)
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

        public async Task<OperationOutput> GetAllVideosWithUserIsLike(string userId)
        {
            try
            {
                var entities = await FindAllAsync(f => f.IsDeleted == false);
                var _entities = _Assembler.WriteListDto(entities);

                foreach (var _entity in _entities)
                {
                    var userIsLiked = await _context.InterActions.AsNoTrackingWithIdentityResolution().CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == userId);
                    _entity.IsLiked = userIsLiked != 0 ? true : false;
                    _entity.LikeNo = await _context.InterActions.AsNoTrackingWithIdentityResolution().CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.AsNoTrackingWithIdentityResolution().CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                }

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetAllVideosWithUserIsLike : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var Video = await GetByIdAsync(id);
                if (Video != null)
                {
                    var _Video = _Assembler.WriteDto(Video);
                    _Video.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _Video.Id.ToString() && f.EntityId == _Video.EntityId);
                    _Video.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _Video.Id.ToString() && f.EntityId == _Video.EntityId);

                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Video, 1);
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

        public async Task<OperationOutput> UpdateEntity(VideoDto entity)
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
                    entity.EntityId = entity.EntityId;
                    entity.ReferenceId = entity.ReferenceId;
                    entity.UpdatedBy = entity.UpdatedBy;

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



    }
}
