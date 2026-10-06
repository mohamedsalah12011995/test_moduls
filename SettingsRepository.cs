using ClosedXML;
using Core.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using GeoCoordinatePortable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Advertisements.Repository;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Advertisements;
using Nupco.Core.Dto.Statistic;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models_Attendance;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LoggerExtensions = Nupco.Core.Helpers.LoggerExtensions;


namespace Nupco.EF.Repositories
{
    internal class SettingsRepository : BaseRepository<Setting>, ISettingsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly Settings_Assembler _setting_Assembler = new Settings_Assembler();
        private readonly ApplicationDbContextAttendance _contextAttendance;
        private readonly Iclock_transaction_Assembler _iclock_transaction_Assembler = new Iclock_transaction_Assembler();

        public SettingsRepository(ApplicationDbContext context, ILogger logger, ApplicationDbContextAttendance contextAttendance) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _contextAttendance = contextAttendance;
        }
        public async Task<OperationOutput> CreateSettings(SettingdObjDto setting)
        {
            try
            {
                var _settings = _setting_Assembler.WriteDal(setting);

                var entity = AddAsync(_settings);
                await SaveChangesAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entity, 1);
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> EditSettings(SettingdObjDto setting)
        {
            try
            {

                var find = await _context.Settings.FirstOrDefaultAsync(f => f.Id == setting.Id);
                if (find != null)
                {
                    find.Key = setting.Key;
                    find.Value = setting.Value;
                    find.NameAr = setting.NameAr;
                    find.NameEn = setting.NameEn;
                    find.DataTypeId = setting.DataTypeId;
                    var entity = Update(find);
                    await SaveChangesAsync();

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(entity, 1);
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

        public async Task<OperationOutput> UpdateSettings(List<SettingsKeyValue> settings, string _serverApiKey, IUnitOfWork _unitOfWork)
        {
            try
            {
                foreach (var setting in settings)
                {
                    var find = await _context.Settings.FirstOrDefaultAsync(f => f.Key == setting.key);
                    if (find != null)
                    {
                        find.Value = setting.value.ToString();
                        var entity = _context.Settings.Update(find);
                        await _context.SaveChangesAsync();

                        if (setting.key == "allowAskCeo" && setting.value == "true")
                        {
                            await SendNotificationWhenActivationAskCeo(setting.key, setting.value, _serverApiKey, _unitOfWork);
                        }

                    }
                }


                var _result = ResultOutputData.GenearetResultOutputSuccess();
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> DeleteSettings(int id)
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


        public async Task<OperationOutput> ActivateSettingsKey(string key, string value, string _serverApiKey, IUnitOfWork _unitOfWork)

        {
            try
            {
                var find = await _context.Settings.FirstOrDefaultAsync(f => f.Key == key);
                if (find != null)
                {
                    find.Value = value;
                    var entity = _context.Settings.Update(find);
                    await _context.SaveChangesAsync();

                    if (key == "allowAskCeo" && value == "true")
                    {
                        await SendNotificationWhenActivationAskCeo(key, value, _serverApiKey, _unitOfWork);
                    }

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(entity.Entity, 1);
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

        private async Task<bool> SendNotificationWhenActivationAskCeo(string key, string value, string _serverApiKey, IUnitOfWork _unitOfWork)
        {
            return await Notifications.SendNotification(_unitOfWork, "askCeo", "", "اسأل الرئيس التنفيذي", "Ask CEO", "", "", _serverApiKey);
        }

        public async Task<OperationOutput> CheckInLocalTransaction(transactionArea model, string _path, string _fileName)
        {
            try
            {
                var hasAttCheckIn = await _context.AttendanceTransactions
                        .Where(u => u.CreatedDate.Value.Date == DateTime.Now.Date && u.LogType.ToLower() == "in" && u.UserId == model.userId).AnyAsync();

                var hasClockCheckIn = await _contextAttendance.iclock_transaction
                                      .Where(u => u.punch_time.Date == DateTime.Now.Date && u.emp_code == model.emp_code && u.terminal_alias.ToLower().Contains("in")).AnyAsync();

                if (hasClockCheckIn || hasAttCheckIn)
                {
                    return await CheckOutTransaction(model, _path, _fileName);
                }
                else
                {
                    var _Areas = _context.Areas.ToList();
                    bool isFound = false;
                    foreach (var _area in _Areas)
                    {
                        var sCoord = new GeoCoordinate(double.Parse(model.latitude), double.Parse(model.longitude));
                        var eCoord = new GeoCoordinate(double.Parse(_area.Latitude), double.Parse(_area.Longitude));
                        var rCoord = sCoord.GetDistanceTo(eCoord);

                        if (rCoord <= double.Parse(_area.Diameter))
                        {
                            var _modelAttendanceTransactions = new DAL.Models.AttendanceTransaction()
                            {
                                AreaId = _area.Id,
                                Distance = rCoord.ToString(),
                                Latitude = model.latitude,
                                Longitude = model.longitude,
                                UserId = model.userId,
                                CreatedDate = DateTime.Now,
                                LogType = "in"

                            };
                            await _context.AttendanceTransactions.AddAsync(_modelAttendanceTransactions);
                            await _context.SaveChangesAsync();


                            //var _AttendanceTransactions = await _context.AttendanceTransactions.FirstOrDefaultAsync(i => i.UserId == model.userId && i.CreatedDate.Value.Date == DateTime.Now.Date && i.LogType == "in");
                            //if (_AttendanceTransactions is null)
                            //{
                            //    await _context.AttendanceTransactions.AddAsync(_modelAttendanceTransactions);
                            //    await _context.SaveChangesAsync();
                            //}



                            isFound = true;
                            break;

                        }
                    }



                    if (isFound == true)
                    {
                        var Result = ResultOutputData.GenearetResultOutputSuccess();
                        return Result;
                    }
                    else
                    {
                        var message = $"Code_Emp :  {model.emp_code}  -   Latitude : {model.latitude}  -   longitude : {model.longitude}  -  typeLog  : in";
                        Core.Helpers.LoggerExtensions.CreateLog($"{_path}_CheckInLocalTransaction", $"{model.emp_code}_{_fileName}", message);


                        OperationOutput Result = new OperationOutput();

                        Result.Header = ApplicationOperation.OperationResult(Enums.ServiceMessages.TransactionSuccess);
                        Result.Header.Success = true;
                        Result.Header.Code = 200;
                        Result.Output = null;
                        return Result;
                    }


                }



            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> CheckInTransaction(transactionArea model, string _path, string _fileName)
        {
            try
            {
                var hasAttCheckIn = await _context.AttendanceTransactions
                        .Where(u => u.CreatedDate.Value.Date == DateTime.Now.Date && u.LogType.ToLower() == "in" && u.UserId == model.userId).AnyAsync();

                var hasClockCheckIn = await _contextAttendance.iclock_transaction
                                      .Where(u => u.punch_time.Date == DateTime.Now.Date && u.emp_code == model.emp_code && u.terminal_alias.ToLower().Contains("in")).AnyAsync();

                if (hasClockCheckIn || hasAttCheckIn)
                {
                    return await CheckOutTransaction(model, _path, _fileName);
                }
                else
                {
                    var _Areas = _context.Areas.Where(a => a.IsActive == true && a.IsDelete != true).ToList();
                    bool isFound = false;

                    foreach (var _area in _Areas)
                    {
                        var sCoord = new GeoCoordinate(double.Parse(model.latitude), double.Parse(model.longitude));
                        var eCoord = new GeoCoordinate(double.Parse(_area.Latitude), double.Parse(_area.Longitude));
                        var rCoord = sCoord.GetDistanceTo(eCoord);

                        if (rCoord <= double.Parse(_area.Diameter))
                        {
                            // Check if user has access to this area
                            var hasAccess = await UserHasAccessToArea(model.userId, _area.Id);

                            if (!hasAccess)
                            {
                                // Log unauthorized access attempt
                                var message = $"UNAUTHORIZED ACCESS - Code_Emp: {model.emp_code} - Area: {_area.NameEn} - User not authorized for this area";
                                LoggerExtensions.CreateLog(_path, _fileName, message);

                                // Skip this area and continue checking other areas
                                continue;
                            }

                            // Rest of your existing code for successful check-in...
                            var _modelAttendanceTransactions = new DAL.Models.AttendanceTransaction()
                            {
                                AreaId = _area.Id,
                                Distance = rCoord.ToString(),
                                Latitude = model.latitude,
                                Longitude = model.longitude,
                                UserId = model.userId,
                                CreatedDate = DateTime.Now,
                                LogType = "in"
                            };

                            await _context.AttendanceTransactions.AddAsync(_modelAttendanceTransactions);
                            await _context.SaveChangesAsync();

                            // Handle attendance system integration...
                            if (_area.HasAttendanceSystem == true)
                            {
                                if (_area.Id == 1 || _area.NameEn.ToLower() == "digital city")
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
                                        terminal_id = 71,
                                        is_mask = 255,
                                        temperature = decimal.Parse("255")

                                    };
                                    await _contextAttendance.iclock_transaction.AddAsync(_model);
                                }
                                else if (_area.NameEn.ToLower() == "New HQ".ToLower())
                                {
                                    var _model = new Iclock_transaction
                                    {
                                        emp_code = model.emp_code,
                                        punch_time = DateTime.Now,
                                        punch_state = "0",
                                        verify_type = 15,
                                        work_code = "0",
                                        terminal_sn = "CMZJ221160096",
                                        terminal_alias = "Mob_Emar_In",
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
                                        terminal_id = 71,
                                        is_mask = 255,
                                        temperature = decimal.Parse("255")

                                    };
                                    await _contextAttendance.iclock_transaction.AddAsync(_model);
                                }
                                else if (_area.NameEn.ToLower() == "Jeddah Office".ToLower())
                                {
                                    var _model = new Iclock_transaction
                                    {
                                        emp_code = model.emp_code,
                                        punch_time = DateTime.Now,
                                        punch_state = "0",
                                        verify_type = 15,
                                        work_code = "0",
                                        terminal_sn = "COVS231660105",
                                        terminal_alias = "Mob_Jeddah_In",
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
                                        terminal_id = 60,
                                        is_mask = 255,
                                        temperature = decimal.Parse("255")

                                    };
                                    await _contextAttendance.iclock_transaction.AddAsync(_model);
                                }
                                else
                                {
                                    var _model2 = new Iclock_transaction
                                    {
                                        emp_code = model.emp_code,
                                        punch_time = DateTime.Now,
                                        punch_state = "255",
                                        verify_type = 15,
                                        work_code = "0",
                                        terminal_sn = "SYZ8241700373",
                                        terminal_alias = "Mob_LaValle_IN",
                                        area_alias = "General Area",
                                        longitude = null,
                                        latitude = null,
                                        gps_location = null,
                                        mobile = null,
                                        source = 1,
                                        purpose = 9,
                                        crc = "AAIAFADACAGABABAAAHA",
                                        is_attendance = null,
                                        reserved = null,
                                        upload_time = DateTime.Now,
                                        sync_status = null,
                                        sync_time = null,
                                        emp_id = null,
                                        terminal_id = 43,
                                        is_mask = 255,
                                        temperature = decimal.Parse("255")

                                    };
                                    await _contextAttendance.iclock_transaction.AddAsync(_model2);
                                }
                            }
                            isFound = true;
                            break;
                        }
                    }

                    if (isFound == true)
                    {
                        var Result = ResultOutputData.GenearetResultOutputSuccess();
                        return Result;
                    }
                    else
                    {
                        var message = $"Code_Emp :  {model.emp_code}  -   Latitude : {model.latitude}  -   longitude : {model.longitude}  -  typeLog  : in - No accessible area found";
                        LoggerExtensions.CreateLog(_path, _fileName, message);

                        OperationOutput Result = new OperationOutput();
                        Result.Header = ApplicationOperation.OperationResult(Enums.ServiceMessages.TransactionSuccess);
                        Result.Header.Success = true;
                        Result.Header.Code = 200;
                        Result.Output = null;
                        return Result;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerExtensions.CreateLog(_path, _fileName, $"Exception in CheckInTransaction: {ex.Message}");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> CheckOutLocalTransaction(transactionArea model, string _path, string _fileName)
        {
            try
            {

                var hasAttCheckIn = await _context.AttendanceTransactions
                        .Where(u => u.CreatedDate.Value.Date == DateTime.Now.Date && u.LogType.ToLower() == "in" && u.UserId == model.userId).AnyAsync();

                var hasClockCheckIn = await _contextAttendance.iclock_transaction
                                      .Where(u => u.punch_time.Date == DateTime.Now.Date && u.emp_code == model.emp_code && u.terminal_alias.ToLower().Contains("in")).AnyAsync();

                if (hasClockCheckIn || hasAttCheckIn)
                {

                    var _Areas = _context.Areas.ToList();
                    bool isFound = false;
                    foreach (var _area in _Areas)
                    {
                        var sCoord = new GeoCoordinate(double.Parse(model.latitude), double.Parse(model.longitude));
                        var eCoord = new GeoCoordinate(double.Parse(_area.Latitude), double.Parse(_area.Longitude));
                        var rCoord = sCoord.GetDistanceTo(eCoord);

                        if (rCoord <= double.Parse(_area.Diameter))
                        {
                            var _modelAttendanceTransactions = new DAL.Models.AttendanceTransaction()
                            {
                                AreaId = _area.Id,
                                Distance = rCoord.ToString(),
                                Latitude = model.latitude,
                                Longitude = model.longitude,
                                UserId = model.userId,
                                CreatedDate = DateTime.Now,
                                LogType = "out"

                            };
                            await _context.AttendanceTransactions.AddAsync(_modelAttendanceTransactions);


                            //var _AttendanceTransactions = await _context.AttendanceTransactions.FirstOrDefaultAsync(i => i.UserId == model.userId && i.CreatedDate.Value.Date == DateTime.Now.Date && i.LogType == "out");
                            //if (_AttendanceTransactions is null)
                            //{
                            //    await _context.AttendanceTransactions.AddAsync(_modelAttendanceTransactions);
                            //}
                            //else
                            //{
                            //    _AttendanceTransactions.CreatedDate = DateTime.Now;
                            //    _context.AttendanceTransactions.Update(_AttendanceTransactions);
                            //}

                            await _context.SaveChangesAsync();


                            isFound = true;
                            break;

                        }
                    }



                    if (isFound == true)
                    {
                        var Result = ResultOutputData.GenearetResultOutputSuccess();
                        return Result;
                    }
                    else
                    {
                        var message = $"Code_Emp :  {model.emp_code}  -   Latitude : {model.latitude}  -   longitude : {model.longitude}  -  typeLog  : out";
                        LoggerExtensions.CreateLog(_path, _fileName, message);

                        OperationOutput Result = new OperationOutput();

                        Result.Header = ApplicationOperation.OperationResult(Enums.ServiceMessages.TransactionSuccess);
                        Result.Header.Success = true;
                        Result.Header.Code = 200;
                        Result.Output = null;
                        return Result;
                    }

                }
                else
                {
                    return await CheckInTransaction(model, _path, _fileName);
                }

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> CheckOutTransaction(transactionArea model, string _path, string _fileName)
        {
            try
            {
                var hasAttCheckIn = await _context.AttendanceTransactions
                        .Where(u => u.CreatedDate.Value.Date == DateTime.Now.Date && u.LogType.ToLower() == "in" && u.UserId == model.userId).AnyAsync();

                var hasClockCheckIn = await _contextAttendance.iclock_transaction
                                      .Where(u => u.punch_time.Date == DateTime.Now.Date && u.emp_code == model.emp_code && u.terminal_alias.ToLower().Contains("in")).AnyAsync();

                if (hasClockCheckIn || hasAttCheckIn)
                {
                    // Get only active areas
                    var _Areas = _context.Areas.Where(a => a.IsActive == true && a.IsDelete != true).ToList();
                    bool isFound = false;

                    foreach (var _area in _Areas)
                    {
                        var sCoord = new GeoCoordinate(double.Parse(model.latitude), double.Parse(model.longitude));
                        var eCoord = new GeoCoordinate(double.Parse(_area.Latitude), double.Parse(_area.Longitude));
                        var rCoord = sCoord.GetDistanceTo(eCoord);

                        if (rCoord <= double.Parse(_area.Diameter))
                        {
                            // Check if user has access to this area for checkout
                            var hasAccess = await UserHasAccessToArea(model.userId, _area.Id);

                            if (!hasAccess)
                            {
                                // Log unauthorized access attempt
                                var message = $"UNAUTHORIZED CHECKOUT ATTEMPT - Code_Emp: {model.emp_code} - Area: {_area.NameEn} - User not authorized for this area";
                                LoggerExtensions.CreateLog(_path, _fileName, message);

                                // Skip this area and continue checking other areas
                                continue;
                            }

                            var _modelAttendanceTransactions = new DAL.Models.AttendanceTransaction()
                            {
                                AreaId = _area.Id,
                                Distance = rCoord.ToString(),
                                Latitude = model.latitude,
                                Longitude = model.longitude,
                                UserId = model.userId,
                                CreatedDate = DateTime.Now,
                                LogType = "out"
                            };

                            await _context.AttendanceTransactions.AddAsync(_modelAttendanceTransactions);
                            await _context.SaveChangesAsync();

                            if (_area.HasAttendanceSystem == true)
                            {
                                if (_area.Id == 1 || _area.NameEn.ToLower() == "digital city")
                                {
                                    var _model = new Iclock_transaction
                                    {
                                        emp_code = model.emp_code,
                                        punch_time = DateTime.Now,
                                        punch_state = "1",
                                        verify_type = 15,
                                        work_code = "0",
                                        terminal_sn = "SYZ8243701355",
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
                                        terminal_id = 71,
                                        is_mask = 255,
                                        temperature = decimal.Parse("255")
                                    };

                                    await _contextAttendance.iclock_transaction.AddAsync(_model);
                                }
                                else if (_area.NameEn.ToLower() == "Jeddah Office".ToLower())
                                {
                                    var _model = new Iclock_transaction
                                    {
                                        emp_code = model.emp_code,
                                        punch_time = DateTime.Now,
                                        punch_state = "1",
                                        verify_type = 15,
                                        work_code = "0",
                                        terminal_sn = "CQUG242260672",
                                        terminal_alias = "Mob_Jeddah_Out",
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
                                        terminal_id = 61,
                                        is_mask = 255,
                                        temperature = decimal.Parse("255")
                                    };

                                    await _contextAttendance.iclock_transaction.AddAsync(_model);
                                }
                                else if (_area.NameEn.ToLower() == "New HQ".ToLower())
                                {
                                    var _model = new Iclock_transaction
                                    {
                                        emp_code = model.emp_code,
                                        punch_time = DateTime.Now,
                                        punch_state = "1",
                                        verify_type = 15,
                                        work_code = "0",
                                        terminal_sn = "SYZ8243701355",
                                        terminal_alias = "Mob_Emar_Out",
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
                                        terminal_id = 71,
                                        is_mask = 255,
                                        temperature = decimal.Parse("255")
                                    };

                                    await _contextAttendance.iclock_transaction.AddAsync(_model);
                                }
                                else
                                {
                                    var _model2 = new Iclock_transaction
                                    {
                                        emp_code = model.emp_code,
                                        punch_time = DateTime.Now,
                                        punch_state = "1",
                                        verify_type = 15,
                                        work_code = "0",
                                        terminal_sn = "SYZ8241700382",
                                        terminal_alias = "Mob_LaValle_OUT",
                                        area_alias = "General Area",
                                        longitude = null,
                                        latitude = null,
                                        gps_location = null,
                                        mobile = null,
                                        source = 1,
                                        purpose = 9,
                                        crc = "AAJADAGAFAAABAAACABA",
                                        is_attendance = null,
                                        reserved = null,
                                        upload_time = DateTime.Now,
                                        sync_status = null,
                                        sync_time = null,
                                        emp_id = null,
                                        terminal_id = 42,
                                        is_mask = 255,
                                        temperature = decimal.Parse("255")
                                    };

                                    await _contextAttendance.iclock_transaction.AddAsync(_model2);
                                }
                                await _contextAttendance.SaveChangesAsync();
                            }

                            isFound = true;
                            break;
                        }
                    }

                    if (isFound == true)
                    {
                        var Result = ResultOutputData.GenearetResultOutputSuccess();
                        return Result;
                    }
                    else
                    {
                        var message = $"Code_Emp : {model.emp_code} - Latitude : {model.latitude} - Longitude : {model.longitude} - TypeLog : out - No accessible area found or unauthorized";
                        LoggerExtensions.CreateLog(_path, _fileName, message);

                        OperationOutput Result = new OperationOutput();
                        Result.Header = ApplicationOperation.OperationResult(Enums.ServiceMessages.TransactionSuccess);
                        Result.Header.Success = true;
                        Result.Header.Code = 200;
                        Result.Output = null;
                        return Result;
                    }
                }
                else
                {
                    // No check-in found for today, redirect to check-in
                    LoggerExtensions.CreateLog(_path, _fileName, $"User {model.emp_code} attempted checkout without check-in - Redirecting to check-in");
                    return await CheckInTransaction(model, _path, _fileName);
                }
            }
            catch (Exception ex)
            {
                LoggerExtensions.CreateLog(_path, _fileName, $"Exception in CheckOutTransaction: {ex.Message}");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GetAllAreas()
        {
            try
            {
                var _areas = await _context.Areas
                    .Include(i => i.City)
                    .Include(i => i.Country)
                    .ToListAsync();
                var _areasDto = _areas.Select(s => new
                {
                    Id = s.Id,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Diameter = s.Diameter,
                    CityNameAr = s.City?.NameAr,
                    CityNameEn = s.City?.NameEn,
                    CountryAr = s.Country?.NameAr,
                    CountryEn = s.Country?.NameEn

                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_areasDto, _areasDto.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllAttendanceTransactions()
        {
            try
            {
                var _attendanceTransactions = await _context.AttendanceTransactions
                    .Include(i => i.User)
                    .Include(i => i.Area).ThenInclude(c => c.City)
                    .Include(i => i.Area).ThenInclude(c => c.Country)
                    .ToListAsync();
                var _attendanceTransactionsDto = _attendanceTransactions.Select(s => new
                {
                    Id = s.Id,
                    CreatedDate = s.CreatedDate,
                    AreaNameAr = s.Area?.NameAr,
                    AreaNameEn = s.Area?.NameEn,
                    UserName = s.User?.UserName,
                    UserCode = s.User?.Code,
                    CityNameAr = s.Area?.City?.NameAr,
                    CityNameEn = s.Area?.City?.NameEn,
                    CountryAr = s.Area?.Country?.NameAr,
                    CountryEn = s.Area?.Country?.NameEn

                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_attendanceTransactionsDto, _attendanceTransactionsDto.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllCities()
        {
            try
            {
                var _cities = await _context.Cities.Include(i => i.Country).ToListAsync();
                var _citiesDto = _cities.Select(s => new
                {
                    Id = s.Id,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn,
                    CountryId = s.CountryId,
                    CountryNameAr = s.Country.NameAr,
                    CountryNameEn = s.Country.NameEn

                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_citiesDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllCountries()
        {
            try
            {
                var _countries = await _context.Countries.ToListAsync();
                var _countriesDto = _countries.Select(s => new
                {
                    Id = s.Id,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn

                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_countriesDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllDataType()
        {
            try
            {
                var _DataTypes = await _context.DataTypes.ToListAsync();
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_DataTypes, _DataTypes.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GetAllSettings()
        {
            try
            {
                var settings = await FindAllAsync(i => i.IsDeleted != true);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(settings, settings.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetFeedback()
        {
            try
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetLocalAttendanceTransactions(transactionAttendance attendance)
        {
            try
            {
                var _endDate = attendance.endDate + " 23:59:57.993";
                var startDate = Dates.ConvertStringToDate(attendance.startDate);
                var endDate = Dates.ConvertStringToDate(_endDate);

                var _attendanceTransactions = await _context.AttendanceTransactions
                    .Where(i => i.UserId == attendance.userId && i.CreatedDate >= startDate && i.CreatedDate <= endDate)
                    .Include(i => i.User)
                    .ToListAsync();

                var _attendanceTransactionsDto = _attendanceTransactions.Select(s => new
                {
                    Id = s.Id,
                    CreatedDate = s.CreatedDate,
                    UserName = s.User?.UserName,
                    UserCode = s.User?.Code,
                    LogType = s.LogType
                }).ToList();

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_attendanceTransactionsDto, _attendanceTransactionsDto.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAttendanceTransactions(transactionAttendance attendance)
        {
            try
            {
                // Prepare date range
                List<AttendanceTransactionResponseDto> data = await GetAttendanceTransactionsDataList(attendance);

                // Generate Result Output
                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(data, data.Count);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Client disconnected or request was canceled from GetAttendanceTransactions.");
                return ResultOutputData.GenearetResultOutputCatch();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAttendanceTransactions: {ex.Message}");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<List<AttendanceTransactionResponseDto>> GetAttendanceTransactionsDataList(transactionAttendance attendance)
        {
            try
            {
                var endDateTimeString = $"{attendance.endDate} 23:59:59.993";
                var startDate = Dates.ConvertStringToDate(attendance.startDate);
                var endDate = Dates.ConvertStringToDate(endDateTimeString);

                var user = attendance.userId == null
                    ? await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Code == attendance.empCode)
                    : await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == attendance.userId);

                if (user == null)
                    return new List<AttendanceTransactionResponseDto>();

                var attendanceTransactions = await _context.AttendanceTransactions
                    .AsNoTracking()
                    .Where(i => i.UserId == user.Id && i.CreatedDate >= startDate && i.CreatedDate <= endDate)
                    .Select(s => new AttendanceTransactionResponseDto
                    {
                        Id = s.Id,
                        CreatedDate = s.CreatedDate,
                        punch_time = s.CreatedDate,
                        UserName = user.UserName,
                        UserCode = user.Code,
                        LogType = s.LogType == "in" ? "in" : "out"
                    })
                    .ToListAsync();

                var iclockTransactions = await _contextAttendance.iclock_transaction
                    .AsNoTracking()
                    .Where(i => i.emp_code == attendance.empCode &&
                                i.punch_time.Date >= startDate.Value.Date &&
                                i.punch_time.Date <= endDate.Value.Date)
                    .Select(s => new AttendanceTransactionResponseDto
                    {
                        Id = s.id,
                        CreatedDate = s.punch_time,
                        punch_time = s.punch_time,
                        UserName = user.UserName,
                        UserCode = user.Code,
                        LogType = GetLogType(s.terminal_alias) // "in" or "out"
                    })
                    .ToListAsync();

                var combinedTransactions = attendanceTransactions
                    .Concat(iclockTransactions)
                    .Where(x => x.punch_time.HasValue)
                    .GroupBy(x => new { x.UserCode, Time = x.punch_time.Value })
                    .Select(g => g.First())
                    .OrderByDescending(t => t.punch_time)
                    .ToList();

                var groupedByDate = combinedTransactions
                    .GroupBy(t => t.punch_time.Value.Date)
                    .ToList();

                var finalList = new List<AttendanceTransactionResponseDto>();

                foreach (var dayGroup in groupedByDate)
                {
                    var records = dayGroup.OrderBy(x => x.punch_time).ToList();

                    if (records.Count == 1)
                    {
                        var single = records.First();
                        finalList.Add(new AttendanceTransactionResponseDto
                        {
                            UserName = single.UserName,
                            UserCode = single.UserCode,
                            CreatedDate = single.punch_time,
                            punch_time = single.punch_time,
                            LogType = "in"
                        });
                    }
                    else
                    {
                        var first = records.First();
                        var last = records.Last();

                        finalList.Add(new AttendanceTransactionResponseDto
                        {
                            UserName = first.UserName,
                            UserCode = first.UserCode,
                            CreatedDate = first.punch_time,
                            punch_time = first.punch_time,
                            LogType = "in"
                        });

                        if (last.punch_time != first.punch_time)
                        {
                            finalList.Add(new AttendanceTransactionResponseDto
                            {
                                UserName = last.UserName,
                                UserCode = last.UserCode,
                                CreatedDate = last.punch_time,
                                punch_time = last.punch_time,
                                LogType = "out"
                            });
                        }
                    }
                }

                return finalList
                    .OrderByDescending(f => f.CreatedDate)
                    .ThenBy(f => f.LogType == "out")
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Client disconnected or request was canceled from GetAttendanceTransactions.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAttendanceTransactions: {ex.Message}");
                return null;
            }
        }



        //public async Task<List<AttendanceTransactionResponseDto>> GetAttendanceTransactionsDataList(transactionAttendance attendance)
        //{
        //    try
        //    {
        //        // Prepare date range
        //        var endDateTimeString = $"{attendance.endDate} 23:59:59.993";
        //        var startDate = Dates.ConvertStringToDate(attendance.startDate);
        //        var endDate = Dates.ConvertStringToDate(endDateTimeString);
        //        var user = attendance.userId == null ?await _context.Users.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(i =>  i.Code == attendance.empCode): await _context.Users.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(i => i.Id == attendance.userId);

        //        // Fetch Attendance Transactions from AttendanceTransactions table
        //        var attendanceTransactions = await _context.AttendanceTransactions
        //            .AsNoTrackingWithIdentityResolution()
        //            .Where(i => i.UserId == user.Id && i.CreatedDate >= startDate && i.CreatedDate <= endDate)
        //            .Select(s => new AttendanceTransactionResponseDto
        //            {
        //                Id = s.Id,
        //                CreatedDate = s.CreatedDate,
        //                punch_time = s.CreatedDate,
        //                UserName = user.UserName,
        //                UserCode = user.Code,
        //                LogType = s.LogType
        //            })
        //            .ToListAsync();

        //        // Fetch Transactions from iclock_transaction table
        //        var iclockTransactions = await _contextAttendance.iclock_transaction
        //            .AsNoTrackingWithIdentityResolution()
        //            .Where(i => i.emp_code == attendance.empCode && i.punch_time.Date >= startDate.Value.Date && i.punch_time.Date <= endDate.Value.Date)
        //            .Select(s => new AttendanceTransactionResponseDto
        //            {
        //                Id = s.id,
        //                CreatedDate = s.punch_time,
        //                punch_time = s.punch_time,
        //                UserName = user.UserName,
        //                UserCode = user.Code,
        //                LogType = GetLogType(s.terminal_alias)
        //            })
        //            .ToListAsync();

        //        // Combine both lists

        //        var combinedTransactions = attendanceTransactions.Union(iclockTransactions).ToList();

        //        return combinedTransactions;
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        _logger.LogWarning("Client disconnected or request was canceled from GetAttendanceTransactions.");
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error in GetAttendanceTransactions: {ex.Message}");
        //        return null;
        //    }
        //}



        private static string GetLogType(string? terminal_alias)
        {
            return terminal_alias.ToLower().Contains("in") ? "in" : "out";
        }

        public async Task<OperationOutput> GetSettingByKey(string key)
        {
            try
            {
                var find = await _context.Settings.FirstOrDefaultAsync(f => f.Key == key);
                if (find != null)
                {
                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(find, 1);
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

        public async Task<OperationOutput> GetSettingByKeysList(List<string> keys)
        {
            try
            {
                var _List = new List<Setting>();

                foreach (var key in keys)
                {

                    var find = await _context.Settings.FirstOrDefaultAsync(f => f.Key == key);
                    if (find != null)
                    {
                        _List.Add(find);

                    }
                }

                if (_List.Count() > 0)
                {
                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_List, 1);
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

        public string GenerateFolder(string key)
        {
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            var folderName = "Localzation\\";
            var path = Path.Combine(sharedPath, folderName);


            System.IO.Directory.CreateDirectory(path);

            folderName = "Localzation\\" + key;
            path = Path.Combine(sharedPath, folderName);


            System.IO.Directory.CreateDirectory(path);

            return path;


        }

        private async Task<bool> UserHasAccessToArea(string userId, int areaId)
        {
            // Check if area is for all employees
            var area = await _context.Areas.FindAsync(areaId);
            if (area == null) return false;

            if (area.IsForAllEmployees == true)
                return true;

            // Get user's groups
            var userGroupIds = await _context.GroupUsers
                .Where(gu => gu.UserId == userId)
                .Select(gu => gu.GroupId)
                .ToListAsync();

            if (!userGroupIds.Any())
                return false;

            // Check if any of user's groups have access to this area
            var hasAccess = await _context.GroupAreas.CountAsync
                (ag => ag.AreaId == areaId && userGroupIds.Contains(ag.GroupId)) > 0;

            return hasAccess;
        }

        public async Task<OperationOutput> AssignGroupsToArea(int areaId ,List<int> groupIds)
        {
            try
            {
                // Remove existing assignments
                var existingAssignments = _context.GroupAreas.Where(ag => ag.AreaId == areaId);
                _context.GroupAreas.RemoveRange(existingAssignments);

                // Add new assignments
                foreach (var groupId in groupIds)
                {
                    await _context.GroupAreas.AddAsync(new GroupArea
                    {
                        AreaId = areaId,
                        GroupId = groupId,
                        CreatedDate = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                
                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput("Groups assigned successfully", 1);
                    return _result;

            }
            catch (Exception ex)
            {

                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

    }
}

