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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    internal class SpinWheelUserPrizeRepository : BaseRepository<SpinWheelUserPrize>, ISpinWheelUserPrizeRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly SpinWheelUserPrize_Assembler _SpinWheel_Assembler;

        private string[] stringArray = {  };


        public SpinWheelUserPrizeRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _SpinWheel_Assembler = new SpinWheelUserPrize_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(SpinWheelUserPrizeDto entity)
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
                // Include User with the query
                var spinWheelUserPrizes = await _context.SpinWheelUserPrizes
                 .Where(i => i.IsDeleted == false)
                 .Include(i => i.User)
                 .Include(i => i.SpinWheelItem) 
                 .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                 .Take((int)_filter.pageSize)
                 .ToListAsync();

                // Map to DTOs
                var spinWheelUserPrizeDtos = spinWheelUserPrizes.Select(p => new SpinWheelUserPrizeDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserFullName = (p.User != null
                    ? $"{(p.User.FirstName ?? "").Trim()} {(p.User.LastName ?? "").Trim()}"
                    : string.Empty),
                    SpinWheelItemId = p.SpinWheelItemId,
                    PrizeTitleEn = p.PrizeTitleEn,
                    PrizeTitleAr = p.PrizeTitleAr,
                    PrizeValue = p.PrizeValue,
                    SpinWheelId = p.SpinWheelItem != null ? p.SpinWheelItem.SpinWheelId : 0,
                    CreatedDate = p.CreatedDate,
                    UpdatedDate = p.UpdatedDate,
                    UpdatedBy = p.UpdatedBy,
                    IsActive = p.IsActive,
                    ActivatedDate = p.ActivatedDate,
                    ActivatedBy = p.ActivatedBy,
                    IsDeleted = p.IsDeleted,
                    DeletedDate = p.DeletedDate,
                    DeletedBy = p.DeletedBy
                }).ToList();

                // Count total
                var counts = await _context.SpinWheelUserPrizes.CountAsync(i => i.IsDeleted == false);

                // Return result
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(spinWheelUserPrizeDtos, counts);
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
                var SpinWheelUserPrize = await GetByIdAsync(id);
                if (SpinWheelUserPrize != null)
                {
                    var _SpinWheelUserPrize = _SpinWheel_Assembler.WriteDto(SpinWheelUserPrize);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_SpinWheelUserPrize, 1);
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

        public async Task<OperationOutput> UpdateEntity(SpinWheelUserPrizeDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.UserId = entity.UserId;
                    _entity.SpinWheelItemId = entity.SpinWheelItemId;
                    _entity.PrizeTitleAr = entity.PrizeTitleAr;
                    _entity.PrizeTitleEn = entity.PrizeTitleEn;
                    _entity.PrizeValue = entity.PrizeValue;

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


        public async Task<OperationOutput> SpinAsync(string userId, int wheelId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Check if user already spun this wheel
                var alreadySpun = await _context.SpinWheelUserPrizes
                    .AnyAsync(p => p.UserId == userId
                                && p.SpinWheelItem.SpinWheelId == wheelId
                                && p.IsDeleted == false);

                if (alreadySpun)
                {
                    return ResultOutputData.GenerateResultOutputFailure("User has already spun once.");
                }

                // 2. Get available prizes for this wheel
                var availablePrizes = await _context.SpinWheelItems
                    .Where(p => p.SpinWheelId == wheelId
                             && p.IsDeleted == false
                             && p.IsActive == true
                             && p.Count > 0
                             && p.Probability > 0)
                    .ToListAsync();

                if (!availablePrizes.Any())
                {
                    return ResultOutputData.GenerateResultOutputFailure("No prizes available.");
                }

                // 3. Pick prize based on probability
                var totalProbability = availablePrizes.Sum(p => p.Probability ?? 0);
                if (totalProbability <= 0)
                {
                    return ResultOutputData.GenerateResultOutputFailure("Invalid prize probabilities.");
                }

                var roll = SecureRandom.NextDouble() * totalProbability;

                double cumulative = 0;
                SpinWheelItem selectedPrize = null;

                foreach (var prize in availablePrizes)
                {
                    cumulative += prize.Probability ?? 0;
                    if (roll <= cumulative)
                    {
                        selectedPrize = prize;
                        break;
                    }
                }

                if (selectedPrize == null)
                {
                    return ResultOutputData.GenerateResultOutputFailure("Failed to select prize.");
                }

                // 4. Try to decrement count with concurrency check
                selectedPrize.Count -= 1;
                selectedPrize.UpdatedDate = DateTime.UtcNow;

                try
                {
                    _context.SpinWheelItems.Update(selectedPrize);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    return ResultOutputData.GenerateResultOutputFailure("Sorry, this prize was already taken.");
                }

                // 5. Save user prize
                var userPrize = new SpinWheelUserPrize
                {
                    UserId = userId,
                    SpinWheelItemId = selectedPrize.Id,
                    PrizeTitleEn = selectedPrize.TitleEn,
                    PrizeTitleAr = selectedPrize.TitleAr,
                    PrizeValue = selectedPrize.PrizeValue,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                await _context.SpinWheelUserPrizes.AddAsync(userPrize);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                var dto = _SpinWheel_Assembler.WriteDto(userPrize);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dto, 1);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }



        public async Task<SpinWheelUserPrize?> GetByUserIdAsync(string userId)
       => await _context.SpinWheelUserPrizes
                        .Include(x => x.SpinWheelItem)
                        .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive == true);



        public async Task<bool> HasUserSpunAsync(string userId)
        {
            return await _context.SpinWheelUserPrizes
                .AnyAsync(x => x.UserId == userId && x.IsDeleted != true);
        }
        public async Task<SpinWheelItem?> GetRandomAvailableItemAndDecreaseCountAsync()
        {
            // Start transaction for concurrency handling
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Select available items with positive Count
                var availableItems = await _context.SpinWheelItems
                    .Where(i => i.Count > 0 && i.IsDeleted != true)
                    .ToListAsync();

                if (!availableItems.Any())
                {
                    return null; // No prizes left
                }

                // Pick random item
                var index = RandomNumberGenerator.GetInt32(availableItems.Count);
                var selectedItem = availableItems[index];

                // Decrease Count safely
                var dbItem = await _context.SpinWheelItems
                    .Where(i => i.Id == selectedItem.Id)
                    .FirstOrDefaultAsync();

                if (dbItem == null || dbItem.Count <= 0)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

                dbItem.Count -= 1;
                dbItem.UpdatedDate = DateTime.UtcNow;

                _context.SpinWheelItems.Update(dbItem);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return dbItem;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(SpinWheelUserPrizeDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<SpinWheelUserPrizeDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<SpinWheelUserPrizeDto> entities)
        {
            throw new NotImplementedException();
        }
    }
}
