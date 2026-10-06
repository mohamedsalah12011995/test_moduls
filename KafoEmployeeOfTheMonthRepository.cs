using Core.Helpers;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.EmployeeOfTheMonth;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models.Kafo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;

namespace Nupco.EF.Repositories
{
    public class KafoEmployeeOfTheMonthRepository : BaseRepository<KafoEmployeeOfTheMonth>, IKafoEmployeeOfTheMonthRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly KafoEmployeeOfTheMonth_Assembler _EmployeeOfTheMonth_Assembler;

        private string[] stringArray = { "EmployeeOfTheMonth" };


        public KafoEmployeeOfTheMonthRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _EmployeeOfTheMonth_Assembler = new KafoEmployeeOfTheMonth_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(KafoEmployeeOfTheMonthDto entity)
        {
            try
            {
                string _year = Dates.ConvertStringToDate(entity.Year).Value.Date.Year.ToString();
                string _month = Dates.ConvertStringToDate(entity.Month).Value.Date.Month.ToString();

                var ListItems = await _context.KafoEmployeeOfTheMonth.Where(i => i.IsDeleted == false && i.Year == _year && i.Month == _month).ToListAsync();
                if (ListItems.Count == 5)
                {
                    var Result = ResultOutputData.GenearetResultOutputLimit();
                    return Result;
                }
                if (!String.IsNullOrEmpty(entity.TopLevelNew))
                {
                    var _findLevelNew = ListItems.Any(i => i.IsDeleted == false && i.Year == _year && i.Month == _month && i.TopLevel == entity.TopLevel);
                    if (_findLevelNew)
                    {
                        var Result = ResultOutputData.GenearetResultOutputLevelIsExists();
                        return Result;
                    }
                }

                var _findLevel = ListItems.Any(i => i.IsDeleted == false && i.Year == _year && i.Month == _month && i.TopLevel == entity.TopLevel);
                if (_findLevel)
                {
                    var Result = ResultOutputData.GenearetResultOutputLevelIsExists();
                    return Result;
                }

                var _findUser = await _context.KafoEmployeeOfTheMonth.AnyAsync(i => i.IsDeleted == false && i.Year == _year && i.Month == _month && i.TopLevel != entity.TopLevel && i.UserId == entity.UserId);

                if (_findUser)
                {
                    var Result = ResultOutputData.GenearetResultOutputIsRegester();
                    return Result;
                }

                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.Year = _year;
                entity.Month = _month;

                var _model = _EmployeeOfTheMonth_Assembler.WriteDal(entity);
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
                var spec = Specification<KafoEmployeeOfTheMonth>.All.And(new KafoEmployeeOfTheMonthSpecification(_filter));

                var EmployeeOfTheMonth = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                var _EmployeeOfTheMonth = _EmployeeOfTheMonth_Assembler.WriteListDto(EmployeeOfTheMonth);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_EmployeeOfTheMonth, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetEmployeesOfMonthByDate(KafoEmployeeOfTheMonthParams employeeOfTheMonth)
        {
            try
            {
                var _year = Dates.ConvertStringToDate(employeeOfTheMonth.date).Value.Date.Year;
                var _month = Dates.ConvertStringToDate(employeeOfTheMonth.date).Value.Date.Month;
                if (employeeOfTheMonth.isMobile == true)
                    _month = _month - 1;

                string[] stringArray = { "User" };

                var entities = await FindAllAsync(i => i.IsDeleted == false && i.Month == _month.ToString() && i.Year == _year.ToString(), stringArray);
                var _entities = _EmployeeOfTheMonth_Assembler.WriteListDto(entities.OrderBy(o => o.TopLevel).ToList());

                var count = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }



        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {
                string[] stringArray = { "User" };

                var entities = await FindAllAsync(i => i.IsDeleted == false, stringArray);
                var _entities = _EmployeeOfTheMonth_Assembler.WriteListDto(entities);
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
                var EmployeeOfTheMonth = await GetByIdAsync(id);
                if (EmployeeOfTheMonth != null)
                {
                    var _EmployeeOfTheMonth = _EmployeeOfTheMonth_Assembler.WriteDto(EmployeeOfTheMonth);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_EmployeeOfTheMonth, 1);
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

        public async Task<OperationOutput> UpdateEntityList(List<KafoEmployeeOfTheMonthDto> entityList)
        {
            try
            {
                foreach (var entity in entityList)
                {
                    entity.Year = Dates.ConvertStringToDate(entity.Year).Value.Date.Year.ToString();
                    entity.Month = Dates.ConvertStringToDate(entity.Month).Value.Date.Month.ToString();

                    var _entity = await FindAsync(i => i.Id == entity.Id);
                    var _entityOnUpdate = await FindAsync(i => i.IsDeleted == false && i.Id == entity.Id && i.Year == entity.Year && entity.Month == entity.Month);

                    if (!string.IsNullOrEmpty(entity.TopLevelNew) && _entityOnUpdate != null)
                    {
                        _entityOnUpdate.TopLevel = _entity.TopLevel;
                        Update(_entityOnUpdate);
                    }

                    if (_entity is not null)
                    {
                        _entity.IsActive = true;
                        _entity.IsDeleted = false;
                        _entity.Year = entity.Year;
                        _entity.Month = entity.Month;
                        _entity.TopLevel = entity.TopLevelNew;
                        _entity.UserId = entity.UserId;

                        var _model = Update(_entity);

                    }
                }

                await SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;

            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }




        }

        public async Task<OperationOutput> UpdateEntity(KafoEmployeeOfTheMonthDto entity)
        {
            try
            {
                var lastRecordyYearMonth = new EmployeeOfTheMonth();

                if ((bool)entity.IsYearChanged)
                    entity.Year = Dates.ConvertStringToDate(entity.Year).Value.Date.Year.ToString();

                if ((bool)entity.IsMonthChanged)
                {

                    entity.Month = Dates.ConvertStringToDate(entity.Month).Value.Date.Month.ToString();

                    var ListItems = await _context.KafoEmployeeOfTheMonth.Where(i => i.IsDeleted == false && i.Year == entity.Year && i.Month == entity.Month).ToListAsync();
                    if (ListItems.Count == 5)
                    {
                        var Result = ResultOutputData.GenearetResultOutputLimit();
                        return Result;
                    }

                    var IsTopLevelIsFound = ListItems.FirstOrDefault(i => i.IsDeleted == false && i.TopLevel == entity.TopLevelNew && i.Year == entity.Year && i.Month == entity.Month);

                    if (IsTopLevelIsFound != null)
                    {
                        var Result = ResultOutputData.GenearetResultOutputLevelIsExists();
                        return Result;
                    }

                }

                var _entityOld = await FindAsync(i => i.IsDeleted == false && i.Id == entity.Id);

                var _entityLevelFound = await FindAsync(i => i.IsDeleted == false && i.TopLevel == entity.TopLevelNew && i.Year == entity.Year && i.Month == entity.Month);
                if (!string.IsNullOrEmpty(entity.TopLevelNew) && _entityLevelFound == null)
                    _entityOld.TopLevel = entity.TopLevelNew;


                if (!string.IsNullOrEmpty(entity.TopLevelNew) && _entityLevelFound != null)
                {

                    _entityLevelFound.TopLevel = _entityOld.TopLevel;
                    Update(_entityLevelFound);

                    _entityOld.TopLevel = entity.TopLevelNew;
                    Update(_entityOld);


                }
                if (_entityOld is not null)
                {
                    _entityOld.IsActive = true;
                    _entityOld.IsDeleted = false;
                    _entityOld.Year = entity.IsMonthChanged == false ? _entityOld.Year : entity.Year;
                    _entityOld.Month = entity.Month;
                    _entityOld.UserId = entity.UserId;

                    var _model = Update(_entityOld);
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

        public Task<OperationOutput> UpdateEntityAsync(EmployeeOfTheMonthDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<EmployeeOfTheMonthDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<EmployeeOfTheMonthDto> entities)
        {
            throw new NotImplementedException();
        }
    }
}
