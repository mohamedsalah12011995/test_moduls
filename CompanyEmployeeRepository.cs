using ClosedXML.Excel;
using Core.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.OutSourceEmployees;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.Core.Models.OutSource_Employees;
using Nupco.DAL;
using Nupco.DAL.Models.OutSourceEmployees;
using Nupco.DAL.Models.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;

namespace Nupco.EF.Repositories
{
    public class CompanyEmployeeRepository : BaseRepository<CompanyEmployee>, ICompanyEmployeeRepository
    {
        private readonly CompanyEmployee_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "CompanyEmployeeData";


        public CompanyEmployeeRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache, IConfiguration configuration) : base(context, logger)
        {
            _Assembler = new CompanyEmployee_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;
            _configuration = configuration;


        }

        public async Task<OperationOutput> CreateNewCompanyEmployee(CompanyEmployeeDto entityDto)
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
        public async Task<OperationOutput> AddNewAsync(CompanyEmployeeDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;

                var _model = _Assembler.WriteDal(entityDto);

                // Check if user and manager exist in the system
                var user = await _context.Users
                    .Where(e => e.Email != null && e.Email == _model.Email).FirstOrDefaultAsync();

                var OutSourceAnnualBalance = await _context.Settings
                    .FirstOrDefaultAsync(e => e.Key == "OutSourceAnnualBalance");

                var OutSourceSickBalance = await _context.Settings
                    .FirstOrDefaultAsync(e => e.Key == "OutSourceSickBalance");

                int annual = 21, sick = 5;
                if(OutSourceAnnualBalance != null)
                     annual = int.Parse(OutSourceAnnualBalance.Value);

                if (OutSourceSickBalance != null)
                     sick = int.Parse(OutSourceSickBalance.Value);



                var manager = await _context.Users
                    .Where(e => e.Email != null && e.Email == _model.DirectManagerEmail).FirstOrDefaultAsync();

                // Link the user if found
                if (user != null)
                {
                    _model.FullName = user.FirstName + " " + user.LastName;
                    _model.UserId = user.Id;
                }

                // Link the manager if found
                if (manager != null)
                {
                    _model.DirectManagerUserId = manager.Id;
                    _model.DirectManagerName = manager.FirstName + " " + manager.LastName;

                    var _managerEmployee = new CompanyEmployee
                    {
                        CompanyId = entityDto.CompanyId,
                        UserId = _model.DirectManagerUserId,
                        FullName = _model.DirectManagerName,
                        Email = _model.DirectManagerEmail,
                        Role = "Direct Manager",
                        IsPrimaryContact = false,
                        IsNupcoEmployee = true,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System",
                        IsDeleted = false
                    };

                    // Check if manager already exists
                    var ManagerExists = await _context.CompanyEmployees
                        .AnyAsync(e => e.Email == _model.DirectManagerEmail && e.CompanyId == entityDto.CompanyId);

                    if (!ManagerExists)
                    {
                        await AddAsync(_managerEmployee);
                    }
                }

                var _entity = await AddAsync(_model);
                await _context.SaveChangesAsync();
                VacationBalance v = new VacationBalance
                {
                    AnnualBalance = annual,
                    SickBalance = sick,
                    CompanyEmployeeId = _entity.Id,
                    IsActive = true,
                    IsDeleted = false,
                    Year = DateTime.Now.Year,
                    ContractStartDate = _model.ContractStartDate,  
                    ContractEndDate = _model.ContractEndDate       
                };


                _context.VacationBalances.AddAsync(v);

                await SaveChangesAsync();

                _cache.Remove(cacheKey);
                var _entityDto = _Assembler.WriteDto(_entity);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entityDto, 1);
                return _result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding company employee");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<CompanyEmployeeDto> entitiesDto)
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
                var CompanyEmployees = await FindAllAsync(i => i.IsDeleted == false);
                var _CompanyEmployees = _Assembler.WriteListDto(CompanyEmployees);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CompanyEmployees, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(CompanyEmployeeDto entityDto)
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

        public async Task<OperationOutput> UpdateEntityAsync(CompanyEmployeeDto entityDto, object key)
        {
            try
            {
                var _CompanyEmployee = await GetByIdAsync((int)entityDto.Id);


                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _CompanyEmployee);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<CompanyEmployeeDto> entities)
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
                var CompanyEmployees = new List<CompanyEmployee>();

                var _CompanyEmployeeList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                CompanyEmployees = _CompanyEmployeeList.Where(i => i.IsDeleted == false).ToList();

                var _CompanyEmployeesDto = _Assembler.WriteListDto(CompanyEmployees);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CompanyEmployeesDto, counts);
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

                var _CompanyEmployee = await _context.CompanyEmployees
                    .Where(e => e.Id == id ).Include(C => C.Company)
                    .FirstOrDefaultAsync();
                if (_CompanyEmployee is not null)
                {
                    var _entity = _Assembler.WriteDto(_CompanyEmployee);
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

        public async Task<OperationOutput> GetEmployeesByCompany(int companyId)
        {
            try
            {
                var employees = await _context.CompanyEmployees
                    .Where(e => e.CompanyId == companyId && e.IsDeleted == false).Include(C => C.Company)
                    .ToListAsync();
                var _CompanyEmployeesDto = _Assembler.WriteListDto(employees);

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(_CompanyEmployeesDto, _CompanyEmployeesDto.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees by company");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetManagedTimeSheets(int employeeId, int month, int year)
        {
            try
            {
                var timeSheets = await _context.TimeSheets
                    .Where(t => t.Employee.Company.Employees.Any(e => e.Id == employeeId) &&
                               t.Month == month &&
                               t.Year == year)
                    .Include(t => t.Employee)
                    .Include(t => t.Entries)
                    .ToListAsync();

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(timeSheets, timeSheets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting managed timesheets");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> AssignTimeSheetToEmployee(int employeeId, int timeSheetId)
        {
            try
            {
                var employee = await _context.CompanyEmployees.FindAsync(employeeId);
                var timeSheet = await _context.TimeSheets.FindAsync(timeSheetId);

                if (employee == null || timeSheet == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                // Add logic to assign timesheet to employee (e.g., update approver field)
                // This depends on your business logic

                await _context.SaveChangesAsync();

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput("Timesheet assigned successfully", 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning timesheet to employee");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> UploadCompanyEmployees(int companyId, IFormFile excelFile)
        {
            try
            {
                // Validate company exists
                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                // Validate file
                if (excelFile == null || excelFile.Length == 0)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var fileExtension = Path.GetExtension(excelFile.FileName).ToLower();
                if (fileExtension != ".xlsx" && fileExtension != ".xls")
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var addedManagerEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var employees = new List<CompanyEmployee>();
                var errorMessages = new List<string>();
                int successCount = 0;

                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RowsUsed().Skip(1); // Skip header row

                        foreach (var row in rows)
                        {
                            try
                            {
                                var fullName = row.Cell(1).GetValue<string>()?.Trim();
                                var email = row.Cell(2).GetValue<string>()?.Trim();
                                var manager_fullname = row.Cell(3).GetValue<string>()?.Trim();
                                var manager_email = row.Cell(4).GetValue<string>()?.Trim();
                                var role = row.Cell(5).GetValue<string>()?.Trim();
                                var isPrimary = row.Cell(6).GetValue<bool>();
                                var isNupco = row.Cell(7).GetValue<bool>();
                                var Code = row.Cell(8).GetValue<string>();


                                // Validate required fields
                                if (string.IsNullOrEmpty(fullName))
                                    throw new Exception("Full name is required");
                                if (string.IsNullOrEmpty(email))
                                    throw new Exception("Email is required");
                                if (string.IsNullOrEmpty(role))
                                    throw new Exception("role is required");

                                // Check if employee already exists
                                var exists = await _context.CompanyEmployees
                                    .AnyAsync(e => e.Email == email && e.CompanyId == companyId);

                                if (exists)
                                {
                                    errorMessages.Add($"Skipped duplicate: {email}");
                                    continue;
                                }

                                string userId = "", directManagerUserId = "", directManagerName = "";

                                var user = await _context.Users.
                                    Where(e => e.Email != null && e.Email == email).FirstOrDefaultAsync();
                                var manager = await _context.Users.
                                    Where(e => e.Email != null && e.Email == manager_email).FirstOrDefaultAsync();
                                if (user != null)
                                {
                                    fullName = user.FirstName + " " + user.LastName;
                                    userId = user.Id;
                                }
                                if (manager != null)
                                {
                                    directManagerUserId = manager.Id;
                                    directManagerName = manager.FirstName + " " + manager.LastName;

                                    var _managerEmployee = new CompanyEmployee
                                    {
                                        CompanyId = companyId,
                                        UserId = directManagerUserId,
                                        FullName = directManagerName,
                                        Email = manager_email,
                                        Role = "Direct Manager",
                                        Code = Code,
                                        DirectManagerUserId = null,
                                        DirectManagerEmail = null,
                                        DirectManagerName = null,
                                        IsPrimaryContact = false,
                                        IsNupcoEmployee = true,
                                        IsActive = true,
                                        CreatedDate = DateTime.Now,
                                        CreatedBy = "Excel Import",
                                        IsDeleted = false
                                    };

                                    // Check if manager already exists
                                    var Manager_exists = await _context.CompanyEmployees
    .AnyAsync(e => e.Email == manager_email && e.CompanyId == companyId);

                                    if (Manager_exists || addedManagerEmails.Contains(manager_email))
                                    {
                                        errorMessages.Add($"Manager skipped duplicate: {manager_email}");
                                    }
                                    else
                                    {
                                        employees.Add(_managerEmployee);
                                        addedManagerEmails.Add(manager_email);
                                    }

                                }

                                var employee = new CompanyEmployee
                                {
                                    CompanyId = companyId,
                                    UserId = userId,
                                    FullName = fullName,
                                    Email = email,
                                    Role = role,
                                    DirectManagerUserId = directManagerUserId,
                                    DirectManagerEmail = manager_email,
                                    DirectManagerName = directManagerName,
                                    IsPrimaryContact = isPrimary,
                                    IsNupcoEmployee = isNupco,
                                    IsActive = true,
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = "Excel Import",
                                    IsDeleted = false
                                };

                                employees.Add(employee);
                                successCount++;
                            }
                            catch (Exception ex)
                            {
                                errorMessages.Add($"Row {row.RowNumber()}: {ex.Message}");
                            }
                        }
                    }
                }

                if (employees.Count > 0)
                {
                    await _context.CompanyEmployees.AddRangeAsync(employees);
                    await _context.SaveChangesAsync();
                }

                var result = new
                {
                    CompanyId = companyId,
                    CompanyName = company.Name,
                    AddedEmployees = successCount,
                    ErrorCount = errorMessages.Count,
                    Errors = errorMessages.Take(10).ToList() // Return first 10 errors if any
                };

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(result, successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading company employees");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        public async Task<OperationOutput> GetEmployeeLeaveSummary(int CompanyEmployeeId)
        {
            try
            {


                var employee = await FindAsync(e => e.Id == CompanyEmployeeId, new[] { "VacationBalance", "LeaveRequests" });



                if (employee == null)
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
                var currentYear = DateTime.Now.Year;

                var summary = new
                {
                    AnnualBalance = employee.VacationBalance?.AnnualBalance ?? 0,
                    SickBalance = employee.VacationBalance?.SickBalance ?? 0,
                    OvertimeHours = employee.VacationBalance?.OvertimeHours ?? 0,
                    ApprovedAnnualLeave = employee.LeaveRequests?
                        .Where(l => l.Type.NameEn == "Annual" && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                        .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0,
                    ApprovedSickLeave = employee.LeaveRequests?
                        .Where(l => l.Type.NameEn == "Sick" && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                        .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0,
                    PendingRequests = employee.LeaveRequests?
                        .Count(l => l.OverallStatus == "Pending") ?? 0
                };
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

        public async Task<OperationOutput> GetEmployeeAbsences(int CompanyEmployeeId, int year)
        {
            try
            {
                var employee = await _context.CompanyEmployees
                //.Include(e => e.TimeSheets)
                //    .ThenInclude(t => t.Entries)
                .FirstOrDefaultAsync(e => e.Id == CompanyEmployeeId);

                if (employee == null)
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
                var absences = employee.ManagedTimeSheets?
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
                var pendingRequests = await _context.LeaveRequests.ToListAsync();


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
                e => e.Department == department, new[] { "VacationBalance", "LeaveRequests" });


                var currentYear = DateTime.Now.Year;

                var summary = employees.Select(e => new
                {
                    CompanyEmployeeId = e.Id,
                    EmployeeName = e.FullName,
                    AnnualBalance = e.VacationBalance?.AnnualBalance ?? 0,
                    SickBalance = e.VacationBalance?.SickBalance ?? 0,
                    UsedAnnual = e.LeaveRequests?
                        .Where(l => l.Type.NameEn == "Annual" && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                        .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0,
                    UsedSick = e.LeaveRequests?
                        .Where(l => l.Type.NameEn == "Sick" && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                        .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0,
                    RemainingAnnual = (e.VacationBalance?.AnnualBalance ?? 0) -
                        (e.LeaveRequests?
                            .Where(l => l.Type.NameEn == "Annual" && l.OverallStatus == "Approved" && l.StartDate?.Year == currentYear)
                            .Sum(l => (l.EndDate - l.StartDate)?.TotalDays + 1) ?? 0),
                    PendingRequests = e.LeaveRequests?
                        .Count(l => l.OverallStatus == "Pending") ?? 0
                }).ToList();


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

        public async Task<OperationOutput> UpdateVacationBalance(int CompanyEmployeeId, int? annual = null, int? sick = null, int? overtime = null)
        {
            try
            {
                var balance = _context.VacationBalances.FirstOrDefault(b => b.CompanyEmployeeId == CompanyEmployeeId);

                if (balance == null)
                {
                    balance = new VacationBalance { CompanyEmployeeId = CompanyEmployeeId };
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

        public async Task<OperationOutput> GetEmployeeTimeSheetSummary(int CompanyEmployeeId, int year, int month)
        {
            try
            {
                //            var timeSheet =  _context.TimeSheets.Where(
                //t => t.CompanyEmployeeId == CompanyEmployeeId && t.Year == year && t.Month == month).Include(t => t.Entries).FirstOrDefault();

                var timeSheet = _context.TimeSheets.Where(
    t => t.CompanyEmployeeId == CompanyEmployeeId && t.Year == year && t.Month == month).Include(T => T.Entries).FirstOrDefault();
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
        public async Task<OperationOutput> CreateEmployeeTimeSheet(TimeSheetBulkUploadDto bulkUploadDto, int Id)
        {
            try
            {
                // Validate input file
                var validationResult = ValidateUploadFile(bulkUploadDto);
                if (validationResult != null) return validationResult;

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
                    var timeSheet = await GetOrCreateTimeSheet(Id, bulkUploadDto.TimeSheetId, month, year);
                    if (timeSheet == null) return ResultOutputData.GenearetResultOutputCatch();

                    // Process Excel entries - updates existing or adds new ones
                    var (entries, errorCount, errorMessages) = await ProcessExcelEntries(worksheet, timeSheet.Id, startRow);
                    if (entries.Count == 0) return ResultOutputData.GenearetResultOutputNoDataReturned();

                    // Save entries and return result
                    return await SaveAndReturnResult(entries, errorCount, errorMessages, Id, timeSheet.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing timesheet for user {UserId}", Id);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> CreateVerifiedEmployeeTimeSheet(TimeSheetBulkUploadDto bulkUploadDto, int employeeId)
        {
            try
            {
                // Validate input file
                ResultOutputData resultOutputData = new ResultOutputData();

                var validationResult = ValidateUploadFile(bulkUploadDto);
                if (validationResult != null) return validationResult;

                using var stream = new MemoryStream();
                await bulkUploadDto.File.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);
                {
                    if (workbook.Worksheets.Count == 0)
                        return ResultOutputData.GenearetResultOutputNoDataReturned();

                    var worksheet = workbook.Worksheet(1);
                    int startRow = FindFirstDateRow(worksheet);
                    if (startRow == -1)
                        return ResultOutputData.GenearetResultOutputNoDataReturned();

                    var (month, year) = DetectMonthAndYearFromEntries(worksheet, startRow);

                    // Get or create timesheet
                    var timeSheet = await GetOrCreateTimeSheet(employeeId, bulkUploadDto.TimeSheetId, month, year);
                    if (timeSheet == null) return ResultOutputData.GenearetResultOutputCatch();

                    // Process Excel entries - updates existing or adds new ones
                    var (entries, errorCount, errorMessages) = await ProcessExcelEntries(worksheet, timeSheet.Id, startRow);
                    if (entries.Count == 0) return ResultOutputData.GenearetResultOutputNoDataReturned();

                    // Save entries
                    var saveResult = await SaveAndReturnResult(entries, errorCount, errorMessages, employeeId, timeSheet.Id);

                    if (!saveResult.Header.Success)
                        return saveResult;

                    // Mark timesheet as approved
                    timeSheet.IsApproved = true;
                    _context.TimeSheets.Update(timeSheet);

                    var employee = await _context.CompanyEmployees
                        .Include(e => e.VacationBalance)
                        .FirstOrDefaultAsync(e => e.Id == employeeId);

                    if (employee == null)
                        return resultOutputData.GenearetResultOutput("Employee not found", 0);

                    var currentYear = DateTime.Now.Year;

                    var vacationBalance = employee.VacationBalance ??
                        await _context.VacationBalances.FirstOrDefaultAsync(vb =>
                            vb.CompanyEmployeeId == employee.Id && vb.Year == currentYear);

                    if (vacationBalance == null)
                    {
                        _logger.LogWarning($"No vacation balance found for employee {employee.Id} in year {currentYear}");
                        return resultOutputData.GenearetResultOutput("Vacation balance not found", 0);
                    }

                    // Fetch saved entries
                    var finalTimeSheet = await _context.TimeSheets
                        .Include(t => t.Entries)
                        .FirstOrDefaultAsync(t => t.Id == timeSheet.Id);

                    var annualTaken = finalTimeSheet.Entries.Count(e => e.Type == DayType.Annual);
                    var sickTaken = finalTimeSheet.Entries.Count(e => e.Type == DayType.Sick);
                    var overtime = finalTimeSheet.Entries.Sum(e => e.OverTimeInHours);


                    if (overtime > 0)
                    {
                        vacationBalance.OvertimeHours += (int)overtime;
                    }

                        if (annualTaken > 0)
                    {
                        var before = vacationBalance.AnnualBalance;
                        vacationBalance.AnnualBalance -= annualTaken;
                        var after = vacationBalance.AnnualBalance;

                        await _context.VacationBalanceHistories.AddAsync(new VacationBalanceHistory
                        {
                            CompanyEmployeeId = employee.Id,
                            VacationBalanceId = vacationBalance.Id,
                            ChangeInBalance = -annualTaken,
                            AdjustmentReason = $"Timesheet #{timeSheet.Id} - Annual Leave",
                            BalanceBeforeAdjustment = before,
                            BalanceAfterAdjustment = after,
                            AdjustedBy = "Admin",
                            AdjustmentDate = DateTime.Now
                        });

                        await _context.VacationBalanceAdjustments.AddAsync(new VacationBalanceAdjustment
                        {
                            VacationBalanceId = vacationBalance.Id,
                            AdjustmentAmount = -annualTaken,
                            AdjustmentReason = $"Timesheet #{timeSheet.Id} - Annual Leave",
                            AdjustedBy = "Admin",
                            AdjustmentDate = DateTime.Now,
                            Status = "Approved"
                        });
                    }

                    if (sickTaken > 0)
                    {
                        var before = vacationBalance.SickBalance;
                        vacationBalance.SickBalance -= sickTaken;
                        var after = vacationBalance.SickBalance;

                        await _context.VacationBalanceHistories.AddAsync(new VacationBalanceHistory
                        {
                            CompanyEmployeeId = employee.Id,
                            VacationBalanceId = vacationBalance.Id,
                            ChangeInBalance = -sickTaken,
                            AdjustmentReason = $"Timesheet #{timeSheet.Id} - Sick Leave",
                            BalanceBeforeAdjustment = before,
                            BalanceAfterAdjustment = after,
                            AdjustedBy = "Admin",
                            AdjustmentDate = DateTime.Now
                        });

                        await _context.VacationBalanceAdjustments.AddAsync(new VacationBalanceAdjustment
                        {
                            VacationBalanceId = vacationBalance.Id,
                            AdjustmentAmount = -sickTaken,
                            AdjustmentReason = $"Timesheet #{timeSheet.Id} - Sick Leave",
                            AdjustedBy = "Admin",
                            AdjustmentDate = DateTime.Now,
                            Status = "Approved"
                        });
                    }

                    vacationBalance.LastUpdatedDate = DateTime.Now;
                    _context.VacationBalances.Update(vacationBalance);

                    await _context.SaveChangesAsync();


                    return resultOutputData.GenearetResultOutput($"Timesheet #{timeSheet.Id} saved and verified successfully", 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying timesheet for employee {EmployeeId}", employeeId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


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

        private async Task<TimeSheet?> GetOrCreateTimeSheet(int CompanyEmployeeId, int timeSheetId, int month, int year)
        {
            // Check if timesheet already exists for this employee/month/year
            var existingTimeSheet = await _context.TimeSheets
                .FirstOrDefaultAsync(t => t.CompanyEmployeeId == CompanyEmployeeId &&
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
                CompanyEmployeeId = CompanyEmployeeId,
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
            const string overTimeColumn = "E";

            for (int row = startRow; row <= endRow; row++)
            {
                try
                {
                    var dateCell = worksheet.Cell($"{dateColumn}{row}");
                    var workModeCell = worksheet.Cell($"{workModeColumn}{row}");
                    var overTimeColumnCell = worksheet.Cell($"{overTimeColumn}{row}");

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
                    var overtime = overTimeColumnCell.IsEmpty()? 0 : overTimeColumnCell.GetValue<int>();
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
                        existingEntry.OverTimeInHours = overtime;
                        entries.Add(existingEntry);
                    }
                    else
                    {
                        // Create new entry
                        var newEntry = new TimeSheetEntry
                        {
                            TimeSheetId = timeSheetId,
                            Date = date,
                            OverTimeInHours = overtime,
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
            int CompanyEmployeeId, int timeSheetId)
        {

            var newEntries = entries.Where(e => e.Id == 0).ToList(); // Assuming Id is 0 for new
            await _context.TimeSheetEntries.AddRangeAsync(newEntries);
            await _context.SaveChangesAsync();
            // We don't need to AddRangeAsync here because:
            // - New entries will be added automatically when we save changes
            // - Existing entries are already tracked by EF Core

            var result = new
            {
                CompanyEmployeeId = CompanyEmployeeId,
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


        private async Task<(User? user, CompanyEmployee? employee)> ProcessUserAndEmployee(
      string userId, int? annualBalance, int? sickBalance)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (null, null);

            var employee = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
     .Include(_context.CompanyEmployees, e => e.VacationBalance)
     .FirstOrDefaultAsync(e => e.UserId == userId);


            if (employee == null)
            {
                employee = CreateNewEmployee(user, annualBalance, sickBalance);
                await _context.CompanyEmployees.AddAsync(employee);
                await _context.SaveChangesAsync();
            }
            else if (employee.VacationBalance == null)
            {
                employee.VacationBalance = CreateVacationBalance(annualBalance, sickBalance);
                _context.CompanyEmployees.Update(employee);
                await _context.SaveChangesAsync();
            }

            return (user, employee);
        }
        private CompanyEmployee CreateNewEmployee(User user, int? annualBalance, int? sickBalance)
        {
            return new CompanyEmployee
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
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
                var query = _context.CompanyEmployees
    .Include(e => e.ManagedTimeSheets)
    .ThenInclude(t => t.Entries)
                .AsQueryable();

                if (department != null)
                {
                    query = query.Where(e => e.Department == department);
                }

                var employees = await query.ToListAsync();


                var result = employees.Select(e => new
                {
                    CompanyEmployeeId = e.Id,
                    EmployeeName = e.FullName,
                    Department = e.Department,
                    Timesheets = e.ManagedTimeSheets?
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

        public async Task<OperationOutput> GetTimesheetComplianceReport(int month, int year, string? managerId = null)
        {
            try
            {
                var cutoffDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1); // End of month

                var query =
                           _context.CompanyEmployees.Include(e => e.ManagedTimeSheets).Include(e => e.VacationBalance);

                // Filter by manager if specified


                var employees = await query.ToListAsync();

                var reportData = new
                {
                    ReportPeriod = $"{month}/{year}",
                    TotalEmployees = employees?.Count,
                    EmployeesWithTimesheets = employees.Count(e => e.ManagedTimeSheets?.Any(t => t.Month == month && t.Year == year) ?? false),
                    ComplianceStats = new
                    {
                        OnTimeSubmissions = employees.Count(e =>
                            e.ManagedTimeSheets?.Any(t =>
                                t.Month == month &&
                                t.Year == year &&
                                t.CreatedDate <= cutoffDate) ?? false),
                        LateSubmissions = employees.Count(e =>
                            e.ManagedTimeSheets?.Any(t =>
                                t.Month == month &&
                                t.Year == year &&
                                t.CreatedDate > cutoffDate) ?? false),
                        PendingApproval = employees.Count(e =>
                            e.ManagedTimeSheets?.Any(t =>
                                t.Month == month &&
                                t.Year == year) ?? false),
                        MissingSubmissions = employees.Count(e =>
                            !e.ManagedTimeSheets?.Any(t =>
                                t.Month == month &&
                                t.Year == year) ?? true)
                    },
                    EmployeeDetails = employees.Select(e => new
                    {
                        e.Id,
                        e.FullName,
                        e.Department,
                        SubmissionDate = e.ManagedTimeSheets?
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

        public async Task<List<string>> GetCompanyEmployeesManagers()
        {
            try
            {
                var managers = await _context.CompanyEmployees
                    .Where(e => e.DirectManagerUserId != null).Select(e => e.DirectManagerUserId).Distinct()
                    .ToListAsync();
                // get all employees in roles in approval configurations 
                var roles = await _context.ApprovalConfigurations
                   .Where(e => e.ApproverRole != null).Select(e => e.ApproverRole).Distinct()
                   .ToListAsync();
                var others = await _context.CompanyEmployees
                    .Where(e => e.UserId != null && roles!.Contains(e.Role!)).Select(e => e.UserId).Distinct()
                    .ToListAsync();

                managers.AddRange(others);
                return managers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees by company");
                return new List<string>();
            }
        }

        public async Task<bool> IsCompanyEmployee(string userId)
        {
            try
            {
                return await _context.CompanyEmployees.AnyAsync(e => e.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} is a company employee", userId);
                return false;
            }
        }

        public async Task<PortalEntryResult> GetAllowedPortalEntries(string userId)
        {
            try
            {
                var result = new PortalEntryResult
                {
                    Entries = string.Empty,
                    EmployeeCompanyId = 0,
                    CompanyId = 0
                };

                // Load user and employee info
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var employee = await _context.CompanyEmployees.FirstOrDefaultAsync(e => e.UserId == userId);

                // Admin check from user record
                if (user != null)
                {
                    int groupid =  _context.Groups.FirstOrDefault(g => g.NameEn == "OutSourceAdmin").Id;
                    var adminUsersConfig = _context.GroupUsers
                    .Where(g => g.GroupId == groupid)
                    .Select(g => g.UserId.ToLower());

                    if (adminUsersConfig != null)
                    {
                        if (adminUsersConfig.Any(User => User.ToLower() == user.Id.ToLower())) // Compare both as lowercase
                        {
                            AppendEntry(result, "admin");
                        }
                    }
                }

                // If not an employee, return early with current result
                if (employee == null)
                {
                    result.Entries = string.IsNullOrEmpty(result.Entries) ? "None" : result.Entries;
                    return result;
                }

                // Set common info
                result.EmployeeCompanyId = employee.Id;
                result.CompanyId = employee.CompanyId;

                // Company contact
                if (employee.IsPrimaryContact == true)
                {
                    AppendEntry(result, "company");
                }

                // Nupco employee logic
                if (employee.IsNupcoEmployee == true)
                {
                    var isManager = await _context.CompanyEmployees.AnyAsync(e => e.DirectManagerUserId == userId);
                    if (isManager)
                    {
                        // Manager role overrides others
                        return new PortalEntryResult
                        {
                            Entries = "manager",
                            EmployeeCompanyId = employee.Id,
                            CompanyId = employee.CompanyId
                        };
                    }

                    AppendEntry(result, "employee");
                }

                // Final fallback if nothing matched
                if (string.IsNullOrWhiteSpace(result.Entries))
                {
                    result.Entries = "None";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining allowed portal entries for user {UserId}", userId);
                return null;
            }
        }

        private void AppendEntry(PortalEntryResult result, string entry)
        {
            if (string.IsNullOrWhiteSpace(result.Entries))
            {
                result.Entries = entry;
            }
            else if (!result.Entries.Split(',').Contains(entry))
            {
                result.Entries += $",{entry}";
            }
        }

        public async Task<List<CompanyEmployeeUserDto>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive && !u.IsDeleted)
                    .Select(u => new CompanyEmployeeUserDto
                    {
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        DepartmentName = u.DepartmentName,
                        Email = u.Email,
                        Id = u.Id
                    })
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees by company");
                return new List<CompanyEmployeeUserDto>();
            }
        }
        public async Task<AssignManagerResult> AssignManagerAsync(int employeeId, string managerUserId)
        {
            var employee = await _context.CompanyEmployees
                .FirstOrDefaultAsync(e => e.Id == employeeId)
                .ConfigureAwait(false);

            if (employee == null)
            {
                return new AssignManagerResult
                {
                    Success = false,
                    Message = $"Employee with ID {employeeId} not found."
                };
            }

            var manager = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == managerUserId)
                .ConfigureAwait(false);

            if (manager == null)
            {
                return new AssignManagerResult
                {
                    Success = false,
                    Message = $"Manager with User ID {managerUserId} not found."
                };
            }

            employee.DirectManagerUserId = manager.Id;
            employee.DirectManagerName = $"{manager.FirstName} {manager.LastName}";
            employee.DirectManagerEmail = manager.Email;

            // Check if manager exists in CompanyEmployees table
            var managerExistsInCompany = await _context.CompanyEmployees
                .AnyAsync(e => e.CompanyId == employee.CompanyId && e.UserId == managerUserId)
                .ConfigureAwait(false);

            if (!managerExistsInCompany)
            {
                var newManagerEmployee = new CompanyEmployee
                {
                    CompanyId = employee.CompanyId,
                    UserId = manager.Id,
                    FullName = employee.DirectManagerName,
                    Email = manager.Email,
                    Role = "Direct Manager",
                    DirectManagerUserId = null,
                    DirectManagerName = null,
                    DirectManagerEmail = null,
                    IsPrimaryContact = false,
                    IsNupcoEmployee = true,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "AssignManagerAsync",
                    IsDeleted = false
                };
                await _context.CompanyEmployees.AddAsync(newManagerEmployee).ConfigureAwait(false);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);

            return new AssignManagerResult
            {
                Success = true,
                Message = $"Manager '{manager.FirstName} {manager.LastName}' assigned to employee ID {employeeId}."
            };
        }

        public async Task<OperationOutput> AssignRole(int employeeId, string role)
        {
            var employee = await _context.CompanyEmployees
                .FirstOrDefaultAsync(e => e.Id == employeeId)
                .ConfigureAwait(false);

            employee.Role = role;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            ResultOutputData result = new ResultOutputData();
            return result.GenearetResultOutput(employee, 1);

        }

        public async Task<List<string>> GetRolesForCompany(int CompanyId)
        {
            var Roles = await _context.ApprovalConfigurations.Where(e => e.CompanyId == CompanyId && e.IsDeleted == false && e.IsActive == true).Select(e => e.ApproverRole).ToListAsync();
            return Roles;
        }



        public async Task<OperationOutput> GetOutsourcedEmployeeAttendance(string managerId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get all timesheets for outsourced employees managed by this manager within date range
                var timesheets = await _context.TimeSheets
                    .Include(ts => ts.CompanyEmployee)
                    .Include(ts => ts.Entries)
                    .Where(ts =>
                        ts.CompanyEmployee.DirectManagerUserId == managerId &&
                        ts.IsDeleted == false &&
                        ts.Entries.Any(e => e.Date >= startDate && e.Date <= endDate))
                    .ToListAsync();

                dynamic result = new List<OutsourcedEmployeeAttendanceDto>();

                foreach (var timesheet in timesheets)
                {
                    // Filter entries within date range and order by date
                    var entries = timesheet.Entries
                        .Where(e => e.Date >= startDate && e.Date <= endDate)
                        .OrderBy(e => e.Date)
                        .ToList();

                    // Calculate working statistics
                    var workingDays = entries.Count(e => e.Type == DayType.WorkingDay);
                    var totalHours = workingDays * 8; // Assuming 8-hour workday
                    var expectedHours = workingDays * 8;

                    dynamic employeeAttendance = new OutsourcedEmployeeAttendanceDto
                    {
                        EmployeeId = timesheet.CompanyEmployeeId ?? 0,
                        EmployeeName = timesheet.CompanyEmployee?.FullName ?? "Unknown",
                        Department = timesheet.CompanyEmployee?.Department ?? "N/A",
                        TotalWorkingDays = workingDays,
                        TotalHours = totalHours,
                        ExpectedHours = expectedHours,
                        AttendanceRecords = entries.Select(e => new AttendanceRecordDto
                        {
                            Date = e.Date ?? DateTime.MinValue,
                            DayType = e.Type?.ToString() ?? "Unknown",
                            Status = GetAttendanceStatus(timesheet, e),
                            CreatedDate = e.CreatedDate,
                            UpdatedDate = e.UpdatedDate
                        }).ToList()
                    };

                    result.Add(employeeAttendance);
                }
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(result, result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outsourced employee attendance");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        private string GetAttendanceStatus(TimeSheet T , TimeSheetEntry entry)
        {
            if (entry.Type != DayType.WorkingDay)
                return entry.Type?.ToString() ?? "Unknown";

            // For working days, check if it was updated (meaning attended)
            return T.IsApproved == true ? "Completed" : "Pending";
        }


        public async Task<OperationOutput> GetCurrentOutsourceEmployeeAttendance(string userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var timesheets = await _context.TimeSheets
                    .Include(ts => ts.CompanyEmployee)
                    .Include(ts => ts.Entries)
                    .Where(ts =>
                        ts.CompanyEmployee.UserId == userId &&
                        ts.IsDeleted == false &&
                        ts.Entries.Any(e => e.Date >= startDate && e.Date <= endDate))
                    .ToListAsync();

                var result = new List<OutsourcedEmployeeAttendanceDto>();

                foreach (var timesheet in timesheets)
                {
                    var entries = timesheet.Entries
                        .Where(e => e.Date >= startDate && e.Date <= endDate)
                        .OrderBy(e => e.Date)
                        .ToList();

                    var employeeAttendance = new OutsourcedEmployeeAttendanceDto
                    {
                        EmployeeId = timesheet.CompanyEmployeeId ?? 0,
                        EmployeeName = timesheet.CompanyEmployee?.FullName ?? "Unknown",
                        Department = timesheet.CompanyEmployee?.Department ?? "N/A",
                        AttendanceRecords = entries.Select(e => new AttendanceRecordDto
                        {
                            Date = e.Date ?? DateTime.MinValue,
                            DayType = e.Type?.ToString() ?? "Unknown",
                            Status = GetAttendanceStatus(timesheet,e),
                            CreatedDate = e.CreatedDate,
                            UpdatedDate = e.UpdatedDate
                        }).ToList()
                    };

                    result.Add(employeeAttendance);
                }

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(result, result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current outsourced employee attendance");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
    }
    public class OutsourcedEmployeeAttendanceDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public int TotalWorkingDays { get; set; }
        public decimal TotalHours { get; set; }
        public decimal ExpectedHours { get; set; }
        public List<AttendanceRecordDto> AttendanceRecords { get; set; } = new();
    }

    public class AttendanceRecordDto
    {
        public DateTime Date { get; set; }
        public string DayType { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

}
