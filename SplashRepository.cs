using Core.Helpers;
using Nupco.Core.Assembler;
using Nupco.Core.Dto.Splash;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL.Models;
using Nupco.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Data.Entity;
using static Core.Helpers.Enums;

namespace Nupco.EF.Repositories
{
    internal class SplashRepository : BaseRepository<Splash>, ISpalshRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly SplashAssembler _assembler;

        public SplashRepository(
            ApplicationDbContext context,
            ILogger logger) : base(context, logger)
        {
            _context = context;
            _logger = logger;
            _assembler = new SplashAssembler();
        }
        public async Task<OperationOutput> AddEntityAsync(SplashDto dto)
        {
            try
            {
                // Check if a record already exists for the same employee, month, and year
               

                // Create the Spalsh entity from the DTO
                var entity = _assembler.WriteDal(dto);

                // Add the main entity to the database
                await _context.Splashs.AddAsync(entity);
                await _context.SaveChangesAsync(); // Save to generate the Id for the entity

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Spalsh.");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }






        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {

                var entities = await GetAllAsync();
                var _entities = _assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = _entities.Count();
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntityAsync(int id, SplashDto dto)
        {
            try
            {
                // Find the existing entity
                var existingEntity = await _context.Splashs
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (existingEntity == null)
                {
                    return ResultOutputData.GenerateResultOutputFailure("Spalsh not found.");
                }

            
                // Update properties
                existingEntity.Type = dto.Type;
                existingEntity.Url = dto.Url;

                // Update details
         

                // Save changes
                _context.Splashs.Update(existingEntity);
                await _context.SaveChangesAsync();

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Spalsh.");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> DeleteEntityAsync(int id)
        {
            try
            {
                // Find the existing entity
                var existingEntity = await _context.Splashs.FindAsync(id);


                if (existingEntity == null)
                {
                    return ResultOutputData.GenerateResultOutputFailure("Spalsh not found.");
                }

                // Remove the entity
                _context.Splashs.Remove(existingEntity);
                await _context.SaveChangesAsync();

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Spalsh.");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetByIdAsync(int id)
        {
            var existingEntity = await _context.Set<Splash>().FirstOrDefaultAsync(E => E.Id == id);

            var _entity = _assembler.WriteDto(existingEntity);

            ResultOutputData resultOutput = new ResultOutputData();
            var count = 1;
            var _result = resultOutput.GenearetResultOutput(_entity, count);
            return _result;
        }

        public async Task<OperationOutput> GetEntityWithMaxIdAsync_Api()
        {
            try
            {
                var maxEntity = await Task.FromResult(_context.Set<Splash>()
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefault());
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(maxEntity, 1);
                return _result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEntityWithMaxIdAsync.");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<Splash> GetEntityWithMaxIdAsync()
        {
            try
            {
                var maxEntity = await Task.FromResult(_context.Set<Splash>()
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefault());
                return maxEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEntityWithMaxIdAsync.");
                return null;
            }
        }



    }

}
