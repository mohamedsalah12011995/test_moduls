using Core.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
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
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;

using Core.Helpers;
using GeneralStructure.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using static Core.Helpers.OperationOutput;




namespace Nupco.EF.Repositories
{
    public class LeaveRequestRepository : BaseRepository<LeaveRequest>, ILeaveRequestRepository
    {
        private readonly LeaveRequest_Assembler _Assembler;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "LeaveRequestData";

        public LeaveRequestRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new LeaveRequest_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public async Task<OperationOutput> CreateNewLeaveRequest(LeaveRequestDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;

                // Set initial status and approval level for new leave request
                entityDto.OverallStatus = "Pending";
                entityDto.CurrentApprovalLevel = 1;
                entityDto.RequestedOn = DateTime.Now;

                var _model = _Assembler.WriteDal(entityDto);
                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                // Create initial approval record
                await CreateInitialApprovalRecord(_entity);

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



        public async Task<OperationOutput> CreateConfirmedLeaveRequest(LeaveRequestDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;
        //        entityDto.CompanyEmployeeId = _context.CompanyEmployees.First(c => c.Code == entityDto.CompanyEmployeeId.ToString()).Id;

                // Set initial status and approval level for new leave request
                entityDto.OverallStatus = "Approved";
                entityDto.CurrentApprovalLevel = 1;
                entityDto.RequestedOn = DateTime.Now;
                

                var _model = _Assembler.WriteDal(entityDto);
                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                // Create initial approval record
           
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

        public async Task<OperationOutput> AddNewAsync(LeaveRequestDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;

                // Set initial status and approval level for new leave request
                entityDto.OverallStatus = "Pending";
                entityDto.CurrentApprovalLevel = 1;
                entityDto.RequestedOn = DateTime.Now;

                var _model = _Assembler.WriteDal(entityDto);
                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                // Create initial approval record
                await CreateInitialApprovalRecord(_entity);

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

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<LeaveRequestDto> entitiesDto)
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
                var _entities = await FindAllAsync(i => i.IsDeleted == false, "Type");
                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _LeaveRequests = _Assembler.WriteListDto(_entities);

                var _result = resultOutput.GenearetResultOutput(_LeaveRequests, count);
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
                var LeaveRequests = await FindAllAsync(i => i.IsDeleted == false && i.RequesterUserId == userId);
                var _LeaveRequests = _Assembler.WriteListDto(LeaveRequests);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_LeaveRequests, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GetAllByCompanyEmployeeIdAsync(int CompanyEmployeeId)
        {
            try
            {
                var LeaveRequests = await _context.LeaveRequests.Include(L => L.Type)
                    .Where(lr => lr.Type.NameEn != "TimeSheet" && lr.CompanyEmployeeId == CompanyEmployeeId ) // prevent materializing null into non-nullable
                    .ToListAsync();
                var _LeaveRequests = _Assembler.WriteListDto(LeaveRequests);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_LeaveRequests, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> UpdateEntity(LeaveRequestDto entityDto)
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

        public async Task<OperationOutput> UpdateEntityAsync(LeaveRequestDto entityDto, object key)
        {
            try
            {
                var _LeaveRequest = await GetByIdAsync((int)entityDto.Id);
                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _LeaveRequest);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<LeaveRequestDto> entities)
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
                var LeaveRequests = new List<LeaveRequest>();

                var _LeaveRequestList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                LeaveRequests = _LeaveRequestList.Where(i => i.IsDeleted == false).ToList();

                var _LeaveRequestsDto = _Assembler.WriteListDto(LeaveRequests);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_LeaveRequestsDto, counts);
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
                string[] stringArray = { "CreatedByUser", "Approvals" };

                var _LeaveRequest = await FindAsync(f => f.Id == id, stringArray);
                if (_LeaveRequest is not null)
                {
                    var _entity = _Assembler.WriteDto(_LeaveRequest);
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

        public async Task<OperationOutput> ApproveLeaveRequest(int leaveRequestId, string approverUserId, string comments, int? timeSheetId = 0)
        {
            try
            {
                ResultOutputData resultOutputData = new ResultOutputData();
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Approvals).Include(L => L.Type)
                    .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);

                if (leaveRequest == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                var currentApproval = leaveRequest.Approvals
                    .FirstOrDefault(a => a.ApprovalLevel == leaveRequest.CurrentApprovalLevel &&
                                        a.ApproverUserId == approverUserId &&
                                        a.Status == "Pending");

                if (currentApproval == null)
                {
                    return resultOutputData.GenearetResultOutput("Approver not found or request already processed", 0);
                }

                currentApproval.Status = "Approved";
                currentApproval.Comments = comments;
                currentApproval.ApprovedDate = DateTime.Now;
                currentApproval.UpdatedDate = DateTime.Now;
                currentApproval.UpdatedBy = approverUserId;

                // Check for next approval level
                var nextApprovalLevel = leaveRequest.CurrentApprovalLevel + 1;

                var requestingEmployee = await _context.CompanyEmployees
                                                .Include(e => e.VacationBalance)
                                                .FirstOrDefaultAsync(e => e.UserId == leaveRequest.RequesterUserId);

                if (requestingEmployee == null)
                {
                    dynamic x = "Requesting employee not found";
                    return resultOutputData.GenearetResultOutput(x, 0);
                }

                var companyId = requestingEmployee.CompanyId;

                var nextApprovalConfig = await _context.ApprovalConfigurations
                    .Where(ac => ac.CompanyId == companyId && ac.ApprovalLevel == nextApprovalLevel && ac.IsActive == true && ac.IsDeleted == false)
                    .OrderBy(ac => ac.ApprovalLevel)
                    .FirstOrDefaultAsync();

                if (nextApprovalConfig != null)
                {
                    leaveRequest.CurrentApprovalLevel = nextApprovalLevel;

                    // Create approval record for next level
                    var nextApproval = new LeaveRequestApproval
                    {
                        LeaveRequestId = leaveRequest.Id,
                        ApprovalLevel = (int)nextApprovalLevel,
                        Status = "Pending",
                        CreatedDate = DateTime.Now,
                        ApproverUserId = await GetApproverUserIdForRole(nextApprovalConfig.ApproverRole, (int)companyId),
                        IsActive = true,
                        IsDeleted = false
                    };

                    if (string.IsNullOrEmpty(nextApproval.ApproverUserId))
                    {
                        return resultOutputData.GenearetResultOutput($"No approver found for role {nextApprovalConfig.ApproverRole}", 0);
                    }

                    _context.LeaveRequestApprovals.Add(nextApproval);
                }
                else
                {
                    // No more approval levels, request is fully approved\
                    leaveRequest.OverallStatus = "Approved";

                    if (leaveRequest.Type.Id == _context.LeaveTypes.First(L => L.NameEn == "TimeSheet").Id)
                    {
                        TimeSheet t = _context.TimeSheets.Include(t => t.Entries).FirstOrDefault(t => t.Id == leaveRequest.TimeSheetId!);

                        if (t != null)
                        {
                            t.IsApproved = true;

                            _context.TimeSheets.Update(t);

                            // Calculate taken days from timesheet entries
                            var annualTaken = t.Entries
                                .Where(e => e.Type == DayType.Annual)
                                .Count();

                            var sickTaken = t.Entries
                                .Where(e => e.Type == DayType.Sick)
                                .Count();

                            var currentYear = DateTime.Now.Year;
                            var vacationBalance = requestingEmployee.VacationBalance ??
                                await _context.VacationBalances.FirstOrDefaultAsync(vb =>
                                    vb.CompanyEmployeeId == requestingEmployee.Id &&
                                    vb.Year == currentYear);

                            if (vacationBalance != null)
                            {
                                // Handle Annual Leave deductions
                                if (annualTaken > 0)
                                {
                                    var annualBefore = vacationBalance.AnnualBalance;
                                    vacationBalance.AnnualBalance -= annualTaken;
                                    var annualAfter = vacationBalance.AnnualBalance;

                                    // Create history record for annual leave
                                    var annualHistory = new VacationBalanceHistory
                                    {
                                        CompanyEmployeeId = requestingEmployee.Id,
                                        VacationBalanceId = vacationBalance.Id,
                                        ChangeInBalance = -annualTaken,
                                        AdjustmentReason = $"Approved Timesheet #{t.Id} - Annual Leave Days",
                                        BalanceBeforeAdjustment = annualBefore,
                                        BalanceAfterAdjustment = annualAfter,
                                        AdjustedBy = approverUserId,
                                        AdjustmentDate = DateTime.Now
                                    };
                                    await _context.VacationBalanceHistories.AddAsync(annualHistory);

                                    // Create adjustment record for annual leave
                                    var annualAdjustment = new VacationBalanceAdjustment
                                    {
                                        VacationBalanceId = vacationBalance.Id,
                                        AdjustmentAmount = -annualTaken,
                                        AdjustmentReason = $"Timesheet #{t.Id} - Annual Leave Days Approved",
                                        AdjustedBy = approverUserId,
                                        AdjustmentDate = DateTime.Now,
                                        Status = "Approved"
                                    };
                                    await _context.VacationBalanceAdjustments.AddAsync(annualAdjustment);
                                }

                                // Handle Sick Leave deductions
                                if (sickTaken > 0)
                                {
                                    var sickBefore = vacationBalance.SickBalance;
                                    vacationBalance.SickBalance -= sickTaken;
                                    var sickAfter = vacationBalance.SickBalance;

                                    // Create history record for sick leave
                                    var sickHistory = new VacationBalanceHistory
                                    {
                                        CompanyEmployeeId = requestingEmployee.Id,
                                        VacationBalanceId = vacationBalance.Id,
                                        ChangeInBalance = -sickTaken,
                                        AdjustmentReason = $"Approved Timesheet #{t.Id} - Sick Leave Days",
                                        BalanceBeforeAdjustment = sickBefore,
                                        BalanceAfterAdjustment = sickAfter,
                                        AdjustedBy = approverUserId,
                                        AdjustmentDate = DateTime.Now
                                    };
                                    await _context.VacationBalanceHistories.AddAsync(sickHistory);

                                    // Create adjustment record for sick leave
                                    var sickAdjustment = new VacationBalanceAdjustment
                                    {
                                        VacationBalanceId = vacationBalance.Id,
                                        AdjustmentAmount = -sickTaken,
                                        AdjustmentReason = $"Timesheet #{t.Id} - Sick Leave Days Approved",
                                        AdjustedBy = approverUserId,
                                        AdjustmentDate = DateTime.Now,
                                        Status = "Approved"
                                    };
                                    await _context.VacationBalanceAdjustments.AddAsync(sickAdjustment);
                                }

                                vacationBalance.LastUpdatedDate = DateTime.Now;
                                _context.VacationBalances.Update(vacationBalance);
                            }
                            else
                            {
                                _logger.LogWarning($"No vacation balance found for employee {requestingEmployee.Id} in year {currentYear}");
                            }
                        }
                        else
                        {
                            return resultOutputData.GenearetResultOutput($"No timesheet found for request {leaveRequest.Id}", 0);
                        }
                    }

                    // Update vacation balance for approved leave requests
                    if (leaveRequest.Type.NameEn == "Annual" || leaveRequest.Type.NameEn == "Sick")
                    {
                        var currentYear = DateTime.Now.Year;
                        var vacationBalance = requestingEmployee.VacationBalance ??
                            await _context.VacationBalances.FirstOrDefaultAsync(vb =>
                                vb.CompanyEmployeeId == requestingEmployee.Id && vb.Year == currentYear);

                        if (vacationBalance != null)
                        {
                            int balanceBefore = 0;
                            int balanceAfter = 0;
                            string balanceType = "";

                            if (leaveRequest.Type == _context.LeaveTypes.First(L => L.NameEn == "Annual"))
                            {
                                balanceBefore = vacationBalance.AnnualBalance;
                                vacationBalance.AnnualBalance -= getdifference((DateTime)leaveRequest.StartDate, (DateTime)leaveRequest.EndDate);
                                balanceAfter = vacationBalance.AnnualBalance;
                                balanceType = "Annual";
                            }
                            else if (leaveRequest.Type == _context.LeaveTypes.First(L => L.NameEn == "Sick"))
                            {
                                balanceBefore = vacationBalance.SickBalance;
                                vacationBalance.SickBalance -= getdifference((DateTime)leaveRequest.StartDate, (DateTime)leaveRequest.EndDate);
                                balanceAfter = vacationBalance.SickBalance;
                                balanceType = "Sick";
                            }

                            vacationBalance.LastUpdatedDate = DateTime.Now;
                            _context.VacationBalances.Update(vacationBalance);

                            // Create vacation balance history record
                            var history = new VacationBalanceHistory
                            {
                                CompanyEmployeeId = requestingEmployee.Id,
                                VacationBalanceId = vacationBalance.Id,
                                ChangeInBalance = -getdifference((DateTime)leaveRequest.StartDate, (DateTime)leaveRequest.EndDate),
                                AdjustmentReason = $"Approved {balanceType} leave request #{leaveRequest.Id}",
                                BalanceBeforeAdjustment = balanceBefore,
                                BalanceAfterAdjustment = balanceAfter,
                                AdjustedBy = approverUserId,
                                AdjustmentDate = DateTime.Now
                            };

                            await _context.VacationBalanceHistories.AddAsync(history);

                            // Create vacation balance adjustment record
                            var adjustment = new VacationBalanceAdjustment
                            {
                                VacationBalanceId = vacationBalance.Id,
                                AdjustmentAmount = -getdifference((DateTime)leaveRequest.StartDate, (DateTime)leaveRequest.EndDate),
                                AdjustmentReason = $"Approved {balanceType} leave request #{leaveRequest.Id}",
                                AdjustedBy = approverUserId,
                                AdjustmentDate = DateTime.Now,
                                Status = "Approved"
                            };

                            await _context.VacationBalanceAdjustments.AddAsync(adjustment);
                        }
                        else
                        {
                            _logger.LogWarning($"No vacation balance found for employee {requestingEmployee.Id} in year {currentYear}");
                        }
                    }
                }

                leaveRequest.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                var updatedLeaveRequestDto = _Assembler.WriteDto(leaveRequest);
                return resultOutputData.GenearetResultOutput(updatedLeaveRequestDto, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving leave request");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        private int getdifference(DateTime startDate, DateTime endDate)
        {

            var x = endDate - startDate;
            var differenceInDays = x.Days + 1;
            return differenceInDays;

        }

        public async Task<OperationOutput> RejectLeaveRequest(int leaveRequestId, string approverUserId, string comments, int? timeSheetId = 0)
        {
            try
            {
                ResultOutputData resultOutputData = new ResultOutputData();
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Approvals).Include(Lr => Lr.Type)
                    .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);

                if (leaveRequest == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                var currentApproval = leaveRequest.Approvals
                    .FirstOrDefault(a => a.ApprovalLevel == leaveRequest.CurrentApprovalLevel &&
                                        a.ApproverUserId == approverUserId &&
                                        a.Status == "Pending");

                if (currentApproval == null)
                {

                    
                    return resultOutputData.GenearetResultOutput("Approver not found or request already processed", 0);
                }

                currentApproval.Status = "Rejected";
                currentApproval.Comments = comments;
                currentApproval.ApprovedDate = DateTime.Now;
                currentApproval.UpdatedDate = DateTime.Now;
                currentApproval.UpdatedBy = approverUserId;

                leaveRequest.OverallStatus = "Rejected";
                leaveRequest.UpdatedDate = DateTime.Now;


                if (leaveRequest.Type.NameEn == "TimeSheet")
                {
                    TimeSheet t = _context.TimeSheets.FirstOrDefault(t => t.Id == timeSheetId);
                    t.IsApproved = false;
                }

                await _context.SaveChangesAsync();

                var updatedLeaveRequestDto = _Assembler.WriteDto(leaveRequest);
                return resultOutputData.GenearetResultOutput(updatedLeaveRequestDto, 1);
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        private async Task CreateInitialApprovalRecord(LeaveRequest leaveRequest)
        {
            var requestingEmployee = await _context.CompanyEmployees
                                        .FirstOrDefaultAsync(e => e.Id == leaveRequest.CompanyEmployeeId);

            if (requestingEmployee == null)
            {
                throw new Exception("Requesting employee not found");
            }

            string lineManagerUserId = requestingEmployee.DirectManagerUserId?.ToString();

            if (string.IsNullOrEmpty(lineManagerUserId))
            {
                throw new Exception("Line manager not set for the employee");
            }

            var initialApproval = new LeaveRequestApproval
            {
                LeaveRequestId = leaveRequest.Id,
                ApproverUserId = lineManagerUserId,
                ApprovalLevel = 1,
                Status = "Pending",
                CreatedDate = DateTime.Now,
                CreatedBy = "System",
                IsActive = true,
                IsDeleted = false
            };
            _context.LeaveRequestApprovals.Add(initialApproval);
            await _context.SaveChangesAsync();
        }

        private async Task<string> GetApproverUserIdForRole(string approverRole, int companyId)
        {
            // Implement logic to find the UserId of the approver based on their role and company
            // This is a placeholder implementation - adjust according to your actual user/role structure

            if (approverRole == "LineManager")
            {
                // For line managers, we'd typically get this from the employee's line manager
                // This might not be the right place to implement this
                return null;
            }

            // For other roles (HR, etc.), query your user/role tables
            var approver = await _context.CompanyEmployees
                                    .FirstOrDefaultAsync(e => e.CompanyId == companyId &&
                                                             e.Role == approverRole);

            return approver?.UserId;
        }

        public async Task<OperationOutput> GetAllDataForEmployeeAsync(string Code)
        {
            try
            {
                var _entities = await FindAllAsync(i => i.IsDeleted == false && i.CompanyEmployee!.Code == Code, "CompanyEmployee");
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
        public async Task<List<LeaveRequest>> GetAllLeaveRequestForEmployeeAsync(string Code)
        {
            try
            {
                // Simply check that LeaveTypeId has a value and exists in database
                var _entities = await FindAllAsync(
                    i => i.IsDeleted == false &&
                         i.LeaveTypeId != null &&  // Has a leave type assigned
                         i.OverallStatus == "Approved" &&
                         i.CompanyEmployeeId.ToString() == Code,
                    "Type");

                return _entities.ToList();
            }
            catch (Exception)
            {
                return new List<LeaveRequest>();
            }
        }
    }
}