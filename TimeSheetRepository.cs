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
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;

namespace Nupco.EF.Repositories
{
    public class TimeSheetRepository : BaseRepository<TimeSheet>, ITimeSheetRepository
    {
        private readonly TimeSheet_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "TimeSheetData";


        public TimeSheetRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new TimeSheet_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;


        }

        public async Task<OperationOutput> CreateNewTimeSheet(TimeSheetDto entityDto)
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

        public async Task<OperationOutput> AddNewAsync(TimeSheetDto entityDto)
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

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<TimeSheetDto> entitiesDto)
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
                var TimeSheets = await FindAllAsync(i => i.IsDeleted == false);
                var _TimeSheets = _Assembler.WriteListDto(TimeSheets);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_TimeSheets, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(TimeSheetDto entityDto)
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

        public async Task<OperationOutput> UpdateEntityAsync(TimeSheetDto entityDto, object key)
        {
            try
            {
                var _TimeSheet = await GetByIdAsync((int)entityDto.Id);


                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _TimeSheet);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<TimeSheetDto> entities)
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
                var TimeSheets = new List<TimeSheet>();

                var _TimeSheetList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                TimeSheets = _TimeSheetList.Where(i => i.IsDeleted == false).ToList();

                var _TimeSheetsDto = _Assembler.WriteListDto(TimeSheets);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_TimeSheetsDto, counts);
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

                var _TimeSheet = await FindAsync(f => f.Id == id, stringArray);
                if (_TimeSheet is not null)
                {
                    var _entity = _Assembler.WriteDto(_TimeSheet);
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
        public async Task<OperationOutput> GetCurrentYearTimeSheets(int companyEmployeeId)
        {
            try
            {
                var currentYear = DateTime.Now.Year;

                var timeSheets =  _context.TimeSheets
                    .Where(t => t.CompanyEmployeeId == companyEmployeeId &&
                               t.Year == currentYear &&
                               t.IsDeleted == false)
                    .Include(t => t.Entries)
                    .OrderBy(t => t.Month)
                    .AsEnumerable()
                    .Select(t => new
                    {
                        TimeSheetId = t.Id,
                        Year = t.Year,
                        Month = t.Month,
                        TotalEntries = t.Entries.Count,
                        WorkingDays = t.Entries.Count(e => e.Type == DayType.WorkingDay),
                        AnnualLeaveDays = t.Entries.Count(e => e.Type == DayType.Annual),
                        SickLeaveDays = t.Entries.Count(e => e.Type == DayType.Sick),
                        OverTime = t.Entries.Sum(e => e.OverTimeInHours),

                        Holidays = t.Entries.Count(e => e.Type == DayType.Holiday && (e.Date.Value.DayOfWeek != DayOfWeek.Friday && e.Date.Value.DayOfWeek != DayOfWeek.Saturday)),
                        weekEnds = t.Entries.Count(e => e.Type == DayType.Holiday && (e.Date.Value.DayOfWeek == DayOfWeek.Friday || e.Date.Value.DayOfWeek == DayOfWeek.Saturday)),
                        CreatedDate = t.CreatedDate,
                        IsApproved = t.IsApproved,
                        Entries = t.Entries.OrderBy(e => e.Date).Select(e => new
                        {
                            Date = e.Date,
                            DayType = e.Type.ToString(),
                            DayName = e.Date.Value.DayOfWeek.ToString(),
                            IsWeekend = e.Date.Value.DayOfWeek == DayOfWeek.Saturday ||
                                       e.Date.Value.DayOfWeek == DayOfWeek.Sunday
                        })
                    });

                if (!timeSheets.Any())
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(timeSheets, timeSheets.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current year timesheets");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetLastMonthTimeSheet(int companyEmployeeId)
        {
            try
            {
                var lastMonth = DateTime.Now.AddMonths(-1);
                var year = lastMonth.Year;
                var month = lastMonth.Month;

                var timeSheet = _context.TimeSheets
                    .Where(t => t.CompanyEmployeeId == companyEmployeeId &&
                                t.Year == year &&
                                t.Month == month &&
                                t.IsDeleted == false)
                    .Include(t => t.Entries)
                                        .AsEnumerable()
                    .Select(t => new
                    {
                        TimeSheetId = t.Id,
                        Year = t.Year,
                        Month = t.Month,
                        TotalEntries = t.Entries.Count,
                        WorkingDays = t.Entries.Count(e => e.Type == DayType.WorkingDay),
                        AnnualLeaveDays = t.Entries.Count(e => e.Type == DayType.Annual),
                        SickLeaveDays = t.Entries.Count(e => e.Type == DayType.Sick),
                        Holidays = t.Entries.Count(e => e.Type == DayType.Holiday && (e.Date.Value.DayOfWeek != DayOfWeek.Friday && e.Date.Value.DayOfWeek != DayOfWeek.Saturday)),
                        weekEnds = t.Entries.Count(e => e.Type == DayType.Holiday && (e.Date.Value.DayOfWeek == DayOfWeek.Friday || e.Date.Value.DayOfWeek == DayOfWeek.Saturday)),
                        CreatedDate = t.CreatedDate,
                        LastModified = t.UpdatedDate ?? t.CreatedDate,
                        IsApproved = t.IsApproved,
                        Entries = t.Entries.OrderBy(e => e.Date).Select(e => new
                        {
                            EntryId = e.Id,
                            Date = e.Date,
                            DayType = e.Type.ToString(),
                            DayName = e.Date.Value.DayOfWeek.ToString(),
                            IsWeekend = e.Date.Value.DayOfWeek == DayOfWeek.Saturday ||
                                       e.Date.Value.DayOfWeek == DayOfWeek.Sunday,
                            CreatedDate = e.CreatedDate,
                            LastModified = e.UpdatedDate ?? e.CreatedDate
                        })
                    });

                if (timeSheet == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(timeSheet, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last month timesheet");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetCurrentYearSummary(int companyEmployeeId)
        {
            try
            {
                var currentYear = DateTime.Now.Year;

                var vacation = await _context.VacationBalances
                    .FirstOrDefaultAsync(v => v.CompanyEmployeeId == companyEmployeeId &&
                    DateTime.Now >= v.ContractStartDate && DateTime.Now <= v.ContractEndDate && !v.IsDeleted && v.IsActive);

                List<TimeSheet> timeSheets;

                if (vacation?.ContractStartDate != null && vacation?.ContractEndDate != null)
                {
                    DateTime start = vacation.ContractStartDate.Value.Date;
                    DateTime end = vacation.ContractEndDate.Value.Date;

                    timeSheets = _context.TimeSheets
                        .Where(t => t.CompanyEmployeeId == companyEmployeeId &&
                                    t.IsDeleted == false &&
                                    t.IsActive == true)
                        .Include(t => t.Entries)
                        .AsEnumerable() 
                        .Where(t => {
                            var firstOfMonth = new DateTime((int)t.Year, (int)t.Month, 1);
                            return firstOfMonth >= start && firstOfMonth <= end;
                        }).ToList();
                }
                else
                {
                    timeSheets = await _context.TimeSheets
                        .Where(t => t.CompanyEmployeeId == companyEmployeeId &&
                                    t.Year == currentYear &&
                                    t.IsDeleted == false &&
                                    t.IsActive == true)
                        .Include(t => t.Entries)
                        .ToListAsync();
                }


                var summary = new
                {
                    TotalWorkingDays = timeSheets.Sum(t => t.Entries?.Count(e => e.Type == DayType.WorkingDay)) ?? 0,
                    TotalAnnualLeaveDays = timeSheets.Sum(t => t.Entries?.Count(e => e.Type == DayType.Annual)) ?? 0,
                    TotalSickLeaveDays = timeSheets.Sum(t => t.Entries?.Count(e => e.Type == DayType.Sick)) ?? 0,
                    TotalHolidays = timeSheets.Sum(t => t.Entries?.Count(e => e.Type == DayType.Holiday && (e.Date.Value.DayOfWeek != DayOfWeek.Friday && e.Date.Value.DayOfWeek != DayOfWeek.Saturday)) ) ?? 0,
                    TotalWeekEnds = timeSheets.Sum(t => t.Entries?.Count(e => e.Type == DayType.Holiday && (e.Date.Value.DayOfWeek == DayOfWeek.Friday || e.Date.Value.DayOfWeek == DayOfWeek.Saturday))) ?? 0,
                    MonthlyBreakdown = timeSheets
                        .OrderBy(t => t.Month)
                        .AsEnumerable()
                        .Select(t => new
                        {
                            Month = t.Month,
                            Year = t.Year,
                            WorkingDays = t.Entries?.Count(e => e.Type == DayType.WorkingDay) ?? 0,
                            AnnualLeaveDays = t.Entries?.Count(e => e.Type == DayType.Annual) ?? 0,
                            SickLeaveDays = t.Entries?.Count(e => e.Type == DayType.Sick) ?? 0,
                            Holidays = t.Entries?.Count(e => e.Type == DayType.Holiday && (e.Date.Value.DayOfWeek != DayOfWeek.Friday && e.Date.Value.DayOfWeek != DayOfWeek.Saturday)) ?? 0,
                            weekEnds = t.Entries?.Count(e => e.Type == DayType.Holiday && (e.Date.Value.DayOfWeek == DayOfWeek.Friday || e.Date.Value.DayOfWeek == DayOfWeek.Saturday)) ?? 0,
                        })
                };

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(summary, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current year timesheet summary");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetTimeSheetByMonth(int companyEmployeeId, int year, int month)
        {
            try
            {
                var timeSheet = await _context.TimeSheets
                    .Where(t => t.CompanyEmployeeId == companyEmployeeId &&
                               t.Year == year &&
                               t.Month == month &&
                               t.IsDeleted == false)
                    .Include(t => t.Entries)
                    .FirstOrDefaultAsync();

                if (timeSheet == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                var timeSheetDto = _Assembler.WriteDto(timeSheet);

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(timeSheetDto, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timesheet by month");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
    }
}
