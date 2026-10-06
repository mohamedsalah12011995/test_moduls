using Core.Helpers;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.AttendanceTransaction;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models_Attendance;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace Nupco.EF.Repositories
{
    internal class Iclock_transactionRepository : BaseRepository<AttendanceTransaction>, IIclock_transactionRepository
    {
        private readonly ApplicationDbContextAttendance _contextAttendance;
        private readonly ILogger _logger;
        private readonly AttendanceTransaction_Assembler _attendanceTransaction_Assembler;

        public Iclock_transactionRepository(ApplicationDbContext context, ApplicationDbContextAttendance contextAttendance, ILogger logger) : base(context, logger)
        {
            _attendanceTransaction_Assembler = new AttendanceTransaction_Assembler();
            _logger = logger;
            _context = context;
            _contextAttendance = contextAttendance;

        }


        //public async Task<OperationOutput> CheckInTransaction1(transactionParam model)
        //{
        //    try
        //    {

        //        var _transactionUsers = await _context.AttendanceTransactions
        //            .Where(i => i.AreaId == 1 && i.LogType == "in")
        //            .Include(i => i.User)
        //            .ToListAsync();
        //        int index = 0;
        //        if (_transactionUsers.Count > 0)
        //        {
        //            foreach (var item in _transactionUsers)
        //            {
        //                var _model = new Iclock_transaction
        //                {
        //                    emp_code = item.User.Code,
        //                    punch_time = item.CreatedDate.Value,
        //                    punch_state = "0",
        //                    verify_type = 15,
        //                    work_code = "0",
        //                    terminal_sn = "CNT7214260133",
        //                    terminal_alias = "F10_Ent2_In",
        //                    area_alias = "General Area",
        //                    longitude = null,
        //                    latitude = null,
        //                    gps_location = null,
        //                    mobile = null,
        //                    source = 1,
        //                    purpose = 9,
        //                    crc = "AABAEACAFACABABAAAFA",
        //                    is_attendance = null,
        //                    reserved = null,
        //                    upload_time = item.CreatedDate.Value,
        //                    sync_status = null,
        //                    sync_time = null,
        //                    emp_id = null,
        //                    terminal_id = 11,
        //                    is_mask = 255,
        //                    temperature = 255

        //                };
        //                try
        //                {
        //                    var obj = _contextAttendance.iclock_transaction.Where(i => i.punch_time.Date.Date == item.CreatedDate.Value.Date.Date && i.punch_time.Hour == item.CreatedDate.Value.Hour && i.punch_time.Minute == item.CreatedDate.Value.Minute && i.emp_code == item.User.Code).FirstOrDefault();
        //                    if (obj == null) { index += 1; await _contextAttendance.iclock_transaction.AddAsync(_model); }


        //                }
        //                catch (Exception)
        //                {
        //                    continue;
        //                }
        //            }
        //            await _contextAttendance.SaveChangesAsync();

        //        }



        //        var Result = ResultOutputData.GenearetResultOutputSuccess();
        //        return Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        var Result = ResultOutputData.GenearetResultOutputCatch();
        //        return Result;
        //    }


        //}



        //public async Task<OperationOutput> CheckOutTransaction2(transactionParam model)
        //{
        //    try
        //    {

        //        var _transactionUsers = await _context.AttendanceTransactions
        //            .Where(i => i.AreaId == 1 && i.LogType == "out")
        //            .Include(i => i.User)
        //            .ToListAsync();
        //        var index = 0;
        //        if (_transactionUsers.Count > 0)
        //        {
        //            foreach (var item in _transactionUsers)
        //            {
        //                var _model = new Iclock_transaction
        //                {
        //                    emp_code = item.User.Code,
        //                    punch_time = item.CreatedDate.Value,
        //                    punch_state = "0",
        //                    verify_type = 15,
        //                    work_code = "0",
        //                    terminal_sn = "CNT7214260107",
        //                    terminal_alias = "F10_Ent2_Out",
        //                    area_alias = "General Area",
        //                    longitude = null,
        //                    latitude = null,
        //                    gps_location = null,
        //                    mobile = null,
        //                    source = 1,
        //                    purpose = 9,
        //                    crc = "AABAEADADAEABABAAAFA",
        //                    is_attendance = null,
        //                    reserved = null,
        //                    upload_time = item.CreatedDate.Value,
        //                    sync_status = null,
        //                    sync_time = null,
        //                    emp_id = null,
        //                    terminal_id = 10,
        //                    is_mask = 255,
        //                    temperature = 255

        //                };
        //                try
        //                {
        //                    var obj = _contextAttendance.iclock_transaction.Where(i => i.punch_time.Date.Date == item.CreatedDate.Value.Date.Date && i.punch_time.Hour == item.CreatedDate.Value.Hour && i.punch_time.Minute == item.CreatedDate.Value.Minute && i.emp_code == item.User.Code).FirstOrDefault();
        //                    if (obj == null) { index += 1; await _contextAttendance.iclock_transaction.AddAsync(_model); }


        //                }
        //                catch (Exception)
        //                {
        //                    continue;
        //                }



        //            }
        //            await _contextAttendance.SaveChangesAsync();

        //        }

        //        var Result = ResultOutputData.GenearetResultOutputSuccess();
        //        return Result;
        //    }
        //    catch (Exception)
        //    {
        //        var Result = ResultOutputData.GenearetResultOutputCatch();
        //        return Result;
        //    }

        //}

        public async Task<OperationOutput> CheckInTransaction(transactionParam model)
        {
            try
            {

                var _model = new Iclock_transaction
                {
                    emp_code = model.emp_code,
                    punch_time = DateTime.Now,
                    punch_state = "0",
                    verify_type = 15,
                    work_code = "0",
                    terminal_sn = "CMZJ221160096",
                    terminal_alias = "Mob_Digital_In",
                    area_alias = "General Area",
                    longitude = null,
                    latitude = null,
                    gps_location = null,
                    mobile = null,
                    source = 1,
                    purpose = 9,
                    crc = "AABACAIACABABABAAAFA",
                    is_attendance = null,
                    reserved = null,
                    upload_time = DateTime.Now,
                    sync_status = null,
                    sync_time = null,
                    emp_id = null,
                    terminal_id = 30,
                    is_mask = 255,
                    temperature = decimal.Parse("255")

                };

                await _contextAttendance.iclock_transaction.AddAsync(_model);
                await _contextAttendance.SaveChangesAsync();



                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<List<AttendanceTransactionPerDayDto>> GetAttendanceTransactionByEmployeeInOut(transactionParamInOut model)
        {
            try
            {
                var transactions = new List<AttendanceTransactionInOut>();

                var startDate = Dates.ConvertStringToDate(model.fromDate);
                var endDate = Dates.ConvertStringToDate(model.toDate);
                var user = await _context.Users.AsNoTracking().Include(i => i.WorkingArea).FirstOrDefaultAsync(i => i.Code == model.emp_code);
                var areaLaValle = await _context.Areas.AsNoTracking().FirstOrDefaultAsync(i => i.Id == 22);
                var areaDigitalCity = await _context.Areas.AsNoTracking().FirstOrDefaultAsync(i => i.Id == 1);

                var workingAreaAr = user?.WorkingArea?.NameAr;
                var workingAreaEn = user?.WorkingArea?.NameEn;
                var iclock_transactionList = await _contextAttendance.iclock_transaction.AsNoTrackingWithIdentityResolution()
                          .Where(i => i.emp_code == model.emp_code && i.punch_time.Date >= startDate.Value.Date && i.punch_time.Date <= endDate.Value.Date)
                          .Select(s => new AttendanceTransactionInOut
                          {
                              AreaNameAr = GetArea(s.terminal_alias, areaLaValle, areaDigitalCity, 0),
                              AreaNameEn = GetArea(s.terminal_alias, areaLaValle, areaDigitalCity, 1),
                              CityNameEn = "Riyadh",
                              CityNameAr = "الرياض",
                              CreatedDate = s.punch_time,
                              EmpCode = model.emp_code,
                              WorkingAreaAr = workingAreaAr,
                              WorkingAreaEn = workingAreaEn,
                              LogType = s.terminal_alias,
                              PunchMethod = "Access Card"
                          }).ToListAsync();

                transactions.AddRange(iclock_transactionList);


                if (user != null)
                {

                    var _AttendanceTransactions = await _context.AttendanceTransactions.AsNoTrackingWithIdentityResolution().Include(i => i.Area).Include(i => i.Area.City)
                                                .Where(i => i.User.Code == model.emp_code && i.CreatedDate.Value.Date >= startDate.Value.Date && i.CreatedDate.Value.Date <= endDate.Value.Date)
                                                                                          .Select(s => new AttendanceTransactionInOut
                                                                                          {
                                                                                              AreaNameAr = s.Area.NameAr,
                                                                                              AreaNameEn = s.Area.NameEn,
                                                                                              CityNameEn = s.Area.City.NameEn,
                                                                                              CityNameAr = s.Area.City.NameAr,
                                                                                              CreatedDate = s.CreatedDate,
                                                                                              EmpCode = user.Code,
                                                                                              WorkingAreaAr = workingAreaAr,
                                                                                              WorkingAreaEn = workingAreaEn,
                                                                                              LogType = s.LogType,
                                                                                              PunchMethod = "Application"

                                                                                          }).ToListAsync();

                    transactions.AddRange(_AttendanceTransactions);


                }

                // Combine and group transactions
                var combinedTransactions = transactions
                    .OrderBy(t => t.CreatedDate)
                    .GroupBy(t => new { t.CreatedDate.Value.Date, t.EmpCode })
                    .Select(g => new AttendanceTransactionPerDayDto
                    {
                        ActionDate = g.Key.Date,
                        EmpCode = g.Key.EmpCode,
                        Tran_in = g.FirstOrDefault(), // First entry as Tran_in
                        Tran_out = g.Skip(1).LastOrDefault() // Last entry as Tran_out if more than one
                    })
                    .ToList();
                return combinedTransactions;
     

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance transactions for emp_code: {EmpCode}", model.emp_code);

                return null;
            }

        }

        private static string GetArea(string? terminal_alias, Area? areaLaValle, Area? areaDigitalCity, int lang)
        {
            switch (lang)
            {
                case 0:
                    return terminal_alias.ToLower().Contains("LaValle".ToLower()) ? areaLaValle != null ? areaLaValle?.NameAr : "" : areaDigitalCity != null ? areaDigitalCity?.NameAr : "";
                case 1:
                    return terminal_alias.ToLower().Contains("LaValle".ToLower()) ? areaLaValle != null ? areaLaValle?.NameEn : "" : areaDigitalCity != null ? areaDigitalCity?.NameEn : "";
                default:
                    return "";

            }
        }


        public async Task<OperationOutput> CheckOutTransaction(transactionParam model)
        {
            try
            {

                var _model = new Iclock_transaction
                {
                    emp_code = model.emp_code,
                    punch_time = DateTime.Now,
                    punch_state = "1",
                    verify_type = 15,
                    work_code = "0",
                    terminal_sn = "CNT7214260115",
                    terminal_alias = "Mob_Digital_Out",
                    area_alias = "General Area",
                    longitude = null,
                    latitude = null,
                    gps_location = null,
                    mobile = null,
                    source = 1,
                    purpose = 9,
                    crc = "AABADADAFAEABABAAAFA",
                    is_attendance = null,
                    reserved = null,
                    upload_time = DateTime.Now,
                    sync_status = null,
                    sync_time = null,
                    emp_id = null,
                    terminal_id = 17,
                    is_mask = 255,
                    temperature = decimal.Parse("255")

                };

                await _contextAttendance.iclock_transaction.AddAsync(_model);
                await _contextAttendance.SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }
        public async Task<OperationOutput> GetAllByPagenation(FiltersBy _filter)
        {
            try
            {
                var startDate = Dates.ConvertStringToDate(_filter.fromDate);
                var endDate = Dates.ConvertStringToDate(_filter.toDate);

                int pageNumber = _filter.pageNumber ?? 0;
                int pageSize = _filter.pageSize ?? 10;

                var spec = Specification<AttendanceTransaction>.All
                    .And(new AttendanceTransactionSpecification(_filter));

                var baseQuery = _context.AttendanceTransactions
                    .AsNoTracking()
                    .Where(spec.ToExpression())
                    .Where(x => x.CreatedDate != null);

                if (startDate.HasValue && endDate.HasValue)
                    baseQuery = baseQuery.Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate);

                // Optimized query: Get minimal data first, then group in memory to avoid correlated subqueries
                var transactions = await baseQuery
                    .Select(x => new
                    {
                        x.Id,
                        x.UserId,
                        x.CreatedDate,
                        Date = x.CreatedDate.Value.Date
                    })
                    .OrderBy(x => x.UserId)
                    .ThenBy(x => x.CreatedDate)
                    .ToListAsync();

                // Group in memory - much faster than correlated subqueries
                var groupedData = transactions
                    .GroupBy(t => new { t.UserId, t.Date })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        g.Key.Date,
                        Count = g.Count(),
                        CheckInId = g.OrderBy(x => x.CreatedDate).First().Id,
                        CheckOutId = g.Count() > 1
                            ? g.OrderByDescending(x => x.CreatedDate).First().Id
                            : (int?)null
                    })
                    .OrderBy(g => g.UserId)
                    .ThenBy(g => g.Date)
                    .ToList();

                var totalDays = groupedData.Count;

                // Get the IDs for the current page
                var pageGroupedData = groupedData
                    .Skip(pageNumber * pageSize)
                    .Take(pageSize)
                    .ToList();

                var allIds = pageGroupedData
                    .SelectMany(g => g.CheckOutId.HasValue
                        ? new[] { g.CheckInId, g.CheckOutId.Value }
                        : new[] { g.CheckInId })
                    .Distinct()
                    .ToList();

                // Now fetch full data only for the current page
                var pagedData = await _context.AttendanceTransactions
                    .AsNoTracking()
                    .Include(x => x.User)
                    .Include(x => x.Area).ThenInclude(c => c.City)
                    .Where(x => allIds.Contains(x.Id))
                    .OrderByDescending(x => x.CreatedDate)
                    .ThenBy(x => x.UserId)
                    .ToListAsync();

                var resultData = pagedData.Select(x => new AttendanceTransactionDto
                {
                    Id = x.Id,
                    AreaId = x.AreaId ?? 0,
                    AreaNameAr = x.Area?.NameAr,
                    AreaNameEn = x.Area?.NameEn,
                    CityNameAr = x.Area?.City?.NameAr,
                    CityNameEn = x.Area?.City?.NameEn,
                    CreatedDate = x.CreatedDate.Value,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Distance = x.Distance,
                    UserName = x.User?.UserName,
                    EmpCode = x.User?.Code,
                    LogType = x.LogType == "in" ? "in" : "out",
                    FullName = x.User?.FirstName + " " + x.User?.LastName,
                })
                    .OrderByDescending(o => o.CreatedDate).ThenBy(o => o.UserName)
                    .ToList();

                var result = new ResultOutputData().GenearetResultOutput(resultData, totalDays);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error in GetAllByPagenation: {ex.Message}");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

    }
}
