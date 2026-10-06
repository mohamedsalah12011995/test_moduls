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
using Microsoft.EntityFrameworkCore; // Required for EF.Functions


namespace Nupco.EF.Repositories
{
    public class VacationBalanceRepository : BaseRepository<VacationBalance>, IVacationBalanceRepository
    {
        private readonly VacationBalance_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "VacationBalanceData";


        public VacationBalanceRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new VacationBalance_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;


        }

        public async Task<OperationOutput> CreateNewVacationBalance(VacationBalanceDto entityDto)
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

        public async Task<OperationOutput> AddNewAsync(VacationBalanceDto entityDto)
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

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<VacationBalanceDto> entitiesDto)
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
                var VacationBalances = await FindAllAsync(i => i.IsDeleted == false);
                var _VacationBalances = _Assembler.WriteListDto(VacationBalances);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_VacationBalances, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(VacationBalanceDto entityDto)
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

        public async Task<OperationOutput> UpdateEntityAsync(VacationBalanceDto entityDto, object key)
        {
            try
            {
                var _VacationBalance = await GetByIdAsync(entityDto.Id);


                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _VacationBalance);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<VacationBalanceDto> entities)
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
                var VacationBalances = new List<VacationBalance>();

                var _VacationBalanceList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                VacationBalances = _VacationBalanceList.Where(i => i.IsDeleted == false).ToList();

                var _VacationBalancesDto = _Assembler.WriteListDto(VacationBalances);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_VacationBalancesDto, counts);
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

                var _VacationBalance = await FindAsync(f => f.Id == id, stringArray);
                if (_VacationBalance is not null)
                {
                    var _entity = _Assembler.WriteDto(_VacationBalance);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_entity, 1);
                    return _result;
                }
                else
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned("");
                    return Result;
                }
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetVacationBalanceAsync(int employeeId, int year)
        {
            try
            {
                var vacationBalance = await _context.VacationBalances
    .FirstOrDefaultAsync(v => v.CompanyEmployeeId == employeeId &&
    DateTime.Now >= v.ContractStartDate && DateTime.Now <= v.ContractEndDate && !v.IsDeleted && v.IsActive);

                if (vacationBalance == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("");
                }

                var vacationBalanceDto = _Assembler.WriteDto(vacationBalance);

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(vacationBalanceDto, 1);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacation balance");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<OperationOutput> CreateVacationBalanceAdjustment(VacationBalanceAdjustmentDto adjustmentDto)
        {
            try
            {
                // Verify the vacation balance exists
                ResultOutputData resultOutputData = new ResultOutputData();

                var vacationBalance = await _context.VacationBalances.FindAsync(adjustmentDto.VacationBalanceId);
                if (vacationBalance == null)
                {
                    return resultOutputData.GenearetResultOutputWithEmptyData("Vacation balance not found",0);
                }

                // Create the adjustment
                var adjustment = new VacationBalanceAdjustment
                {
                    VacationBalanceId = adjustmentDto.VacationBalanceId,
                    AdjustmentAmount = adjustmentDto.AdjustmentAmount,
                    AdjustmentReason = adjustmentDto.AdjustmentReason,
                    AdjustedBy = adjustmentDto.AdjustedBy,
                    AdjustmentDate = DateTime.Now,
                    Status = "Approved" // Assuming auto-approval for this implementation
                };

                // Apply the adjustment to the balance
                if (adjustmentDto.BalanceType == BalanceType.Annual)
                {
                    vacationBalance.AnnualBalance += adjustmentDto.AdjustmentAmount;
                }
                else if (adjustmentDto.BalanceType == BalanceType.Sick)
                {
                    vacationBalance.SickBalance += adjustmentDto.AdjustmentAmount;
                }
                else if (adjustmentDto.BalanceType == BalanceType.Overtime)
                {
                    vacationBalance.OvertimeHours += adjustmentDto.AdjustmentAmount;
                }

                // Update last updated date
                vacationBalance.LastUpdatedDate = DateTime.Now;

                // Create history record
                var history = new VacationBalanceHistory
                {
                    CompanyEmployeeId = vacationBalance.CompanyEmployeeId,
                    ChangeInBalance = adjustmentDto.AdjustmentAmount,
                    AdjustmentReason = adjustmentDto.AdjustmentReason,
                    BalanceBeforeAdjustment = adjustmentDto.BalanceType == BalanceType.Annual ?
                        vacationBalance.AnnualBalance - adjustmentDto.AdjustmentAmount :
                        adjustmentDto.BalanceType == BalanceType.Sick ?
                            vacationBalance.SickBalance - adjustmentDto.AdjustmentAmount :
                            vacationBalance.OvertimeHours - adjustmentDto.AdjustmentAmount,
                    BalanceAfterAdjustment = adjustmentDto.BalanceType == BalanceType.Annual ?
                        vacationBalance.AnnualBalance :
                        adjustmentDto.BalanceType == BalanceType.Sick ?
                            vacationBalance.SickBalance :
                            vacationBalance.OvertimeHours,
                    AdjustedBy = adjustmentDto.AdjustedBy,
                    AdjustmentDate = DateTime.Now
                };

                // Save changes
                await _context.VacationBalanceAdjustments.AddAsync(adjustment);
                await _context.VacationBalanceHistories.AddAsync(history);
                _context.VacationBalances.Update(vacationBalance);
                await _context.SaveChangesAsync();

                // Clear relevant cache
                _cache.Remove(cacheKey);

                // Return success
                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vacation balance adjustment");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetVacationBalanceAdjustments(int vacationBalanceId)
        {
            try
            {
                var adjustments = await _context.VacationBalanceAdjustments
                    .Where(a => a.VacationBalanceId == vacationBalanceId)
                    .OrderByDescending(a => a.AdjustmentDate)
                    .ToListAsync();

                if (adjustments == null || !adjustments.Any())
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("No adjustments found");
                }

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(adjustments, adjustments.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacation balance adjustments");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetVacationBalanceHistory(int employeeId, int? year = null)
        {
            try
            {
                var query = _context.VacationBalanceHistories
                    .Where(h => h.CompanyEmployeeId == employeeId);

                if (year.HasValue)
                {
                    query = query.Where(h => h.AdjustmentDate.Year == year.Value);
                }

                var history = await query
                    .OrderByDescending(h => h.AdjustmentDate)
                    .ToListAsync();

                if (history == null || !history.Any())
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("No history found");
                }

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(history, history.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacation balance history");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

       

        public async Task<OperationOutput> InitializeYearlyVacationBalance(int employeeId, int year, string createdBy)
        {
            try
            {
                // Check if balance already exists for this year
                var existingBalance = await _context.VacationBalances
                    .FirstOrDefaultAsync(vb => vb.CompanyEmployeeId == employeeId && vb.Year == year);

                if (existingBalance != null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("Vacation balance already exists for this year");
                }

                // Get employee contract details
                var employee = await _context.CompanyEmployees
                    .Include(e => e.VacationBalance)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employee == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("Employee not found");
                }

                // Create new vacation balance
                var newBalance = new VacationBalance
                {
                    CompanyEmployeeId = employeeId,
                    Year = year,
                    ContractStartDate = employee.ContractStartDate,
                    ContractEndDate = employee.ContractEndDate,
                    CreatedBy = createdBy,
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    IsDeleted = false,
                    // Set initial balances based on business rules
                    AnnualBalance = 30, // Example: 30 days annual leave
                    SickBalance = 10,   // Example: 10 days sick leave
                    OvertimeHours = 0,
                    AvailableBalance = 30 // Initially same as annual balance
                };

                await _context.VacationBalances.AddAsync(newBalance);
                await _context.SaveChangesAsync();

                // Create history record for initialization
                var history = new VacationBalanceHistory
                {
                    CompanyEmployeeId = employeeId,
                    ChangeInBalance = 30, // Initial annual balance
                    AdjustmentReason = "Yearly vacation balance initialization",
                    BalanceBeforeAdjustment = 0,
                    BalanceAfterAdjustment = 30,
                    AdjustedBy = createdBy,
                    AdjustmentDate = DateTime.Now
                };

                await _context.VacationBalanceHistories.AddAsync(history);
                await _context.SaveChangesAsync();

                // Clear relevant cache
                _cache.Remove(cacheKey);

                // Return the new balance
                var vacationBalanceDto = _Assembler.WriteDto(newBalance);
                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(vacationBalanceDto, 1);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing yearly vacation balance");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        public async Task<OperationOutput> GetBalanceSummary(int employeeId)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var balance = await _context.VacationBalances
                    .FirstOrDefaultAsync(vb => vb.CompanyEmployeeId == employeeId && vb.Year == currentYear);

                if (balance == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("No balance record found");
                }
                var Types = _context.LeaveTypes.ToList();
                // Calculate used days from leave requests
                var usedAnnual = await _context.LeaveRequests
                    .Where(lr => lr.CompanyEmployeeId == employeeId &&
                                lr.RequestedOn.Year == currentYear &&
                                lr.OverallStatus == "Approved" &&
                                lr.Type == Types.Where(l => l.NameEn == "Annual"))
                    .SumAsync(lr => getdifference((DateTime)lr.StartDate, (DateTime)lr.EndDate));

                var usedSick = await _context.LeaveRequests
                    .Where(lr => lr.CompanyEmployeeId == employeeId &&
                                lr.RequestedOn.Year == currentYear &&
                                lr.OverallStatus == "Approved" &&
                                lr.Type == Types.Where(l => l.NameEn == "Sick"))
                    .SumAsync(lr => getdifference((DateTime)lr.StartDate, (DateTime)lr.EndDate));

                var summary = new
                {
                    AnnualBalance = balance.AnnualBalance,
                    AnnualUsed = usedAnnual,
                    AnnualRemaining = balance.AnnualBalance - usedAnnual,
                    SickBalance = balance.SickBalance,
                    SickUsed = usedSick,
                    SickRemaining = balance.SickBalance - usedSick,
                    OvertimeHours = balance.OvertimeHours,
                    AvailableBalance = balance.AvailableBalance,
                    LastUpdated = balance.LastUpdatedDate
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(summary, 1);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting balance summary");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> RequestAdjustment(VacationAdjustmentRequestDto requestDto)
        {
            try
            {
                var balance = await _context.VacationBalances
                    .FirstOrDefaultAsync(vb => vb.CompanyEmployeeId == requestDto.EmployeeId && vb.Year == requestDto.Year);

                if (balance == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("Vacation balance not found");
                }

                var adjustment = new VacationBalanceAdjustment
                {
                    VacationBalanceId = balance.Id,
                    AdjustmentAmount = requestDto.Amount,
                    AdjustmentReason = requestDto.Reason,
                    AdjustedBy = requestDto.RequestedBy,
                    AdjustmentDate = DateTime.Now,
                    Status = "Pending"
                };

                await _context.VacationBalanceAdjustments.AddAsync(adjustment);
                await _context.SaveChangesAsync();

                _cache.Remove(cacheKey);

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(adjustment, 1);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting adjustment");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetPendingAdjustments(int employeeId)
        {
            try
            {
                var adjustments = await _context.VacationBalanceAdjustments
                    .Include(a => a.VacationBalance)
                    .Where(a => a.VacationBalance.CompanyEmployeeId == employeeId && a.Status == "Pending")
                    .OrderByDescending(a => a.AdjustmentDate)
                    .ToListAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(adjustments, adjustments.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending adjustments");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> ProcessAdjustment(int adjustmentId, bool isApproved, string processedBy)
        {
            try
            {
                var adjustment = await _context.VacationBalanceAdjustments
                    .Include(a => a.VacationBalance)
                    .FirstOrDefaultAsync(a => a.Id == adjustmentId);

                if (adjustment == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("Adjustment not found");
                }

                if (isApproved)
                {
                    // Apply the adjustment
                    adjustment.VacationBalance.AnnualBalance += adjustment.AdjustmentAmount;
                    adjustment.VacationBalance.LastUpdatedDate = DateTime.Now;

                    // Create history record
                    var history = new VacationBalanceHistory
                    {
                        CompanyEmployeeId = adjustment.VacationBalance.CompanyEmployeeId,
                        ChangeInBalance = adjustment.AdjustmentAmount,
                        AdjustmentReason = adjustment.AdjustmentReason,
                        BalanceBeforeAdjustment = adjustment.VacationBalance.AnnualBalance - adjustment.AdjustmentAmount,
                        BalanceAfterAdjustment = adjustment.VacationBalance.AnnualBalance,
                        AdjustedBy = processedBy,
                        AdjustmentDate = DateTime.Now
                    };

                    await _context.VacationBalanceHistories.AddAsync(history);
                }

                adjustment.Status = isApproved ? "Approved" : "Rejected";
                adjustment.AdjustedBy = processedBy;
                adjustment.AdjustmentDate = DateTime.Now;

                _context.VacationBalanceAdjustments.Update(adjustment);
                await _context.SaveChangesAsync();

                _cache.Remove(cacheKey);

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(adjustment, 1);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing adjustment");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetBalanceProjection(int employeeId)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var balance = await _context.VacationBalances
                    .FirstOrDefaultAsync(vb => vb.CompanyEmployeeId == employeeId && vb.Year == currentYear);

                if (balance == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("No balance record found");
                }
                var Types = _context.LeaveTypes.ToList();

                // Get average monthly usage
                var monthlyUsage = await _context.LeaveRequests
                    .Where(lr => lr.CompanyEmployeeId == employeeId &&
                                lr.RequestedOn.Year == currentYear &&
                                lr.OverallStatus == "Approved" &&
                                lr.Type == Types.Where(l => l.NameEn == "Annual"))
                    .GroupBy(lr => lr.RequestedOn.Month)
                    .Select(g => new { Month = g.Key, DaysUsed = g.Sum(lr => getdifference((DateTime)lr.StartDate , (DateTime)lr.EndDate) ) })
                    .ToListAsync();

                var averageMonthlyUsage = monthlyUsage.Any() ? monthlyUsage.Average(x => x.DaysUsed) : 0;
                var monthsRemaining = 12 - DateTime.Now.Month;

                var projection = new
                {
                    CurrentBalance = balance.AnnualBalance,
                    AverageMonthlyUsage = Math.Round(averageMonthlyUsage, 2),
                    ProjectedYearEndBalance = balance.AnnualBalance - (averageMonthlyUsage * monthsRemaining),
                    ProjectedMonthlyBalance = Enumerable.Range(DateTime.Now.Month + 1, monthsRemaining)
                        .ToDictionary(
                            month => new DateTime(currentYear, month, 1).ToString("MMM"),
                            month => balance.AnnualBalance - (averageMonthlyUsage * (month - DateTime.Now.Month))
                        )
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(projection, 1);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting balance projection");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
         
        private int getdifference (DateTime startDate , DateTime endDate)
            {

            var x = endDate - startDate;
            var differenceInDays = x.Days + 1;
            return differenceInDays;

            }
        public async Task<OperationOutput> InitializeYearlyBalances(BulkInitializeRequestDto request)
        {
            try
            {
                var employees = await _context.CompanyEmployees
                    .Where(e => request.EmployeeIds.Contains(e.Id))
                    .ToListAsync();

                var existingBalances = await _context.VacationBalances
                    .Where(vb => vb.Year == request.Year && request.EmployeeIds.Contains(vb.CompanyEmployeeId))
                    .ToListAsync();

                var newBalances = new List<VacationBalance>();
                var histories = new List<VacationBalanceHistory>();

                foreach (var employee in employees)
                {
                    if (existingBalances.Any(eb => eb.CompanyEmployeeId == employee.Id))
                        continue;

                    var newBalance = new VacationBalance
                    {
                        CompanyEmployeeId = employee.Id,
                        Year = request.Year,
                        AnnualBalance = request.DefaultAnnualBalance,
                        SickBalance = request.DefaultSickBalance,
                        OvertimeHours = 0,
                        AvailableBalance = request.DefaultAnnualBalance,
                        ContractStartDate = employee.ContractStartDate,
                        ContractEndDate = employee.ContractEndDate,
                        CreatedBy = request.InitializedBy,
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                        IsDeleted = false,
                        LastUpdatedDate = DateTime.Now
                    };

                    newBalances.Add(newBalance);

                    histories.Add(new VacationBalanceHistory
                    {
                        CompanyEmployeeId = employee.Id,
                        ChangeInBalance = request.DefaultAnnualBalance,
                        AdjustmentReason = "Yearly balance initialization",
                        BalanceBeforeAdjustment = 0,
                        BalanceAfterAdjustment = request.DefaultAnnualBalance,
                        AdjustedBy = request.InitializedBy,
                        AdjustmentDate = DateTime.Now
                    });
                }

                await _context.VacationBalances.AddRangeAsync(newBalances);
                await _context.VacationBalanceHistories.AddRangeAsync(histories);
                await _context.SaveChangesAsync();

                _cache.Remove(cacheKey);

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(new { Count = newBalances.Count }, newBalances.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing yearly balances");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetUsageTrends(int employeeId, int yearsBack = 3)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var startYear = currentYear - yearsBack;

                var trends = await _context.VacationBalanceHistories
                    .Where(h => h.CompanyEmployeeId == employeeId &&
                               h.AdjustmentDate.Year >= startYear)
                    .GroupBy(h => h.AdjustmentDate.Year)
                    .Select(g => new
                    {
                        Year = g.Key,
                        AnnualUsed = g.Where(x => x.AdjustmentReason.Contains("Annual")).Sum(x => -x.ChangeInBalance),
                        SickUsed = g.Where(x => x.AdjustmentReason.Contains("Sick")).Sum(x => -x.ChangeInBalance),
                        Adjustments = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ToListAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(trends, trends.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage trends");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> TransferBalances(BalanceTransferDto transferDto)
        {
            try
            {
                var balance = await _context.VacationBalances
                    .FirstOrDefaultAsync(vb => vb.CompanyEmployeeId == transferDto.EmployeeId && vb.Year == transferDto.Year);

                if (balance == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("Vacation balance not found");
                }

                // Validate transfer
                if (transferDto.FromType == BalanceType.Annual && balance.AnnualBalance < transferDto.Amount)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("Insufficient annual balance");
                }

                if (transferDto.FromType == BalanceType.Sick && balance.SickBalance < transferDto.Amount)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("Insufficient sick balance");
                }

                if (transferDto.FromType == BalanceType.Overtime && balance.OvertimeHours < transferDto.Amount)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned("Insufficient overtime hours");
                }

                // Perform transfer
                switch (transferDto.FromType)
                {
                    case BalanceType.Annual:
                        balance.AnnualBalance -= transferDto.Amount;
                        break;
                    case BalanceType.Sick:
                        balance.SickBalance -= transferDto.Amount;
                        break;
                    case BalanceType.Overtime:
                        balance.OvertimeHours -= transferDto.Amount;
                        break;
                }

                switch (transferDto.ToType)
                {
                    case BalanceType.Annual:
                        balance.AnnualBalance += transferDto.Amount;
                        break;
                    case BalanceType.Sick:
                        balance.SickBalance += transferDto.Amount;
                        break;
                    case BalanceType.Overtime:
                        balance.OvertimeHours += transferDto.Amount;
                        break;
                }

                balance.LastUpdatedDate = DateTime.Now;

                // Create history records
                var fromHistory = new VacationBalanceHistory
                {
                    CompanyEmployeeId = transferDto.EmployeeId,
                    ChangeInBalance = -transferDto.Amount,
                    AdjustmentReason = $"Transfer to {transferDto.ToType} balance",
                    BalanceBeforeAdjustment = 0, // Will be set below
                    BalanceAfterAdjustment = 0,  // Will be set below
                    AdjustedBy = transferDto.RequestedBy,
                    AdjustmentDate = DateTime.Now
                };

                var toHistory = new VacationBalanceHistory
                {
                    CompanyEmployeeId = transferDto.EmployeeId,
                    ChangeInBalance = transferDto.Amount,
                    AdjustmentReason = $"Transfer from {transferDto.FromType} balance",
                    BalanceBeforeAdjustment = 0, // Will be set below
                    BalanceAfterAdjustment = 0,  // Will be set below
                    AdjustedBy = transferDto.RequestedBy,
                    AdjustmentDate = DateTime.Now
                };

                // Set before/after values based on transfer type
                // (Implementation depends on your specific requirements)

                _context.VacationBalances.Update(balance);
                await _context.VacationBalanceHistories.AddRangeAsync(fromHistory, toHistory);
                await _context.SaveChangesAsync();

                _cache.Remove(cacheKey);

                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(balance, 1);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring balances");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
    }
}
