using Core.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.EmployeeTimeConfiguration;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    public class EmployeeTimeConfigurationRepository : BaseRepository<EmployeeTimeConfiguration>, IEmployeeTimeConfigurationRepository
    {
        private readonly IConfiguration _configuration;
        private readonly EmployeeTimeConfiguration_Assembler _Assembler;

        public EmployeeTimeConfigurationRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _Assembler = new EmployeeTimeConfiguration_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(EmployeeTimeConfigurationDto entity)
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
                var EmployeeTimeConfigurations = new List<EmployeeTimeConfiguration>();
                var _EmployeeTimeConfigurations = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                EmployeeTimeConfigurations = _EmployeeTimeConfigurations.Where(i => i.IsDeleted == false).ToList();
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

        public async Task<OperationOutput> UpdateEntity(EmployeeTimeConfigurationDto entity)
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
                    _entity.Discription = entity.Discription;
                    _entity.NumberOfHours = entity.NumberOfHours;
                    _entity.StartDate = entity.StartDate;
                    _entity.EndDate = entity.EndDate;
                    _entity.StartHour = entity.StartHour;
                    _entity.EndHour = entity.EndHour;
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
