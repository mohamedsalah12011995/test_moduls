using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Advertisements.Repository;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Advertisements;
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

namespace Nupco.EF.Repositories
{
    internal class AdvertisementsRepository : BaseRepository<Advertisement>, IAdvertisementsRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Advertisement_Assembler _Advertisement_Assembler;

        public AdvertisementsRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Advertisement_Assembler = new Advertisement_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(AdvertisementDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;
                var _model = _Advertisement_Assembler.WriteDal(entity);
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
                var spec = Specification<Advertisement>.All.And(new AdvertisementSpecification(_filter));

                var Advertisements = await FindAllPagenationAsync(i => i.IsDeleted == false, null, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                var _Advertisements = _Advertisement_Assembler.WriteListDto(Advertisements);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Advertisements, counts);
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
                var entities = await FindAllAsync(i => i.IsDeleted == false && i.FromDate <= i.ToDate && i.ToDate.Value.Date > DateTime.Now.Date);
                var _entities = _Advertisement_Assembler.WriteListDto(entities);

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
                var Advertisement = await GetByIdAsync(id);
                if (Advertisement != null)
                {
                    var _Advertisement = _Advertisement_Assembler.WriteDto(Advertisement);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Advertisement, 1);
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

        public async Task<OperationOutput> UpdateEntity(AdvertisementDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.UpdatedDate = DateTime.Now;
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.TitleAr = entity.TitleAr;
                    _entity.TitleEn = entity.TitleEn;
                    _entity.Code = entity.Code;
                    _entity.ReferenceId = entity.ReferenceId;
                    _entity.EntityId= entity.EntityId;
                    _entity.UpdatedBy = entity.UpdatedBy;
                    _entity.ContentAr= entity.ContentAr;
                    _entity.ContentEn= entity.ContentEn;
                    _entity.Url = entity.Url;
                    _entity.FromDate = entity.FromDate;
                    _entity.ToDate = entity.ToDate;
                    _entity.OriginalPic=entity.OriginalPic;
                    _entity.OriginalPicOther=entity.OriginalPicOther;
                    _entity.Destination= entity.Destination;
                    _entity.IsPopup= entity.IsPopup;
                    _entity.IsHomeSliderAd= entity.IsHomeSliderAd;
                    _entity.SerialNum= entity.SerialNum;

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


        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(AdvertisementDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<AdvertisementDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<AdvertisementDto> entities)
        {
            throw new NotImplementedException();
        }
    }
}
