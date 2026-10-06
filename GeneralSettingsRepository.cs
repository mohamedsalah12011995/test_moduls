using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
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

namespace Nupco.EF.Repositories
{
    internal class GeneralSettingsRepository : BaseRepository<GeneralSetting>, IGeneralSettingsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly GeneralSetting_Assembler _Assembler;


        public GeneralSettingsRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Assembler = new GeneralSetting_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(GeneralSettingDto entity)
        {
            try
            {
                entity.IsActive = true;
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

        public async Task<OperationOutput> DeleteEntity(int id,string userId)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                Delete(find);
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
                var spec = Specification<GeneralSetting>.All.And(new GeneralSettingSpecification(_filter));

                var GeneralSetting = await FindAllAsync(i => i.IsActive == false || i.IsActive == true, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                var _GeneralSetting = _Assembler.WriteListDto(GeneralSetting);
                var counts = await CountAsync();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_GeneralSetting, counts);
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

                var entities = await FindAllAsync(i => i.IsActive == false || i.IsActive == true);
                var _entities = _Assembler.WriteListDto(entities);

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

        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var GeneralSetting = await GetByIdAsync(id);
                if (GeneralSetting != null)
                {
                    var _GeneralSetting = _Assembler.WriteDto(GeneralSetting);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_GeneralSetting, 1);
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

        public async Task<OperationOutput> UpdateEntity(GeneralSettingDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.IsActive = true;
                    _entity.ContentAr = entity.ContentAr;
                    _entity.ContentEn = entity.ContentEn;

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


        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(GeneralSettingDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<GeneralSettingDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<GeneralSettingDto> entities)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationOutput> GetAllGeneralSettingsByEntityId(int entityId)
        {

            try
            {
                var _entities = await FindAllAsync(i => i.EntityId == entityId);
                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.EntityId == entityId);
                var _GeneralSetting = _Assembler.WriteListDto(_entities);

                var _result = resultOutput.GenearetResultOutput(_GeneralSetting, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
    }
}
