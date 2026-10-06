using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.SpinWheel;
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
    internal class SpinWheelRepository : BaseRepository<SpinWheel>, ISpinWheelRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly SpinWheel_Assembler _SpinWheel_Assembler;

        private string[] stringArray = {  };


        public SpinWheelRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _SpinWheel_Assembler = new SpinWheel_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(SpinWheelDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                var _model = _SpinWheel_Assembler.WriteDal(entity);
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

                var SpinWheelItem = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize,null);
                var _SpinWheelItem = _SpinWheel_Assembler.WriteListDto(SpinWheelItem);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_SpinWheelItem, counts);
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

                var entities = await FindAllAsync(i => i.IsDeleted == false, stringArray);
                var _entities = _SpinWheel_Assembler.WriteListDto(entities);

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
                var SpinWheelItem = await GetByIdAsync(id);
                if (SpinWheelItem != null)
                {
                    var _SpinWheelItem = _SpinWheel_Assembler.WriteDto(SpinWheelItem);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_SpinWheelItem, 1);
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

        public async Task<OperationOutput> DeleteEntity(int id)
        {
            try
            {
                var SpinWheelItem = await GetByIdAsync(id);
                if (SpinWheelItem != null)
                {
                    SpinWheelItem.IsDeleted = true;
                    var _model = Update(SpinWheelItem);
                    await SaveChangesAsync();

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_model, 1);
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

        public async Task<OperationOutput> UpdateEntity(SpinWheelDto entity)
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
                    _entity.UpdatedBy = entity.UpdatedBy;
                    _entity.UpdatedDate = DateTime.Now;
                    _entity.OriginalPic = entity.OriginalPic;

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
        public async Task<SpinWheelDto?> GetWheelForUserAsync(string userId)
        {
            // 1. First try to get a group wheel that the user belongs to
            var groupWheel = await _context.SpinWheels
                .Include(w => w.Items)
                .Where(w => w.IsDeleted != true && w.IsActive == true && w.GroupId.HasValue)
                .Join(
                    _context.GroupUsers.Where(gu => gu.UserId == userId),
                    wheel => wheel.GroupId,
                    groupUser => groupUser.GroupId,
                    (wheel, groupUser) => wheel
                )
                .FirstOrDefaultAsync();

            SpinWheelDto _SpinWheelItem;

            if (groupWheel != null)
            {
                // Check if user already spun this wheel
                var alreadySpun = await _context.SpinWheelUserPrizes
                    .AnyAsync(p => p.UserId == userId
                                && p.SpinWheelItem.SpinWheelId == groupWheel.Id
                                && p.IsDeleted == false);

                if (alreadySpun)
                {
                    return null; // user already spun this group wheel
                }

                _SpinWheelItem = _SpinWheel_Assembler.WriteDto(groupWheel);
                return _SpinWheelItem;
            }

            // 2. If no group wheel found, get the current public wheel for the active period
            var publicWheel = await _context.SpinWheels
                .Include(w => w.Items)
                .Where(w =>
                    w.IsDeleted != true &&
                    w.IsActive == true &&
                    w.IsPublic == true &&
                    w.StartDate <= DateTime.UtcNow &&
                    w.EndDate >= DateTime.UtcNow
                )
                .FirstOrDefaultAsync();

            if (publicWheel != null)
            {
                // Check if user already spun this public wheel
                var alreadySpun = await _context.SpinWheelUserPrizes
                    .AnyAsync(p => p.UserId == userId
                                && p.SpinWheelItem.SpinWheelId == publicWheel.Id
                                && p.IsDeleted == false);

                if (alreadySpun)
                {
                    return null; // user already spun this public wheel
                }

                _SpinWheelItem = _SpinWheel_Assembler.WriteDto(publicWheel);
                return _SpinWheelItem;
            }

            return null;
        }


        public async Task<bool> AssignGroupAsync(int wheelId, int groupId)
        {
            var wheel = await _context.SpinWheels.FindAsync(wheelId);
            if (wheel == null) return false;

            wheel.GroupId = groupId;
            wheel.IsPublic = false;
            wheel.UpdatedDate = DateTime.UtcNow;

            _context.SpinWheels.Update(wheel);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MakePublicAsync(int wheelId)
        {
            var wheel = await _context.SpinWheels.FindAsync(wheelId);
            if (wheel == null) return false;

            wheel.GroupId = null;
            wheel.IsPublic = true;
            wheel.UpdatedDate = DateTime.UtcNow;

            _context.SpinWheels.Update(wheel);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<IEnumerable<SpinWheelWinnerStatsDto>> GetWinnersAsync(int wheelId)
        {
            return await _context.SpinWheelUserPrizes
                .Where(p => p.SpinWheelItem.SpinWheelId == wheelId && p.IsDeleted != true)
                .Select(p => new SpinWheelWinnerStatsDto
                {
                    UserId = p.UserId,
                    UserName = p.User.UserName,
                    PrizeTitleEn = p.PrizeTitleEn,
                    PrizeTitleAr = p.PrizeTitleAr,
                    PrizeValue = p.PrizeValue,
                    WonDate = p.CreatedDate ?? DateTime.UtcNow
                })
                .ToListAsync();
        }

        public async Task<SpinWheelStatsDto> GetWheelStatisticsAsync(int wheelId)
        {
            var items = await _context.SpinWheelItems
                .Where(i => i.SpinWheelId == wheelId && i.IsDeleted != true)
                .ToListAsync();

            var winners = await _context.SpinWheelUserPrizes
                .Where(p => p.SpinWheelItem.SpinWheelId == wheelId && p.IsDeleted != true)
                .ToListAsync();

            return new SpinWheelStatsDto
            {
                TotalPrizes = items.Sum(i => (i.Count ?? 0)),
                RemainingPrizes = items.Where(i => i.Count.HasValue).Sum(i => i.Count.Value),
                TotalWinners = winners.Count,
                TotalPrizeValueDistributed = winners.Sum(p => p.PrizeValue ?? 0)
            };
        }




        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(SpinWheelDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<SpinWheelDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<SpinWheelDto> entities)
        {
            throw new NotImplementedException();
        }
    }
}
