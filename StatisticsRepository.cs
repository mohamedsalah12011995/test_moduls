using Core.Helpers;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Dto.Kafo;
using Nupco.Core.Dto.Statistic;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
namespace Nupco.EF.Repositories
{
    public class StatisticsRepository : IStatisticRepository
    {
    private readonly ApplicationDbContext _context;
    private readonly ILogger _logger;

    public StatisticsRepository(ApplicationDbContext context, ILogger logger) 
    {
        _logger = logger;
        _context = context;

    }

        public async Task<OperationOutput> GetAreasAttendanceByYear(int? year)
        {
            try
            {
                //var AllRecords = new List<AttendanceTransaction>();

                // Get the month names

                DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;

                string[] MonthNames = dtfi.MonthNames;
                var months = Enumerable.Range(1, 12)
                           .Select(x => new
                           {
                               year = DateTime.Now.Year,
                               month = x
                           });


                   var AllRecords =await _context.AttendanceTransactions.Where(i => i.CreatedDate.Value.Year == year).AsNoTrackingWithIdentityResolution()
                    .GroupBy(j=> new { j.CreatedDate, j.LogType }).Select(g=> new
                    {
                       monthNum = g.Key.CreatedDate.Value.Month,
                        LogType = g.Key.LogType,
                       ItemCount = g.Count(),
                       monthName = MonthNames[g.Key.CreatedDate.Value.Month-1]
                    }).ToListAsync();

                var items = new List<AttendanceDepartureByMonth>();

                foreach (var item in months)
                {
                    var newRecord = new AttendanceDepartureByMonth();

                    var attCount = AllRecords.Where(i => i.LogType == "in" && i.monthNum == item.month).Count();
                    var _att = AllRecords.Where(i => i.LogType == "in" && i.monthNum == item.month).OrderBy(i => i.monthNum)
                        .Select(s => new Attendance
                        {
                            MonthNum = s.monthNum,
                            MonthName = MonthNames[item.month - 1],
                            ItemCount = attCount,
                            LogType = s.LogType,
                        }).FirstOrDefault();
                    newRecord.Attendances.Add(_att);

                    var depCount = AllRecords.Where(i => i.LogType == "out" && i.monthNum == item.month).Count();
                    var _dep = AllRecords.Where(i => i.LogType == "out" && i.monthNum == item.month).OrderBy(i => i.monthNum).Select(s => new Departure
                    {
                        MonthNum = s.monthNum,
                        MonthName = MonthNames[item.month - 1],
                        ItemCount = depCount,
                        LogType = s.LogType,
                    }).FirstOrDefault();
                    newRecord.Departures.Add(_dep);


                    newRecord.monthNum = item.month;
                    newRecord.monthName = MonthNames[item.month - 1];
                    items.Add(newRecord);
                }
                





                //var newRecord = new
                //{

                //    Attendances = AllRecords.Where(i => i.LogType == "in").OrderBy(i=>i.monthNum).ToList(),

                //    Departures = AllRecords.Where(i => i.LogType == "out").OrderBy(i => i.monthNum).ToList()
                //};



                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(items, items.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAreasAttendanceByYearOfMonth(int year, int month)
        {
            try
            {
                var _attendanceTransactions = new List<AttendanceTransaction>();

                if (year == 0 && month == 0)
                    _attendanceTransactions = await _context.AttendanceTransactions.AsNoTrackingWithIdentityResolution().Include(i => i.Area).Select(s => s).Distinct().ToListAsync();
                else
                    _attendanceTransactions = await _context.AttendanceTransactions.Include(i => i.Area)
                        .Where(i => i.CreatedDate.Value.Year == year && i.CreatedDate.Value.Month == month)
                        .Select(s => s).Distinct().ToListAsync();


                var items_attendance = _attendanceTransactions.Where(i => i.LogType == "in").DistinctBy(i=>i.AreaId).Select(s => new AttendanceDeparture
                {

                    attendance = new Attendance
                    {
                        ItemCount = _attendanceTransactions.Where(i => i.AreaId == s.AreaId && i.LogType == "in").Count(),
                        TitleAr = s.Area.NameAr,
                        TitleEn = s.Area.NameEn,
                        LogType = "in",

                    }


                }).ToList();
                var items_departure = _attendanceTransactions.Where(i => i.LogType == "out").DistinctBy(i => i.AreaId).Select(s => new AttendanceDeparture
                {

                    departure = new Departure
                    {
                        ItemCount = _attendanceTransactions.Where(i => i.AreaId == s.AreaId && i.LogType == "out").Count(),
                        TitleAr = s.Area.NameAr,
                        TitleEn = s.Area.NameEn,
                        LogType = "out",
                        CreatedDate = s.CreatedDate

                    }


                }).ToList();

                var _itemsAttendance = items_attendance.OrderByDescending(i => i.attendance.ItemCount).ToList();
                var _itemsDeparture = items_departure.OrderByDescending(i => i.departure.ItemCount).ToList();

                var _items = new
                {
                    _itemsAttendance = _itemsAttendance,
                    _itemsDeparture = _itemsDeparture
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, _attendanceTransactions.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }



        public async Task<OperationOutput> GetTopActivitiesUsers()
        {
            try
            {

                var _activities = await _context.ActivitiesUsers.AsNoTrackingWithIdentityResolution().Include(i => i.Activities).Select(s => s.Activities).Distinct().ToListAsync();
                var items = _activities.Select(s => new
                {

                    ItemCount = _context.ActivitiesUsers.Where(i => i.ActivitiesId == s.Id).Count(),
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,

                }).ToList();
                if (items.FirstOrDefault()?.TitleAr == null && items.FirstOrDefault()?.TitleEn == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }
                var _items = items.OrderByDescending(i => i.ItemCount).Take(5).ToList();


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, _items.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetTopChanalsMessages()
        {
            try
            {
                var _chats = await _context.Chats.AsNoTrackingWithIdentityResolution().Include(i => i.Channel).Select(s => s.Channel).Distinct().ToListAsync();
                var items = _chats.Select(s => new
                {
                    ItemCount = _context.Chats.Where(i => i.ChannelId == s.Id).Count(),
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                }).ToList();

                if (items.FirstOrDefault()?.TitleAr == null && items.FirstOrDefault()?.TitleEn == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }
                var _items = items.OrderByDescending(i => i.ItemCount).Take(5).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, _items.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetTopCommentsUser()
        {
            try
            {
                var items = await _context.Comments
                    .AsNoTracking()
                    .Where(c => c.CreatedBy != null)
                    .GroupBy(c => c.CreatedBy)
                    .Select(g => new
                    {
                        itemCount = g.Count(),
                        comennterName = g.Select(c => c.CreatedUser.UserName).FirstOrDefault()
                    })
                    .OrderByDescending(x => x.itemCount)
                    .Take(5)
                    .ToListAsync();

                if (items.Count == 0 || (items[0].itemCount == 0 && items[0].comennterName == null))
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(items, items.Count);

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetTopEventsUsers()
        {
            try
            {
                var _events = await _context.EventsUsers.AsNoTrackingWithIdentityResolution().Include(i => i.Events).Select(s => s.Events).Distinct().ToListAsync();
                var items = _events.Select(s => new
                {

                    ItemCount = _context.EventsUsers.Where(i => i.EventsId == s.Id).Count(),
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,

                }).ToList();
                if (items.FirstOrDefault()?.TitleAr == null && items.FirstOrDefault()?.TitleEn == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }
                var _items = items.OrderByDescending(i => i.ItemCount).Take(5).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, _items.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public Task<OperationOutput> GetTopInterActionsNews() =>
            GetTopInterActionsForEntityAsync(1);

        public Task<OperationOutput> GetTopInterActionsSpace() =>
            GetTopInterActionsForEntityAsync(2);
        public Task<OperationOutput> GetTopInterActionsKafo() =>
            GetTopInterActionsForEntityAsync((int)Enums.Entities.Kafo);

        /// <summary>
        /// One SQL round-trip: aggregate InterActions + Comments per ItemId, join, ORDER BY total, TOP 5 (no huge IN list, no loading all ItemIds into memory).
        /// Targets SQL Server table names from migrations (InterActions, Comments).
        /// </summary>
        private async Task<OperationOutput> GetTopInterActionsForEntityAsync(int entityId)
        {
            try
            {
                var ranked = await LoadTop5InteractionItemScoresAsync(entityId).ConfigureAwait(false);

                if (ranked.Count == 0)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var postIdSet = new HashSet<int>();
                foreach (var r in ranked)
                {
                    if (int.TryParse(r.ItemId, out var pid))
                        postIdSet.Add(pid);
                }

                var posts = await _context.Posts
                    .AsNoTracking()
                    .Where(p => p.EntityId == entityId && postIdSet.Contains(p.Id))
                    .Select(p => new { p.Id, p.PostBriefeContentAr, p.PostBriefeContentEn })
                    .ToListAsync()
                    .ConfigureAwait(false);

                var postById = posts.ToDictionary(p => p.Id);

                var items = ranked.Select(r =>
                {
                    string? titleAr = null;
                    string? titleEn = null;
                    if (int.TryParse(r.ItemId, out var postId) && postById.TryGetValue(postId, out var row))
                    {
                        titleAr = row.PostBriefeContentAr;
                        titleEn = row.PostBriefeContentEn;
                    }
                    return new
                    {
                        r.ItemCount,
                        TitleAr = titleAr,
                        TitleEn = titleEn
                    };
                }).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(items, items.Count);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        private async Task<List<(string ItemId, int ItemCount)>> LoadTop5InteractionItemScoresAsync(int entityId)
        {
            // Single batch: only ever returns 5 rows. Uses same semantics as before (ItemIds must appear in InterActions).
            const string sql = @"
SELECT TOP (5) ia.ItemId, ia.IaCount + COALESCE(cm.CmCount, 0) AS ItemCount
FROM (
    SELECT ItemId, COUNT(*) AS IaCount
    FROM InterActions
    WHERE EntityId = @entityId AND ItemId IS NOT NULL AND ItemId <> N''
    GROUP BY ItemId
) AS ia
LEFT JOIN (
    SELECT ItemId, COUNT(*) AS CmCount
    FROM Comments
    WHERE EntityId = @entityId AND ItemId IS NOT NULL AND ItemId <> N''
    GROUP BY ItemId
) AS cm ON ia.ItemId = cm.ItemId
ORDER BY ia.IaCount + COALESCE(cm.CmCount, 0) DESC";

            await _context.Database.OpenConnectionAsync().ConfigureAwait(false);
            try
            {
                await using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                var p = command.CreateParameter();
                p.ParameterName = "@entityId";
                p.Value = entityId;
                command.Parameters.Add(p);

                await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                var list = new List<(string ItemId, int ItemCount)>();
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var itemId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    var itemCount = reader.GetInt32(1);
                    list.Add((itemId, itemCount));
                }

                return list;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }

        public async Task<OperationOutput> GetTopInterActionsUser()
        {
            try
            {
                // Group on DB by UserId (avoids N+1 Count and null User rows from Distinct().Select(User)).
                var items = await _context.InterActions
                    .AsNoTracking()
                    .Where(i => i.UserId != null && i.UserId != "")
                    .GroupBy(i => i.UserId)
                    .Select(g => new
                    {
                        ItemCount = g.Count(),
                        EmployeeName = g.Select(x => x.User.UserName).FirstOrDefault()
                    })
                    .OrderByDescending(x => x.ItemCount)
                    .Take(5)
                    .ToListAsync();

                if (items.Count == 0)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(items, items.Count);
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetTopInterestsUsers()
        {
            try
            {
                var _interests = await _context.InterestsUsers.AsNoTrackingWithIdentityResolution().Include(i => i.Interests).Select(s => s.Interests).Distinct().ToListAsync();
                var items = _interests.Select(s => new
                {

                    ItemCount = _context.InterestsUsers.Where(i => i.InterestsId == s.Id).Count(),
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                }).ToList();
                if (items.FirstOrDefault()?.TitleAr == null && items.FirstOrDefault()?.TitleEn == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }
                var _items = items.OrderByDescending(i => i.ItemCount).Take(5).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, _items.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetTopJoindChannel()
        {
            try
            {
                var items = await _context.ChanalsUsers
                    .AsNoTracking()
                    .Where(cu => cu.ChanalId != null)
                    .GroupBy(cu => cu.ChanalId)
                    .Select(g => new
                    {
                        ItemCount = g.Count(),
                        TitleAr = g.Select(x => x.Channel != null ? x.Channel.TitleAr : null).FirstOrDefault(),
                        TitleEn = g.Select(x => x.Channel != null ? x.Channel.TitleEn : null).FirstOrDefault()
                    })
                    .OrderByDescending(i => i.ItemCount)
                    .Take(5)
                    .ToListAsync();

                if (items.Count == 0 || (items.FirstOrDefault()?.TitleAr == null && items.FirstOrDefault()?.TitleEn == null))
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(items, items.Count);
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetTopSkillsUsers()
        {
            try
            {
                var _skills = await _context.SkillsUsers.AsNoTrackingWithIdentityResolution().Include(i => i.Skill).Select(s => s.Skill).Distinct().ToListAsync();
                var items = _skills.Select(s => new
                {

                    ItemCount = _context.SkillsUsers.Where(i => i.SkillId == s.Id).Count(),
                    TitleAr = s.NameAr,
                    TitleEn = s.NameEn,
                }).ToList();
                if (items.FirstOrDefault()?.TitleAr == null && items.FirstOrDefault()?.TitleEn == null)
                {
                    return  ResultOutputData.GenearetResultOutputNoDataReturned();
                }
                var _items = items.OrderByDescending(i => i.ItemCount).Take(5).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, _items.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetTotalsUsersByDevices()
        {
            try
            {
                var _users = await _context.Users.Where(i => i.IsDeleted == false && i.IsActive == true).AsNoTrackingWithIdentityResolution().ToListAsync();
                var _items = new
                {
                    totalUsers = _users.Count(),
                    totalUsersAndroid = _users.Count(i=> i.DeviceType?.ToLower()== "android"),
                    totalUsersIos = _users.Count(i=> i.DeviceType?.ToLower()=="ios"),
                    totalUsersWeb = _users.Count(i=> i.DeviceType?.ToLower()== "web"),
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetVideoStatistic()
        {
            try
            {
                var _items = new
                {
                    totalVideo = await _context.Videos.CountAsync(),
                    totalVideoComment = await _context.Comments.CountAsync(i => i.EntityId == 11),
                    totalVideoInterActionsComment = await _context.InterActions.CountAsync(i => i.EntityId == 11),
                    UsersViewsMobile = await _context.UserViews.Where(c => c.EntityId == (int)Enums.EntityType.Videos && c.Platform == "mobile").Select(c => c.UserId).Distinct().CountAsync(),
                    UsersViewsWeb = await _context.UserViews.Where(c => c.EntityId == (int)Enums.EntityType.Videos && c.Platform == "web").Select(c => c.UserId).Distinct().CountAsync()

                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<OperationOutput> GetNewsStatistic()
        {
            try
            {
                var _items = new
                {
                    totalNews = await _context.Posts.CountAsync(i =>i.EntityId==1 && i.IsDeleted == false && i.IsActive == true),
                    totalNewsComment = await _context.Comments.CountAsync(i => i.EntityId == 1),
                    totalNewsInterActionsComment = await _context.InterActions.CountAsync(i => i.EntityId == 1),
                    UsersViewsMobile = await _context.UserViews.Where(c => c.EntityId == (int)Enums.EntityType.News && c.Platform == "mobile").Select(c => c.UserId).Distinct().CountAsync(),
                    UsersViewsWeb = await _context.UserViews.Where(c => c.EntityId == (int)Enums.EntityType.News && c.Platform == "web").Select(c => c.UserId).Distinct().CountAsync()

                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetSpaceStatistic()
        {
            try
            {
                var _items = new
                {
                    totalSpace = await _context.Posts.CountAsync(i => i.EntityId == 2 && i.IsDeleted == false && i.IsActive == true),
                    totalSpaceComment = await _context.Comments.CountAsync(i => i.EntityId == 2),
                    totalSpaceInterActionsComment = await _context.InterActions.CountAsync(i => i.EntityId == 2),
                    UsersViewsMobile = await _context.UserViews.Where(c => c.EntityId == (int)Enums.EntityType.Spaces &&c.Platform=="mobile").Select(c => c.UserId).Distinct().CountAsync(),
                    UsersViewsWeb = await _context.UserViews.Where(c => c.EntityId == (int)Enums.EntityType.Spaces && c.Platform=="web").Select(c => c.UserId).Distinct().CountAsync()
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetStatisticToDashbord()
        {
            try
            {
                var Statistic = new
                {
                    ChannelsStatistic = await GetChannelsStatistic(),
                    SpaceStatistic = await GetSpaceStatistic(),
                    NewsStatistic = await GetNewsStatistic(),
                    VideoStatistic = await GetVideoStatistic(),
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(Statistic, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }   
         public async Task<OperationOutput> GetChannelsStatistic()
        {
            try
            {
                // Retrieve top 5 records per channel with duplicates
                var records = await _context.Chats.Include(i => i.Channel).AsNoTrackingWithIdentityResolution()
               .GroupBy(r => r.ChannelId) // ChannelId تجمع حسب 
               .Select(g => new
               {
                   Id = g.Key,
                   Count = g.Count(), // تحسب عدد مرات التكرار
                   channelNameAr = g.FirstOrDefault().Channel.TitleAr,
                   channelNameEn = g.FirstOrDefault().Channel.TitleEn
               })
               .OrderByDescending(x => x.Count) // ترتيب حسب العدد تنازليًا
               .Take(5) // تأخذ أول 5
               .ToListAsync();


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(records, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }


        }

        public async Task<OperationOutput> GetNewChannelsStatistic()
        {
            try
            {
                // Retrieve top 5 records per channel with duplicates
                var records = await _context.Chats.Include(i => i.Channel).AsNoTrackingWithIdentityResolution()
               .GroupBy(r => r.ChannelId) // ChannelId تجمع حسب 
               .Select(g => new
               {
                   Id = g.Key,
                   Count = g.Count(), // تحسب عدد مرات التكرار
                   channelNameAr = g.FirstOrDefault().Channel.TitleAr,
                   channelNameEn = g.FirstOrDefault().Channel.TitleEn
               })
               .OrderByDescending(x => x.Count) // ترتيب حسب العدد تنازليًا
               .Take(5) // تأخذ أول 5
               .ToListAsync();

                var _obj = new
                {
                    UsersViewsMobile = await _context.UserViews
                        .Where(c => c.EntityId == (int)Enums.EntityType.Channels && c.Platform == "mobile")
                        .Select(c => c.UserId)
                        .Distinct()
                        .CountAsync(),
                    UsersViewsWeb = await _context.UserViews
                        .Where(c => c.EntityId == (int)Enums.EntityType.Channels && c.Platform == "web")
                        .Select(c => c.UserId)
                        .Distinct()
                        .CountAsync(),

                    records = records
                };



                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_obj, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }


        }

        public async Task<OperationOutput> GetKafoStatistic()
        {
            try
            {
                var now = DateTime.Now;
                var currentMonth = now.Month;
                var currentYear = now.Year;

                var counts = await _context.KafoUsers
                    .Select(i => new { i.CreatedDate.Value.Month, i.CreatedDate.Value.Year })
                    .GroupBy(_ => 1) 
                    .Select(g => new
                    {
                        MonthCount = g.Count(i => i.Month == currentMonth && i.Year == currentYear),
                        YearCount = g.Count(i => i.Year == currentYear),
                        TotalCount = g.Count()
                    })
                    .FirstOrDefaultAsync() ?? new { MonthCount = 0, YearCount = 0, TotalCount = 0 };

                var topSender = await _context.KafoUsers
                    .GroupBy(u => new { u.SenderId, u.SenderUser.FirstName, u.SenderUser.LastName })
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key.FirstName + " " + g.Key.LastName)
                    .FirstOrDefaultAsync() ?? string.Empty;

                var topReceiver = await _context.KafoUsers
                    .GroupBy(u => new { u.ReceiverId, u.ReceiverUser.FirstName, u.ReceiverUser.LastName })
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key.FirstName + " " + g.Key.LastName)
                    .FirstOrDefaultAsync() ?? string.Empty;

                var _items = new
                {
                    totalKafoOfMonth = counts.MonthCount,
                    totalKafoOfYear = counts.YearCount,
                    totalKafoUsers = counts.TotalCount,
                    topKafoSender = topSender,
                    topKafoReceiver = topReceiver
                };

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_items, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetKafoStatisticYearsOnly()
        {
            try
            {
                var yearlyCounts = await _context.KafoUsers
                    .Where(i => i.CreatedDate != null)
                    .GroupBy(i => i.CreatedDate.Value.Year)
                    .Select(g => new
                    {
                        Year = g.Key,
                        TotalCount = g.Count()
                    })
                    .OrderByDescending(g => g.Year)
                    .ToListAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(yearlyCounts, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetKafoInsights(KafoInsightsFilter filter)
        {
            try
            {
                filter ??= new KafoInsightsFilter();
                var now = DateTime.Now;
                var year = filter.Year ?? now.Year;
                var month = filter.Month ?? now.Month;
                var top = filter.Top > 0 ? filter.Top : 5;

                var baseQuery = _context.KafoUsers.AsNoTracking().Where(i => i.CreatedDate != null);
                var yearQuery = baseQuery.Where(i => i.CreatedDate.Value.Year == year);
                var monthQuery = yearQuery.Where(i => i.CreatedDate.Value.Month == month);

                var totalKafoCards = await baseQuery.CountAsync();
                var totalKafoOfYear = await yearQuery.CountAsync();
                var totalKafoOfMonth = await monthQuery.CountAsync();

                var topSenders = await GetTopSenderRowsAsync(monthQuery, top);
                var topReceivers = await GetTopReceiverRowsAsync(monthQuery, top);
                var topSenderManagers = await GetTopSenderManagerRowsAsync(monthQuery, top);
                var orgTop = Math.Max(top, 8);
                var topSenderDepartments = await GetTopOrgUnitRowsAsync(monthQuery, senderSide: true, byDivision: false, orgTop);
                var topReceiverDepartments = await GetTopOrgUnitRowsAsync(monthQuery, senderSide: false, byDivision: false, orgTop);
                var topSenderDivisions = await GetTopOrgUnitRowsAsync(monthQuery, senderSide: true, byDivision: true, orgTop);
                var topReceiverDivisions = await GetTopOrgUnitRowsAsync(monthQuery, senderSide: false, byDivision: true, orgTop);

                var uniqueSendersOfMonth = await monthQuery
                    .Where(i => i.SenderId != null)
                    .Select(i => i.SenderId)
                    .Distinct()
                    .CountAsync();

                var uniqueReceiversOfMonth = await monthQuery
                    .Where(i => i.ReceiverId != null)
                    .Select(i => i.ReceiverId)
                    .Distinct()
                    .CountAsync();

                var managerUserNames = await GetManagerUserNameSetAsync();
                var managerCardsOfMonth = 0;
                if (managerUserNames.Count > 0)
                {
                    var senderUserNames = await monthQuery
                        .Where(i => i.SenderUser != null && i.SenderUser.UserName != null)
                        .Select(i => i.SenderUser.UserName)
                        .ToListAsync();
                    managerCardsOfMonth = senderUserNames.Count(u => managerUserNames.Contains(u.Trim().ToLowerInvariant()));
                }

                var monthlyVolumeRaw = await yearQuery
                    .GroupBy(i => i.CreatedDate.Value.Month)
                    .Select(g => new KafoMonthlyVolumeDto
                    {
                        Month = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var monthlyVolume = Enumerable.Range(1, 12)
                    .Select(m => new KafoMonthlyVolumeDto
                    {
                        Month = m,
                        Count = monthlyVolumeRaw.FirstOrDefault(x => x.Month == m)?.Count ?? 0
                    })
                    .ToList();

                var topSenderName = topSenders.FirstOrDefault()?.FullName
                    ?? topSenders.FirstOrDefault()?.UserName
                    ?? string.Empty;
                var topReceiverName = topReceivers.FirstOrDefault()?.FullName
                    ?? topReceivers.FirstOrDefault()?.UserName
                    ?? string.Empty;

                // All-time top names (same idea as GetKafoStatistic) for KPI strip
                var allTimeTopSender = await baseQuery
                    .Where(u => u.SenderUser != null)
                    .GroupBy(u => new { u.SenderId, u.SenderUser.FirstName, u.SenderUser.LastName })
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key.FirstName + " " + g.Key.LastName)
                    .FirstOrDefaultAsync() ?? string.Empty;

                var allTimeTopReceiver = await baseQuery
                    .Where(u => u.ReceiverUser != null)
                    .GroupBy(u => new { u.ReceiverId, u.ReceiverUser.FirstName, u.ReceiverUser.LastName })
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key.FirstName + " " + g.Key.LastName)
                    .FirstOrDefaultAsync() ?? string.Empty;

                var result = new KafoInsightsResultDto
                {
                    TotalKafoOfMonth = totalKafoOfMonth,
                    TotalKafoOfYear = totalKafoOfYear,
                    TotalKafoCards = totalKafoCards,
                    UniqueSendersOfMonth = uniqueSendersOfMonth,
                    UniqueReceiversOfMonth = uniqueReceiversOfMonth,
                    ManagerCardsOfMonth = managerCardsOfMonth,
                    TopKafoSender = string.IsNullOrWhiteSpace(allTimeTopSender) ? topSenderName : allTimeTopSender,
                    TopKafoReceiver = string.IsNullOrWhiteSpace(allTimeTopReceiver) ? topReceiverName : allTimeTopReceiver,
                    TopSenders = topSenders,
                    TopReceivers = topReceivers,
                    TopSenderManagers = topSenderManagers,
                    MonthlyVolume = monthlyVolume,
                    TopSenderDepartments = topSenderDepartments,
                    TopReceiverDepartments = topReceiverDepartments,
                    TopSenderDivisions = topSenderDivisions,
                    TopReceiverDivisions = topReceiverDivisions
                };

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(result, 1);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetTopKafoSendersOfMonths(KafoInsightsFilter filter)
        {
            try
            {
                filter ??= new KafoInsightsFilter();
                var now = DateTime.Now;
                var year = filter.Year ?? now.Year;
                var month = filter.Month ?? now.Month;
                var top = filter.Top > 0 ? filter.Top : 5;

                var monthQuery = _context.KafoUsers.AsNoTracking()
                    .Where(i => i.CreatedDate != null
                        && i.CreatedDate.Value.Year == year
                        && i.CreatedDate.Value.Month == month);

                var topSenders = await GetTopSenderRowsAsync(monthQuery, top);
                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(topSenders, topSenders.Count);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetTopKafoSenderManagersOfMonths(KafoInsightsFilter filter)
        {
            try
            {
                filter ??= new KafoInsightsFilter();
                var now = DateTime.Now;
                var year = filter.Year ?? now.Year;
                var month = filter.Month ?? now.Month;
                var top = filter.Top > 0 ? filter.Top : 5;

                var monthQuery = _context.KafoUsers.AsNoTracking()
                    .Where(i => i.CreatedDate != null
                        && i.CreatedDate.Value.Year == year
                        && i.CreatedDate.Value.Month == month);

                var topManagers = await GetTopSenderManagerRowsAsync(monthQuery, top);
                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(topManagers, topManagers.Count);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        private async Task<List<KafoInsightUserCountDto>> GetTopSenderRowsAsync(IQueryable<KafoUsers> monthQuery, int top)
        {
            var rows = await monthQuery
                .Where(u => u.SenderId != null && u.SenderUser != null)
                .GroupBy(u => new
                {
                    u.SenderId,
                    u.SenderUser.UserName,
                    u.SenderUser.FirstName,
                    u.SenderUser.LastName
                })
                .Select(g => new
                {
                    g.Key.SenderId,
                    g.Key.UserName,
                    g.Key.FirstName,
                    g.Key.LastName,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToListAsync();

            return rows.Select(x => new KafoInsightUserCountDto
            {
                UserId = x.SenderId,
                UserName = x.UserName,
                FullName = $"{x.FirstName} {x.LastName}".Trim(),
                Count = x.Count
            }).ToList();
        }

        private async Task<List<KafoInsightUserCountDto>> GetTopReceiverRowsAsync(IQueryable<KafoUsers> monthQuery, int top)
        {
            var rows = await monthQuery
                .Where(u => u.ReceiverId != null && u.ReceiverUser != null)
                .GroupBy(u => new
                {
                    u.ReceiverId,
                    u.ReceiverUser.UserName,
                    u.ReceiverUser.FirstName,
                    u.ReceiverUser.LastName
                })
                .Select(g => new
                {
                    g.Key.ReceiverId,
                    g.Key.UserName,
                    g.Key.FirstName,
                    g.Key.LastName,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToListAsync();

            return rows.Select(x => new KafoInsightUserCountDto
            {
                UserId = x.ReceiverId,
                UserName = x.UserName,
                FullName = $"{x.FirstName} {x.LastName}".Trim(),
                Count = x.Count
            }).ToList();
        }

        private async Task<HashSet<string>> GetManagerUserNameSetAsync()
        {
            var managers = await _context.UsersManagerEmployees
                .AsNoTracking()
                .Where(u => u.UserNameManager != null && u.UserNameManager != "")
                .Select(u => u.UserNameManager)
                .Distinct()
                .ToListAsync();

            return managers
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m!.Trim().ToLowerInvariant())
                .ToHashSet();
        }

        private async Task<List<KafoInsightUserCountDto>> GetTopSenderManagerRowsAsync(IQueryable<KafoUsers> monthQuery, int top)
        {
            var managerSet = await GetManagerUserNameSetAsync();
            if (managerSet.Count == 0)
            {
                return new List<KafoInsightUserCountDto>();
            }

            var senderRows = await monthQuery
                .Where(u => u.SenderId != null && u.SenderUser != null && u.SenderUser.UserName != null)
                .GroupBy(u => new
                {
                    u.SenderId,
                    u.SenderUser.UserName,
                    u.SenderUser.FirstName,
                    u.SenderUser.LastName
                })
                .Select(g => new
                {
                    g.Key.SenderId,
                    g.Key.UserName,
                    g.Key.FirstName,
                    g.Key.LastName,
                    Count = g.Count()
                })
                .ToListAsync();

            return senderRows
                .Where(x => x.UserName != null && managerSet.Contains(x.UserName.Trim().ToLowerInvariant()))
                .OrderByDescending(x => x.Count)
                .Take(top)
                .Select(x => new KafoInsightUserCountDto
                {
                    UserId = x.SenderId,
                    UserName = x.UserName,
                    FullName = $"{x.FirstName} {x.LastName}".Trim(),
                    Count = x.Count
                })
                .ToList();
        }

        private async Task<List<KafoInsightOrgCountDto>> GetTopOrgUnitRowsAsync(
            IQueryable<KafoUsers> monthQuery,
            bool senderSide,
            bool byDivision,
            int top)
        {
            List<(string? Name, int Count)> rows;

            if (senderSide && byDivision)
            {
                rows = (await monthQuery
                    .Where(u => u.SenderUser != null)
                    .GroupBy(u => u.SenderUser.DivisionName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .Select(x => (x.Name, x.Count))
                    .ToList();
            }
            else if (senderSide)
            {
                rows = (await monthQuery
                    .Where(u => u.SenderUser != null)
                    .GroupBy(u => u.SenderUser.DepartmentName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .Select(x => (x.Name, x.Count))
                    .ToList();
            }
            else if (byDivision)
            {
                rows = (await monthQuery
                    .Where(u => u.ReceiverUser != null)
                    .GroupBy(u => u.ReceiverUser.DivisionName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .Select(x => (x.Name, x.Count))
                    .ToList();
            }
            else
            {
                rows = (await monthQuery
                    .Where(u => u.ReceiverUser != null)
                    .GroupBy(u => u.ReceiverUser.DepartmentName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .Select(x => (x.Name, x.Count))
                    .ToList();
            }

            return rows
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Name) ? "Unassigned" : x.Name!.Trim())
                .Select(g => new KafoInsightOrgCountDto
                {
                    Name = g.Key,
                    Count = g.Sum(x => x.Count)
                })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToList();
        }
    }
}
