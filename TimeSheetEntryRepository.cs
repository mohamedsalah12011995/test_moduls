using Core.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.Core.Models.OutSource_Employees;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models.OutSourceEmployees;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;

namespace Nupco.EF.Repositories
{
    public class TimeSheetEntryRepository : BaseRepository<TimeSheetEntry>, ITimeSheetEntryRepository
    {
        private readonly TimeSheetEntry_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "TimeSheetEntryData";


        public TimeSheetEntryRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new TimeSheetEntry_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;


        }

        public async Task<OperationOutput> CreateNewTimeSheetEntry(TimeSheetEntryDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(entityDto);


                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);

                var _entityDto = _Assembler.WriteDto(_entity);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entityDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewAsync(TimeSheetEntryDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;


                var _model = _Assembler.WriteDal(entityDto);

                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);
                var _entityDto = _Assembler.WriteDto(_entity);


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entityDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<TimeSheetEntryDto> entitiesDto)
        {
            try
            {
                var _entities = _Assembler.WriteListDal(entitiesDto);
                var _entitiesDto = await AddRangeAsync(_entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entitiesDto, 1);
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
                var _entities = await FindAllAsync(i => i.IsDeleted == false);
                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            try
            {
                var TimeSheetEntrys = await FindAllAsync(i => i.IsDeleted == false);
                var _TimeSheetEntrys = _Assembler.WriteListDto(TimeSheetEntrys);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_TimeSheetEntrys, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(TimeSheetEntryDto entityDto)
        {
            try
            {

                var _entity = _Assembler.WriteDal(entityDto);

                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = Update(_entity);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);


                var objDto = _Assembler.WriteDto(_entity);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(objDto, 1);

                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> UpdateEntityAsync(TimeSheetEntryDto entityDto, object key)
        {
            try
            {
                var _TimeSheetEntry = await GetByIdAsync((int)entityDto.Id);


                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _TimeSheetEntry);
                await SaveChangesAsync();


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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<TimeSheetEntryDto> entities)
        {
            try
            {
                foreach (var item in entities)
                {
                    var find = await FindAsync(f => f.Id == item.Id);
                    find.IsDeleted = true;
                    Update(find);
                }

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





        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "CreatedByUser" };
                var TimeSheetEntrys = new List<TimeSheetEntry>();

                var _TimeSheetEntryList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                TimeSheetEntrys = _TimeSheetEntryList.Where(i => i.IsDeleted == false).ToList();

                var _TimeSheetEntrysDto = _Assembler.WriteListDto(TimeSheetEntrys);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_TimeSheetEntrysDto, counts);
                return _result;
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
                string[] stringArray = { "CreatedByUser" };

                var _TimeSheetEntry = await FindAsync(f => f.Id == id, stringArray);
                if (_TimeSheetEntry is not null)
                {
                    var _entity = _Assembler.WriteDto(_TimeSheetEntry);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_entity, 1);
                    return _result;
                }
                else
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


    }
}
