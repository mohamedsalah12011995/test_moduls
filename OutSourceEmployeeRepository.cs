using ClosedXML.Excel;
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
using Nupco.DAL.Models.OutSourceEmployees;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Bibliography;
using Nupco.DAL.Models.Users;


namespace Nupco.EF.Repositories
{
    public class OutSourceEmployeeRepository : BaseRepository<OutSourceEmployee>, IOutSourceEmployeeRepository
    {
        private readonly OutSourceEmployee_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "OutSourceEmployeeData";

        public object Result { get; private set; }

        public OutSourceEmployeeRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new OutSourceEmployee_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;


        }

        public async Task<OperationOutput> CreateNewOutSourceEmployee(OutSourceEmployeeDto entityDto)
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

        public async Task<OperationOutput> AddNewAsync(OutSourceEmployeeDto entityDto)
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

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<OutSourceEmployeeDto> entitiesDto)
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
                var OutSourceEmployees = await FindAllAsync(i => i.IsDeleted == false);
                var _OutSourceEmployees = _Assembler.WriteListDto(OutSourceEmployees);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_OutSourceEmployees, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(OutSourceEmployeeDto entityDto)
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

        public async Task<OperationOutput> UpdateEntityAsync(OutSourceEmployeeDto entityDto, object key)
        {
            try
            {
                var _OutSourceEmployee = await GetByIdAsync((int)entityDto.Id);


                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _OutSourceEmployee);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<OutSourceEmployeeDto> entities)
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
                var OutSourceEmployees = new List<OutSourceEmployee>();

                var _OutSourceEmployeeList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                OutSourceEmployees = _OutSourceEmployeeList.Where(i => i.IsDeleted == false).ToList();

                var _OutSourceEmployeesDto = _Assembler.WriteListDto(OutSourceEmployees);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_OutSourceEmployeesDto, counts);
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

                var _OutSourceEmployee = await FindAsync(f => f.Id == id, stringArray);
                if (_OutSourceEmployee is not null)
                {
                    var _entity = _Assembler.WriteDto(_OutSourceEmployee);
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


        public async Task<OperationOutput> GetEmployeeLeaveSummary(int employeeId)
        {
            try
            {
              

                var employee = await FindAsync(e => e.Id == employeeId, new[] { "VacationBalance", "LeaveRequests" });



                if (employee == null)
{
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
                var currentYear = DateTime.Now.Year;

                var summary = new { };
                //{
                //    AnnualBalance = employee.VacationBalance.AnnualBalance ?? 0,
                //    SickBalance = employee.VacationBalance?.SickBalance ?? 0,
                //    OvertimeHours = employee.VacationBalance?.OvertimeHours ?? 0,
                //    ApprovedAnnualLeave = employee.LeaveRequests?
                //        .Where(l => l.Type == LeaveType.Annual && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                //        .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0,
                //    ApprovedSickLeave = employee.LeaveRequests?
                //        .Where(l => l.Type == LeaveType.Sick && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                //        .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0,
                //    PendingRequests = employee.LeaveRequests?
                //        .Count(l => l.OverallStatus == "Pending") ?? 0
                //};
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(summary, 1);
                return _result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetEmployeeAbsences(int employeeId, int year)
        {
            try
            {
                var employee = await _context.OutSourceEmployees
                //.Include(e => e.TimeSheets)
                //    .ThenInclude(t => t.Entries)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employee == null)
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
                var absences = employee.TimeSheets?
                    .SelectMany(t => t.Entries)
                    .Where(e => e.Date?.Year == year && e.Type == DayType.WorkingDay && e.Date < DateTime.Today)
                    .GroupBy(e => e.Date?.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        AbsenceDays = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(absences, 1);
                return _result;

            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetPendingLeaveRequests()
        {
            try
            {
            //    var pendingRequests = await  _context.LeaveRequests.Where(
        //            l => l.Status == "Pending").Include(l => l.Employee).ToListAsync();        
                var pendingRequests = await  _context.LeaveRequests.ToListAsync();


                var result = pendingRequests
                    .GroupBy(l => l.Type)
                    .Select(g => new
                    {
                        LeaveType = g.Key.ToString(),
                        Count = g.Count(),
                        Requests = g.Select(r => new
                        {
                            r.Id,
                            EmployeeName = r.CompanyEmployee?.FullName,
                            r.StartDate,
                            r.EndDate,
                            r.Reason
                        })
                    })
                    .ToList();



                ResultOutputData resultt = new ResultOutputData();
                var _result = resultt.GenearetResultOutput(result, 1);
                return _result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetDepartmentLeaveSummary(string department)
        {
            try
            {
                var employees = await FindAllAsync(
                e => e.Department == department, new[] { "VacationBalance" , "LeaveRequests" } );


                var currentYear = DateTime.Now.Year;

                var summary = new { };
                //employees.Select(e => new
                //{
                //    EmployeeId = e.Id,
                //    EmployeeName = e.FullName,
                //    AnnualBalance = e.VacationBalance?.AnnualBalance ?? 0,
                //    SickBalance = e.VacationBalance?.SickBalance ?? 0,
                //    UsedAnnual = e.LeaveRequests?
                //        .Where(l => l.Type == LeaveType.Annual && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                //        .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0,
                //    UsedSick = e.LeaveRequests?
                //        .Where(l => l.Type == LeaveType.Sick && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                //        .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0,
                //    RemainingAnnual = (e.VacationBalance?.AnnualBalance ?? 0) -
                //        (e.LeaveRequests?
                //            .Where(l => l.Type == LeaveType.Annual && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                //            .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0),
                //    PendingRequests = e.LeaveRequests?
                //        .Count(l => l.OverallStatus == "Pending") ?? 0
                //}).ToList();


                ResultOutputData resultt = new ResultOutputData();
                var _result = resultt.GenearetResultOutput(summary, 1);
                return _result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateVacationBalance(int employeeId, int? annual = null, int? sick = null, int? overtime = null)
        {
            try
            {
                var balance =  _context.VacationBalances.FirstOrDefault(b => b.CompanyEmployeeId == employeeId);

                if (balance == null)
                {
                    balance = new VacationBalance { CompanyEmployeeId = employeeId };
                    await _context.VacationBalances.AddAsync(balance);
                }

                if (annual.HasValue) balance.AnnualBalance = annual.Value;
                if (sick.HasValue) balance.SickBalance = sick.Value;
                if (overtime.HasValue) balance.OvertimeHours = overtime.Value;

                balance.UpdatedDate = DateTime.Now;
                _context.VacationBalances.Update(balance);
                await SaveChangesAsync();


                ResultOutputData resultt = new ResultOutputData();
                var _result = resultt.GenearetResultOutput("success", 1);
                return _result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        // Additional useful methods

        public async Task<OperationOutput> GetEmployeeTimeSheetSummary(int employeeId, int year, int month)
        {
            try
            {
                //            var timeSheet =  _context.TimeSheets.Where(
                //t => t.EmployeeId == employeeId && t.Year == year && t.Month == month).Include(t => t.Entries).FirstOrDefault();

                var timeSheet = _context.TimeSheets.Where(
    t => t.EmployeeId == employeeId && t.Year == year && t.Month == month).FirstOrDefault();
                if (timeSheet == null)
{
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
                var summary = new
                {
                    WorkingDays = timeSheet.Entries?.Count(e => e.Type == DayType.WorkingDay) ?? 0,
                    AnnualLeaveDays = timeSheet.Entries?.Count(e => e.Type == DayType.Annual) ?? 0,
                    SickLeaveDays = timeSheet.Entries?.Count(e => e.Type == DayType.Sick) ?? 0,
                    Holidays = timeSheet.Entries?.Count(e => e.Type == DayType.Holiday) ?? 0
                };


                ResultOutputData resultt = new ResultOutputData();
                var _result = resultt.GenearetResultOutput(summary, 1);
                return _result;
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        //public async Task<OperationOutput> ProcessEmployeeTimeSheet(Guid userId, int month, int year, System.Data.DataTable excelData)
        //{
        //    try
        //    {
        //        var user = await _context.Users.FindAsync(userId);
        //        if (user == null)
        //        {
        //            return ResultOutputData.GenearetResultOutputNoDataReturned();
        //        }

        //        var employee = await _context.OutSourceEmployees.FirstOrDefaultAsync(e => e.UserId == userId);
        //        if (employee == null)
        //        {
        //            employee = new OutSourceEmployee
        //            {
        //                UserId = userId,
        //                FullName = user.FirstName+" "+user.LastName,
        //                Email = user.Email,
        //                IsActive = true,
        //                IsDeleted = false,
        //                CreatedDate = DateTime.Now
        //            };
        //            await _context.OutSourceEmployees.AddAsync(employee);
        //            await _context.SaveChangesAsync();
        //        }

        //        var timeSheet = new TimeSheet
        //        {
        //            EmployeeId = employee.Id,
        //            Year = year,
        //            Month = month,
        //            IsActive = true,
        //            IsDeleted = false,
        //            CreatedDate = DateTime.Now
        //        };
        //        await _context.TimeSheets.AddAsync(timeSheet);
        //        await _context.SaveChangesAsync();

        //        var entries = new List<TimeSheetEntry>();
        //        foreach (DataRow row in excelData.Rows)
        //        {
        //            if (DateTime.TryParse(row["Date"].ToString(), out var entryDate) &&
        //                Enum.TryParse<DayType>(row["DayType"].ToString(), true, out var dayType))
        //            {
        //                entries.Add(new TimeSheetEntry
        //                {
        //                    TimeSheetId = timeSheet.Id,
        //                    Date = entryDate,
        //                    Type = dayType,
        //                    IsActive = true,
        //                    IsDeleted = false,
        //                    CreatedDate = DateTime.Now
        //                });
        //            }
        //        }

        //        await _context.TimeSheetEntries.AddRangeAsync(entries);
        //        await _context.SaveChangesAsync();

        //        var result = new
        //        {
        //            EmployeeId = employee.Id,
        //            TimeSheetId = timeSheet.Id,
        //            EntriesCount = entries.Count
        //        };

        //        ResultOutputData resultOutput = new ResultOutputData();
        //        return resultOutput.GenearetResultOutput(result, entries.Count);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing timesheet for user {UserId}", userId);
        //        return ResultOutputData.GenearetResultOutputCatch();
        //    }
        //}

        public async Task<OperationOutput> ProcessEmployeeTimeSheet(TimeSheetBulkUploadDto bulkUploadDto, string userId,
      int? annualBalance = 21, int? sickBalance = 5)
        {
            try
            {
                // Validate input file
                var validationResult = ValidateUploadFile(bulkUploadDto);
                if (validationResult != null) return validationResult;

                // Process user and employee
                var (user, employee) = await ProcessUserAndEmployee(userId, annualBalance, sickBalance);
                if (user == null) return ResultOutputData.GenearetResultOutputNoDataReturned();

                // Process Excel file to get month/year and entries
                using var stream = new MemoryStream();
                await bulkUploadDto.File.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);
                {
                    if (workbook.Worksheets.Count == 0)
                        return ResultOutputData.GenearetResultOutputNoDataReturned();

                    var worksheet = workbook.Worksheet(1);

                    // Find first data row with dates (column B)
                    int startRow = FindFirstDateRow(worksheet);
                    if (startRow == -1)
                        return ResultOutputData.GenearetResultOutputNoDataReturned();

                    // Detect month and year from the actual date entries
                    var (month, year) = DetectMonthAndYearFromEntries(worksheet, startRow);

                    // Get or create timesheet
                    var timeSheet = await GetOrCreateTimeSheet(employee.Id, bulkUploadDto.TimeSheetId, month, year);
                    if (timeSheet == null) return ResultOutputData.GenearetResultOutputCatch();

                    // Process Excel entries - updates existing or adds new ones
                    var (entries, errorCount, errorMessages) = await ProcessExcelEntries(worksheet, timeSheet.Id, startRow);
                    if (entries.Count == 0) return ResultOutputData.GenearetResultOutputNoDataReturned();

                    // Save entries and return result
                    return await SaveAndReturnResult(entries, errorCount, errorMessages, employee.Id, timeSheet.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing timesheet for user {UserId}", userId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        private int FindFirstDateRow(IXLWorksheet worksheet)
        {
            // Search for the first row with a valid date in column B
            for (int row = 1; row <= 20; row++) // Check first 20 rows
            {
                var dateCell = worksheet.Cell($"C{row}");
                if (!dateCell.IsEmpty() && DateTime.TryParse(dateCell.GetValue<string>(), out _))
                {
                    return row;
                }
            }
            return -1; // Not found
        }

        private (int month, int year) DetectMonthAndYearFromEntries(IXLWorksheet worksheet, int startRow)
        {
            // Scan through the first few date entries to find a valid date
            for (int row = startRow; row <= startRow + 5; row++) // Check first 5 rows after start
            {
                var dateCell = worksheet.Cell($"C{row}");
                if (!dateCell.IsEmpty())
                {
                    if (DateTime.TryParse(dateCell.GetValue<string>(), out DateTime date))
                    {
                        return (date.Month, date.Year);
                    }
                }
            }

            // Fallback to current month/year if no dates found
            var currentDate = DateTime.Now;
            return (currentDate.Month, currentDate.Year);
        }

        private async Task<TimeSheet?> GetOrCreateTimeSheet(int employeeId, int timeSheetId, int month, int year)
        {
            // Check if timesheet already exists for this employee/month/year
            var existingTimeSheet = await _context.TimeSheets
                .FirstOrDefaultAsync(t => t.EmployeeId == employeeId &&
                                        t.Month == month &&
                                        t.Year == year &&
                                        t.Id != timeSheetId); // Exclude current timesheet if updating

            if (existingTimeSheet != null)
            {
                // If we were trying to create a new timesheet but one exists, return the existing one
                if (timeSheetId <= 0)
                {
                    return existingTimeSheet;
                }

                // If we were trying to update a specific timesheet but another one exists for this period,
                // we should probably handle this as an error case
                if (timeSheetId > 0 && existingTimeSheet.Id != timeSheetId)
                {
                    return null;
                }
            }

            // If TimeSheetId is provided, get that timesheet
            if (timeSheetId > 0)
            {
                var timeSheet = await _context.TimeSheets.FindAsync(timeSheetId);
                if (timeSheet != null)
                {
                    // Update month and year if needed
                    timeSheet.Month = month;
                    timeSheet.Year = year;
                    await _context.SaveChangesAsync();
                    return timeSheet;
                }
            }

            // Create new timesheet
            var newTimeSheet = new TimeSheet
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.Now
            };
            await _context.TimeSheets.AddAsync(newTimeSheet);
            await _context.SaveChangesAsync();
            return newTimeSheet;
        }

        private async Task<(List<TimeSheetEntry> entries, int errorCount, List<string> errorMessages)>
            ProcessExcelEntries(IXLWorksheet worksheet, int timeSheetId, int startRow)
        {
            var entries = new List<TimeSheetEntry>();
            int errorCount = 0;
            var errorMessages = new List<string>();

            int endRow = worksheet.LastRowUsed().RowNumber() - 5;

            // Get existing entries for this timesheet to avoid duplicates
            var existingEntries = await _context.TimeSheetEntries
                .Where(e => e.TimeSheetId == timeSheetId)
                .ToListAsync();

            // Column mappings
            const string dateColumn = "C";
            const string workModeColumn = "D";

            for (int row = startRow; row <= endRow; row++)
            {
                try
                {
                    var dateCell = worksheet.Cell($"{dateColumn}{row}");
                    var workModeCell = worksheet.Cell($"{workModeColumn}{row}");

                    // Skip if date cell is empty
                    if (dateCell.IsEmpty())
                        continue;

                    // Parse date
                    if (!DateTime.TryParse(dateCell.GetValue<string>(), out DateTime date))
                    {
                        errorCount++;
                        errorMessages.Add($"Row {row}: Invalid date format");
                        continue;
                    }

                    // Get work mode (default to WorkingDay if empty)
                    var workMode = workModeCell.IsEmpty() ? "Remotely" : workModeCell.GetValue<string>()?.Trim();

                    // Check if entry already exists for this date
                    var existingEntry = existingEntries.FirstOrDefault(e => e.Date == date.Date);

                    if (existingEntry != null)
                    {
                        // Update existing entry
                        existingEntry.Type = workMode switch
                        {
                            "Remotely" => DayType.WorkingDay,
                            "Annual Leave" => DayType.Annual,
                            "Weekend" => DayType.Holiday,
                            "Sick Leave" => DayType.Sick,
                            "Public Holiday" => DayType.Holiday,
                            _ => DayType.WorkingDay
                        };
                        existingEntry.UpdatedDate = DateTime.Now;
                        entries.Add(existingEntry);
                    }
                    else
                    {
                        // Create new entry
                        var newEntry = new TimeSheetEntry
                        {
                            TimeSheetId = timeSheetId,
                            Date = date,
                            Type = workMode switch
                            {
                                "Remotely" => DayType.WorkingDay,
                                "Annual Leave" => DayType.Annual,
                                "Weekend" => DayType.Holiday,
                                "Sick Leave" => DayType.Sick,
                                "Public Holiday" => DayType.Holiday,
                                _ => DayType.WorkingDay
                            },
                            CreatedDate = DateTime.Now,
                            IsActive = true,
                            IsDeleted = false
                        };
                        entries.Add(newEntry);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errorMessages.Add($"Row {row}: Error processing - {ex.Message}");
                }
            }

            return (entries, errorCount, errorMessages);
        }

        private async Task<OperationOutput> SaveAndReturnResult(
            List<TimeSheetEntry> entries, int errorCount, List<string> errorMessages,
            int employeeId, int timeSheetId)
        {

            var newEntries = entries.Where(e => e.Id == 0).ToList(); // Assuming Id is 0 for new
            await _context.TimeSheetEntries.AddRangeAsync(newEntries);
            await _context.SaveChangesAsync();
            // We don't need to AddRangeAsync here because:
            // - New entries will be added automatically when we save changes
            // - Existing entries are already tracked by EF Core

            var result = new
            {
                EmployeeId = employeeId,
                TimeSheetId = timeSheetId,
                EntriesCount = entries.Count,
                UpdatedEntries = entries.Count(e => e.UpdatedDate != null),
                NewEntries = entries.Count(e => e.UpdatedDate == null),
                ErrorCount = errorCount,
                Errors = errorCount > 0 ? errorMessages.Take(10).ToList() : null
            };

            ResultOutputData resultOutput = new ResultOutputData();
            return resultOutput.GenearetResultOutput(result, entries.Count);
        }
        private OperationOutput? ValidateUploadFile(TimeSheetBulkUploadDto bulkUploadDto)
        {
            if (bulkUploadDto.File == null || bulkUploadDto.File.Length == 0)
                return ResultOutputData.GenearetResultOutputNoDataReturned();

            var fileExtension = Path.GetExtension(bulkUploadDto.File.FileName).ToLower();
            if (fileExtension != ".xlsx" && fileExtension != ".xls")
                return ResultOutputData.GenearetResultOutputNoDataReturned();

            return null;
        }


        private async Task<(User? user, OutSourceEmployee? employee)> ProcessUserAndEmployee(
      string userId, int? annualBalance, int? sickBalance)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (null, null);

            var employee = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
     .Include(_context.OutSourceEmployees, e => e.VacationBalance)
     .FirstOrDefaultAsync(e => e.UserId == userId);


            if (employee == null)
            {
                employee = CreateNewEmployee(user, annualBalance, sickBalance);
                await _context.OutSourceEmployees.AddAsync(employee);
                await _context.SaveChangesAsync();
            }
            else if (employee.VacationBalance == null)
            {
                employee.VacationBalance = CreateVacationBalance(annualBalance, sickBalance);
                _context.OutSourceEmployees.Update(employee);
                await _context.SaveChangesAsync();
            }

            return (user, employee);
        }
        private OutSourceEmployee CreateNewEmployee(User user, int? annualBalance, int? sickBalance)
        {
            return new OutSourceEmployee
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                UserCode = user.Code,
                Department = user.DepartmentName,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.Now,
                VacationBalance = CreateVacationBalance(annualBalance, sickBalance)
            };
        }

        private VacationBalance CreateVacationBalance(int? annualBalance, int? sickBalance)
        {
            return new VacationBalance
            {
                AnnualBalance = annualBalance ?? 21,
                SickBalance = sickBalance ?? 5,
                OvertimeHours = 0,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.Now
            };
        }
        // ... (Keep other helper methods the same)
        // ... (Keep the existing ProcessUserAndEmployee, CreateNewEmployee, CreateVacationBalance methods as they were)

        public async Task<OperationOutput> GetEmployeeTimesheetStatusOverview(DateTime startDate, DateTime endDate, string? department = null)
        {
            try
            {
                var query = _context.OutSourceEmployees
    .Include(e => e.TimeSheets)
    .ThenInclude(t => t.Entries)
                .AsQueryable();

                if (department != null)
                {
                    query = query.Where(e => e.Department == department);
                }

                var employees = await query.ToListAsync();


                var result = employees.Select(e => new
                {
                    EmployeeId = e.Id,
                    EmployeeName = e.FullName,
                    EmployeeCode = e.UserCode,
                    Department = e.Department,
                    Timesheets = e.TimeSheets?
                        .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                        .Select(t => new
                        {
                            TimeSheetId = t.Id,
                            Period = $"{t.Month}/{t.Year}",
                            TotalWorkingDays = t.Entries?.Count(entry => entry.Type == DayType.WorkingDay) ?? 0,
                            TotalLeaveDays = t.Entries?.Count(entry => entry.Type == DayType.Annual || entry.Type == DayType.Sick) ?? 0,
                            LastModified = t.UpdatedDate ?? t.CreatedDate
                        })
                        .OrderByDescending(t => t.LastModified)
                        .ToList()
                }).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(result, employees.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timesheet status overview");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetTimesheetComplianceReport(int month, int year, Guid? managerId = null)
        {
            try
            {
                var cutoffDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1); // End of month

                var query =
                           _context.OutSourceEmployees.Include(e => e.TimeSheets).Include(e => e.VacationBalance);

                // Filter by manager if specified
                if (managerId.HasValue)
                {
                    query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<OutSourceEmployee, VacationBalance?>)query.Where(e => e.LineManagerId == managerId);
                }

                var employees = await query.ToListAsync();

                var reportData = new
                {
                    ReportPeriod = $"{month}/{year}",
                    TotalEmployees = employees?.Count,
                    EmployeesWithTimesheets = employees.Count(e => e.TimeSheets?.Any(t => t.Month == month && t.Year == year) ?? false),
                    ComplianceStats = new
                    {
                        OnTimeSubmissions = employees.Count(e =>
                            e.TimeSheets?.Any(t =>
                                t.Month == month &&
                                t.Year == year &&
                                t.CreatedDate <= cutoffDate) ?? false),
                        LateSubmissions = employees.Count(e =>
                            e.TimeSheets?.Any(t =>
                                t.Month == month &&
                                t.Year == year &&
                                t.CreatedDate > cutoffDate) ?? false),
                        PendingApproval = employees.Count(e =>
                            e.TimeSheets?.Any(t =>
                                t.Month == month &&
                                t.Year == year ) ?? false),
                        MissingSubmissions = employees.Count(e =>
                            !e.TimeSheets?.Any(t =>
                                t.Month == month &&
                                t.Year == year) ?? true)
                    },
                    EmployeeDetails = employees.Select(e => new
                    {
                        e.Id,
                        e.FullName,
                        e.Department,
                        SubmissionDate = e.TimeSheets?
                            .FirstOrDefault(t => t.Month == month && t.Year == year)?.CreatedDate,
                        LeaveBalance = e.VacationBalance != null ? new
                        {
                            e.VacationBalance.AnnualBalance,
                            e.VacationBalance.SickBalance
                        } : null
                    })
                };

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(reportData, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating timesheet compliance report");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
    }
}
