using Core.Helpers;
using Nupco.Core.Assembler;
using Nupco.Core.Dto.EmployeeOfTheMonth;
using Nupco.Core.Interfaces;
using Nupco.DAL.Models;
using Nupco.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nupco.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Nupco.EF.Repositories
{
    public class EmployeeOfTheMonthRepository : BaseRepository<EmployeeOfTheMonth>, IEmployeeOfTheMonthRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly EmployeeOfTheMonthAssembler _assembler;

        public EmployeeOfTheMonthRepository(
            ApplicationDbContext context,
            ILogger logger) : base(context, logger)
        {
            _context = context;
            _logger = logger;
            _assembler = new EmployeeOfTheMonthAssembler();
        }
        public async Task<OperationOutput> AddEntityAsync(EmployeeOfTheMonthDto dto)
        {
            try
            {
                // Check if a record already exists for the same employee, month, and year
                var existingEntity = await _context.EmployeesOfTheMonth
                    .FirstOrDefaultAsync(e => e.Month == dto.Month &&
                                              e.Year == dto.Year);

                if (existingEntity != null)
                {
                    return ResultOutputData.GenerateResultOutputFailure("this month already has a record");
                }

                // Create the EmployeeOfTheMonth entity from the DTO
                var entity = _assembler.WriteDal(dto);

                // Add the main entity to the database
                await _context.EmployeesOfTheMonth.AddAsync(entity);
                await _context.SaveChangesAsync(); // Save to generate the Id for the entity

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Employee of the Month.");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }



        public new async Task<IEnumerable<EmployeeOfTheMonth>> GetAllAsync()
        {
            return await _context.Set<EmployeeOfTheMonth>().Include(E => E.Details).ToListAsync();
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

        public async Task<OperationOutput> UpdateEntityAsync(int id, EmployeeOfTheMonthDto dto)
        {
            try
            {
                // Find the existing entity
                var existingEntity = await _context.EmployeesOfTheMonth.Include(e => e.Details)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (existingEntity == null)
                {
                    return ResultOutputData.GenerateResultOutputFailure("Employee of the month not found.");
                }

                var SameMonthEntity = await _context.EmployeesOfTheMonth
                .FirstOrDefaultAsync(e => e.Month == dto.Month &&
                              e.Year == dto.Year && dto.Id != e.Id);

                if (SameMonthEntity != null)
                {
                    return ResultOutputData.GenerateResultOutputFailure("this month already has a record");
                }

                // Update properties
                existingEntity.EmployeeId = dto.EmployeeId;
                existingEntity.EmployeeName = dto.EmployeeName;
                existingEntity.Month = dto.Month;
                existingEntity.Year = dto.Year;
                existingEntity.PictureUrl = dto.PictureUrl ?? existingEntity.PictureUrl;

                // Update details
                existingEntity.Details.Clear(); // Remove old details
                foreach (var detail in dto.Details)
                {
                    existingEntity.Details.Add(new EmployeeOfTheMonthDetail
                    {
                        ArKey = detail.ArKey,
                        ArValue = detail.ArValue,
                        EnKey = detail.EnKey,
                        EnValue = detail.EnValue,
                    });
                }

                // Save changes
                _context.EmployeesOfTheMonth.Update(existingEntity);
                await _context.SaveChangesAsync();

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Employee of the Month.");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> DeleteEntityAsync(int id)
        {
            try
            {
                // Find the existing entity
                var existingEntity = await _context.EmployeesOfTheMonth
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (existingEntity == null)
                {
                    return ResultOutputData.GenerateResultOutputFailure("Employee of the month not found.");
                }

                // Remove the entity
                _context.EmployeesOfTheMonth.Remove(existingEntity);
                await _context.SaveChangesAsync();

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Employee of the Month.");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetByIdAsync(int id)
        {
            var existingEntity = await _context.Set<EmployeeOfTheMonth>().Include(E => E.Details).FirstOrDefaultAsync(E => E.Id == id);

            var _entity = _assembler.WriteDto(existingEntity);

            ResultOutputData resultOutput = new ResultOutputData();
            var count = 1;
            var _result = resultOutput.GenearetResultOutput(_entity, count);
            return _result;
        }
        public async Task<OperationOutput> GetByMonthAndYear(int month , int year)
        {
            var existingEntity = await _context.Set<EmployeeOfTheMonth>().Include(E => E.Details).FirstOrDefaultAsync(E => E.Month == month && E.Year == year);
            if (existingEntity == null)
            {
                return ResultOutputData.GenearetResultOutputSuccess();
            }
            var _entity = _assembler.WriteDto(existingEntity);

            ResultOutputData resultOutput = new ResultOutputData();
            var count = 1;
            var _result = resultOutput.GenearetResultOutput(_entity, count);
            return _result;
        }

        public async Task<OperationOutput> GetListOfKeys(string? Lang = "Ar")
        {
            List<string> res = new List<string>();
            if(Lang == null || Lang?.ToLower() == "ar")
            {
                 res = await _context.Set<EmployeeOfTheMonthDetail>().Select(D => D.ArKey).Distinct().ToListAsync();
            }
            else
            {
                 res = await _context.Set<EmployeeOfTheMonthDetail>().Select(D => D.EnKey).Distinct().ToListAsync();
            }

            ResultOutputData resultOutput = new ResultOutputData();
            var _result = resultOutput.GenearetResultOutput(res, res.Count);
            return _result;

        }
    }
}
