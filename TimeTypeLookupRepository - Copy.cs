using Core.Helpers;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.Extensions.Logging;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto.EmployeeTimeConfiguration;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.Core.Models.OutSource_Employees;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models.OutSourceEmployees;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    public class LeaveTypeRepository : BaseRepository<LeaveType>, ILeaveTypeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly LeaveType_Assembler _Assembler;

        public LeaveTypeRepository(
            ApplicationDbContext context,
            ILogger logger) : base(context, logger)
        {
            _context = context;
            _logger = logger;
            _Assembler = new LeaveType_Assembler();

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

        public async Task<OperationOutput> AddNewAsync(LeaveTypeDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;
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
                var EmployeeTimeConfigurations = new List<LeaveType>();
                var _EmployeeTimeConfigurations = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                EmployeeTimeConfigurations = _EmployeeTimeConfigurations.Where(i => (bool)i.IsDeleted == false).ToList();
                var _EmployeeTimeConfigurationsDto = _Assembler.WriteListDto(EmployeeTimeConfigurations);
                var counts = Count(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_EmployeeTimeConfigurationsDto, counts);
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
                var entities = await FindAllAsync(i => i.IsDeleted == false);
                var _entities = _Assembler.WriteListDto(entities);

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
                var EmployeeTimeConfiguration = await FindAsync(f => f.Id == id);
                if (EmployeeTimeConfiguration != null)
                {
                    var _EmployeeTimeConfiguration = _Assembler.WriteDto(EmployeeTimeConfiguration);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_EmployeeTimeConfiguration, 1);
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

        public async Task<OperationOutput> UpdateEntity(LeaveTypeDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.UpdatedDate = DateTime.Now;
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.CreatedDate = DateTime.Now;
                    _entity.DescriptionEn = entity.DescriptionEn;
                    _entity.DescriptionAr = entity.DescriptionAr;
                    _entity.NameEn = entity.NameEn;
                    _entity.NameAr = entity.NameAr;
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
    }
}
