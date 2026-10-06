using Core.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    public class OfferRepository : BaseRepository<Offer>, IOfferRepository
    {
        private readonly Offer_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "OfferData";


        public OfferRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new Offer_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;


        }

        public async Task<OperationOutput> CreateNewOffer(OfferDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(entityDto);


                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);

                var _entityDto = _Assembler.WriteDto(_entity);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entityDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewAsync(OfferDto entityDto)
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
                if (!String.IsNullOrEmpty(entityDto.OriginalPicDescBase64))
                {
                    entityDto.OriginalDescPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicDescBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicDescBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalDescPic;
                }
                var _model = _Assembler.WriteDal(entityDto);
                _model.OriginalPic = entityDto.OriginalPic;
                _model.OriginalDescPic = entityDto.OriginalDescPic;

                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);
                var _entityDto = _Assembler.WriteDto(_entity);


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entityDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<OfferDto> entitiesDto)
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

        public async Task<OperationOutput> GetAllOfferByFilterKey(FilterByOffer _filter)
        {
            try
            {
                List<OfferDto> _Offers = new List<OfferDto>();
                var Offers = new List<Offer>();
                if (!string.IsNullOrEmpty(_filter.searchBy))
                    _filter.searchBy = _filter.searchBy.ToLower().Trim();

                Offers = await _context.Offers.Where(i =>
                                                 i.TitleAr.ToLower().Contains(_filter.searchBy) || i.TitleEn.ToLower().Contains(_filter.searchBy) ||
                                                 i.Category.ToLower().Contains(_filter.searchBy) || i.Code.ToLower().Contains(_filter.searchBy) ||
                                                 i.DescriptionAr.ToLower().Contains(_filter.searchBy) || i.DescriptionEn.ToLower().Contains(_filter.searchBy))
                 .Where(i => i.IsDeleted == false && i.IsActive == true)
                 .Where(i => i.EndDate.Value.Date >= DateTime.Now.Date)
                 .Include(i => i.CreatedByUsers)
                 .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                 .OrderByDescending(o => o.Id)
                 .ToListAsync();




                _Offers = _Assembler.WriteListDto(Offers);

                //foreach (var _entity in _Offers)
                //{
                //    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _filter.userId);
                //    _entity.IsLiked = userIsLiked != 0 ? true : false;
                //    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                //    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                //}
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Offers, counts);
                return _result;

            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetAllByPagenationAsync in Offer : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }


        }

        public async Task<OperationOutput> GetAllByPagenationAsync(FilterByOffer _filter)
        {
            try
            {
                List<OfferDto> _Offers;

                var spec = Specification<Offer>.All.And(new OfferSpecification(_filter));
                var baseQuery = await _context.Offers
                    .Include(i => i.CreatedByUsers)
                    .Where(spec.ToExpression())
                    .Where(i => i.IsDeleted == false && i.CreatedDate.Value.Date >= _filter.StartDate.Value.Date && i.CreatedDate.Value.Date <= _filter.EndDate.Value.Date)
                    .OrderByDescending(o => o.Id)
                    .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                    .Take((int)_filter.pageSize).AsNoTrackingWithIdentityResolution().ToListAsync();


                _Offers = _Assembler.WriteListDto(baseQuery);


                // Total count (consider caching this)
                var counts = await _context.Offers
                    .Where(i => i.IsDeleted == false && i.EntityId == _filter.entityId)
                    .CountAsync();

                var result = new ResultOutputData().GenearetResultOutput(_Offers, counts);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllByPagenationAsync in Offer: {ex.Message}");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            try
            {
                var Offers = await FindAllAsync(i => i.IsDeleted == false);
                var _Offers = _Assembler.WriteListDto(Offers);

                //foreach (var _entity in _Offers)
                //{
                //    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == userId);
                //    _entity.IsLiked = userIsLiked != 0 ? true : false;
                //    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                //    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                //}
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Offers, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(OfferDto entityDto)
        {
            try
            {
                var _entity = _Assembler.WriteDal(entityDto);

                if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64))
                {
                    _entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : _entity.OriginalPic;
                }
                if (!String.IsNullOrEmpty(entityDto.OriginalPicDescBase64))
                {
                    _entity.OriginalDescPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicDescBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicDescBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : _entity.OriginalDescPic;
                }

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

        public async Task<OperationOutput> UpdateEntityAsync(OfferDto entityDto, object key)
        {
            try
            {
                var _Offer = await GetByIdAsync(entityDto.Id.Value);
                if (entityDto.IsActive == true)
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPic;
                }


                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _Offer);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<OfferDto> entities)
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





        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "CreatedByUsers" };
                var Offers = new List<Offer>();

                var _OfferList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                Offers = _OfferList.Where(i => i.IsDeleted == false).ToList();

                var _OffersDto = _Assembler.WriteListDto(Offers);

                //foreach (var _entity in _OffersDto)
                //{
                //    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _filter.UserId);
                //    _entity.IsLiked = userIsLiked != 0 ? true : false;
                //    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                //    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                //}
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_OffersDto, counts);
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
            string[] stringArray = { "CreatedByUsers" };

            var _Offer = await FindAsync(f => f.Id == id, stringArray);
            if (_Offer is not null)
            {
                var _entity = _Assembler.WriteDto(_Offer);
                //var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == _entity.CreatedBy);
                //_entity.IsLiked = userIsLiked != 0 ? true : false;
                //_entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                //_entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

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

        public async Task<OperationOutput> GetAllByPagenationOffer(FilterByOffer _filter, CancellationToken cancellationToken)
        {
            try
            {
                var spec = Specification<Offer>.All.And(new OfferSpecification(_filter));
                var Offers = new List<Offer>();


                Offers = await _context.Offers
                .Where(spec.ToExpression()).Where(i => i.IsDeleted == false)
                .Include(i => i.CreatedByUsers)
                .OrderByDescending(o => o.EndDate)
                .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                .ToListAsync();

                var _Offers = _Assembler.WriteListDto(Offers);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Offers, counts);
                return _result;
            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetAllByPagenationOffer : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }



        }

    }
}
