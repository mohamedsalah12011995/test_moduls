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
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    internal class CountryRepository : BaseRepository<Country>, ICountryRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Country_Assembler _Country_Assembler;

        private string[] stringArray = { "Country" };


        public CountryRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Country_Assembler = new Country_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(CountryDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                var _model = _Country_Assembler.WriteDal(entity);
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
                var spec = Specification<Country>.All.And(new CountrySpecification(_filter));

                var Country = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                var _Country = _Country_Assembler.WriteListDto(Country);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Country, counts);
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
                var _entities = _Country_Assembler.WriteListDto(entities);

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
                var Country = await GetByIdAsync(id);
                if (Country != null)
                {
                    var _Country = _Country_Assembler.WriteDto(Country);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Country, 1);
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

        public async Task<OperationOutput> UpdateEntity(CountryDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.NameAr = entity.NameAr;
                    _entity.NameEn = entity.NameEn;

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

        public Task<OperationOutput> UpdateEntityAsync(CountryDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<CountryDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<CountryDto> entities)
        {
            throw new NotImplementedException();
        }
    }
}
