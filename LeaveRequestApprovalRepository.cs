using Core.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
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
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;
using Microsoft.EntityFrameworkCore;


namespace Nupco.EF.Repositories
{
    public class LeaveRequestApprovalRepository : BaseRepository<LeaveRequestApproval>, ILeaveRequestApprovalRepository
    {
        private readonly LeaveRequestApproval_Assembler _Assembler;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "LeaveRequestApprovalData";

        public LeaveRequestApprovalRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new LeaveRequestApproval_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public async Task<OperationOutput> CreateNewLeaveRequestApproval(LeaveRequestApprovalDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;

                // Set initial status and approval level for new leave request

                var _model = _Assembler.WriteDal(entityDto);
                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                // Create initial approval record

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

        public async Task<OperationOutput> AddNewAsync(LeaveRequestApprovalDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;

                // Set initial status and approval level for new leave request

                var _model = _Assembler.WriteDal(entityDto);
                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                // Create initial approval record

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

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<LeaveRequestApprovalDto> entitiesDto)
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
                var LeaveRequestApprovals = await FindAllAsync(i => i.IsDeleted == false && i.ApproverUserId == userId);
                var _LeaveRequestApprovals = _Assembler.WriteListDto(LeaveRequestApprovals);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_LeaveRequestApprovals, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(LeaveRequestApprovalDto entityDto)
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

        public async Task<OperationOutput> UpdateEntityAsync(LeaveRequestApprovalDto entityDto, object key)
        {
            try
            {
                var _LeaveRequestApproval = await GetByIdAsync((int)entityDto.Id);
                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _LeaveRequestApproval);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<LeaveRequestApprovalDto> entities)
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
                var LeaveRequestApprovals = new List<LeaveRequestApproval>();

                var _LeaveRequestApprovalList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                LeaveRequestApprovals = _LeaveRequestApprovalList.Where(i => i.IsDeleted == false).ToList();

                var _LeaveRequestApprovalsDto = _Assembler.WriteListDto(LeaveRequestApprovals);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_LeaveRequestApprovalsDto, counts);
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

                var _LeaveRequestApproval = await FindAsync(f => f.Id == id, stringArray);
                if (_LeaveRequestApproval is not null)
                {
                    var _entity = _Assembler.WriteDto(_LeaveRequestApproval);
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

        public async Task<OperationOutput> GetAllApprovalPerRequest(int RequestId)
        {
            try
            {
                var _entities = await FindAllAsync(i => i.IsDeleted == false && i.LeaveRequestId == RequestId);
                foreach (var item in _entities.AsEnumerable())
                {
                    var x  = await  _context.CompanyEmployees.FirstOrDefaultAsync(e => e.UserId == item.ApproverUserId);
                    if(x is not null)   
                    item.ApproverUserId = x.FullName;
                }
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

        public async Task<OperationOutput> GePendingApprovalsToApproveforUser(string UserId)
        {
            try
            {
                var _entities = await _context.LeaveRequestApprovals
                    .Include(x => x.LeaveRequest)
                    .Where(i => i.IsDeleted == false && i.ApproverUserId == UserId && i.LeaveRequest.Type.NameEn != "TimeSheet")
                    .ToListAsync();

                foreach (var item in _entities.AsEnumerable())
                {
                    var x = await _context.CompanyEmployees.FirstOrDefaultAsync(e => e.UserId == item.LeaveRequest.RequesterUserId);
                    if (x is not null)
                        item.LeaveRequest.RequesterUserId = x.FullName;
                }

                var _entitiess = _Assembler.WriteListDto(_entities);


                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entitiess, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GePendingApprovalsToApproveforUser_timeSheet(string UserId)
        {
            try
            {
                var _entities = await _context.LeaveRequestApprovals
                    .Include(x => x.LeaveRequest)
                    .Where(i => i.IsDeleted == false && i.ApproverUserId == UserId && i.LeaveRequest.Type.NameEn == "TimeSheet")
                    .ToListAsync();

                foreach (var item in _entities.AsEnumerable())
                {
                    var x = await _context.CompanyEmployees.FirstOrDefaultAsync(e => e.UserId == item.LeaveRequest.RequesterUserId);
                    if (x is not null)
                        item.LeaveRequest.RequesterUserId = x.FullName;
                }

                var _entitiess = _Assembler.WriteListDto(_entities);


                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entitiess, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

    }
}