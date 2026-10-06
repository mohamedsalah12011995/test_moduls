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
using Nupco.DAL.Models.Users;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Nupco.Core.UsersManagerEmployeeSpecification;

namespace Nupco.EF.Repositories
{
    internal class UsersManagerEmployeeRepository : BaseRepository<UsersManagerEmployee>, IUsersManagerEmployeeRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly UsersManagerEmployee_Assembler _UsersManagerEmployee_Assembler;

        private string[] stringArray = { "" };


        public UsersManagerEmployeeRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _UsersManagerEmployee_Assembler = new UsersManagerEmployee_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(UsersManagerEmployeeDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                var _model = _UsersManagerEmployee_Assembler.WriteDal(entity);
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

        public async Task<OperationOutput> GetAllWithAttendanceAsync(PagenationBy _filter)
        {
            try
            {
                // Filter is already applied in FindAllAsync, no need to filter again after pagination
                var UsersManagerEmployee = await FindAllAsync(i=> i.IsDeleted==false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                var _UsersManagerEmployee = _UsersManagerEmployee_Assembler.WriteListDto(UsersManagerEmployee.ToList());
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_UsersManagerEmployee, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GetAllByPagenationAsync(FilterByPagenation filter,CancellationToken cancellationToken)
        {
            try
            {
                var spec = Specification<UsersManagerEmployee>.All
                    .And(new UsersManagerEmployeeSpecification(filter))
                    .And(new UsersManagerEmployeeNotDeletedSpecification());

                int pageNumber = filter.pageNumber ?? 1;
                int pageSize = filter.pageSize ?? 10;

                int skip = (pageNumber) * pageSize;

                var entities = await FindAllAsync(
                    spec.ToExpression(),
                    skip,
                    filter.pageSize
                );

                var totalCount = await CountAsync(spec.ToExpression());

                var dtoList = _UsersManagerEmployee_Assembler.WriteListDto(entities);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(dtoList, totalCount);
                return _result;
            }
            catch (Exception ex)
            {
                // لو عندك logger
                // _logger.LogError(ex, "Error in GetAllByPagenationAsync");

                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {

                var entities = await FindAllAsync(i => i.IsDeleted == false);
                var _entities = _UsersManagerEmployee_Assembler.WriteListDto(entities);

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

        public async Task<OperationOutput> GetEmployeesByManager(string managerName)
        {
            try
            {
                var employeeList = await _context.UsersManagerEmployees
                    .Where(u => u.UserNameManager.ToLower()== managerName.ToLower())
                    .Select(u => new
                    {
                        u.Id,
                        u.UserNameEmployee,
                        u.CodeEmployee
                    })
                    .Distinct()
                    .ToListAsync();

                ResultOutputData result = new ResultOutputData();


                if (employeeList.Count > 0)
                {
                    var _result = result.GenearetResultOutput(employeeList, employeeList.Count());
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


        public async Task<OperationOutput> GetAllManagers()
        {
            try
            {
                // نختار فقط أسماء المديرين بناءً على الموظفين
                var managersList = await _context.UsersManagerEmployees
                    .Where(u => !string.IsNullOrEmpty(u.UserNameManager))   
                    .Select(u => u.UserNameManager)                         
                    .Distinct()
                    .ToListAsync();                                          



                if (managersList.Count > 0)
                {
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(managersList, managersList.Count());
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
        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var UsersManagerEmployee = await GetByIdAsync(id);
                if (UsersManagerEmployee != null)
                {
                    var _UsersManagerEmployee = _UsersManagerEmployee_Assembler.WriteDto(UsersManagerEmployee);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_UsersManagerEmployee, 1);
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

        public async Task<OperationOutput> UpdateEntity(UsersManagerEmployeeDto dto)
        {
            try
            {
                var entity = await FindAsync(x => x.Id == dto.Id);

                if (entity == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                // Update entity fields
                entity.IsActive = true;
                entity.IsDeleted = false;
                //entity.UserNameManager = dto.UserNameManager;
                //entity.UserNameEmployee = dto.UserNameEmployee;
                entity.CodeEmployee = dto.CodeEmployee;

                Update(entity);

                // Update user code only if needed
                if (!string.IsNullOrWhiteSpace(entity.CodeEmployee))
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserName == entity.UserNameEmployee);

                    if (user != null && user.Code != entity.CodeEmployee)
                    {
                        user.Code = entity.CodeEmployee;
                        _context.Users.Update(user);
                    }
                }

                await SaveChangesAsync();

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(entity, 1);
                return _result;
            }
            catch (Exception ex)
            {
                // ⚠️ لو عندك Logger يفضّل تسجيل الخطأ
                // _logger.LogError(ex, "Error updating UsersManagerEmployee");

                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(UsersManagerEmployeeDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<UsersManagerEmployeeDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<UsersManagerEmployeeDto> entities)
        {
            throw new NotImplementedException();
        }

        public Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            throw new NotImplementedException();
        }
    }
}
