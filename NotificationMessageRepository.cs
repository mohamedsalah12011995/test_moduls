using Core.Helpers;
using EmpMobile.DAL.Dto;
using EmpMobile.DAL.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using System.Data;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    internal class NotificationMessageRepository : BaseRepository<NotificationMessage>, INotificationMessageRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly NotificationMessage_Assembler _NotificationMessage_Assembler;
        string[] stringArray = { "Entity", "NotificationAction", "CreatedByUser", "UpdatedByUser", "NotificationAction" };

        public NotificationMessageRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _NotificationMessage_Assembler = new NotificationMessage_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(NotificationMessageObjDto entity)
        {
            try
            {
                //if (await IsExistActionEntity(entity.EntityId, entity.ActionId))    
                //     return ResultOutputData.GenearetResultOutputIsRegester();
                
                var _entity = _NotificationMessage_Assembler.WriteDal(entity);
                _entity.IsActive = true;
                _entity.IsDelete = false;
                _entity.CreatedDate = DateTime.Now;
                var obj = await AddAsync(_entity);
                await _context.SaveChangesAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(obj.Adapt<NotificationMessageDto>(), 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        private async Task<bool> IsExistActionEntity(int entityId, int actionId, int? excludeId = null)
        {
            return await _context.NotificationMessage.AnyAsync(a =>
                a.EntityId == entityId &&
                a.ActionId == actionId &&
                a.IsDelete == false &&
                (!excludeId.HasValue || a.Id != excludeId.Value)
            );
        }


        public async Task<OperationOutput> DeleteEntity(int id,string userId)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsDelete = true;
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

        public async Task<OperationOutput> GetAllByPagenationAsync(FilterByNotification _filter)
        {
            try
            {
                var spec = Specification<NotificationMessage>.All.And(new NotificationMessageSpecification(_filter));
                var NotificationMessage = new List<NotificationMessage>();

                var _NotificationMessage = await FindAllAsync(spec.ToExpression(), stringArray);
                NotificationMessage = _NotificationMessage.Where(i => i.IsDelete == false).ToList();

                var _NotificationMessageDto = _NotificationMessage_Assembler.WriteListDto(NotificationMessage);
                var counts = Count(i => i.IsDelete == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_NotificationMessageDto, counts);
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
                var entities = await FindAllAsync(i => i.IsDelete == false, stringArray);
                var _entities = _NotificationMessage_Assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDelete == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<List<NotificationMessageDto>> GetNotificationMessageByEntityAndActionId(int entityId , int actionId)
        {
            try
            {
                var NotificationMessages = await FindAllAsync(f => f.EntityId == entityId && f.ActionId== actionId && f.IsDelete == false, stringArray);
                if (NotificationMessages.Count() > 0)
                    return _NotificationMessage_Assembler.WriteListDto(NotificationMessages).ToList();

                return new List<NotificationMessageDto>();
            }
            catch (Exception)
            {
                return new List<NotificationMessageDto>();
            }
        }

        public async Task<OperationOutput> GetAllNotificationActions()
        {
            try
            {
                var entities = await _context.NotificationActions.Where(i=> i.IsDelete==false).ToListAsync();
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, entities.Count());
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<List<NotificationMessageDto>> GetDataWithEntityId(int id)
        {
            try
            {
                var NotificationMessages = await FindAllAsync(f => f.EntityId == id && f.IsDelete == false, stringArray);
                if (NotificationMessages.Count() > 0)
                    return _NotificationMessage_Assembler.WriteListDto(NotificationMessages).ToList();

                return new List<NotificationMessageDto>();
            }
            catch (Exception)
            {
                return new List<NotificationMessageDto>();
            }
        }
        public async Task<NotificationMessageDto?> GetDataByNotificationAction(int actionId , int entityId)
        {
            try
            {
                var NotificationMessage = await FindAsync(f=> f.ActionId== actionId  && f.EntityId== entityId && f.IsDelete==false, stringArray);
                if (NotificationMessage != null)
                   return _NotificationMessage_Assembler.WriteDto(NotificationMessage);

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var NotificationMessage = await FindAsync(f=> f.Id==id, stringArray);
                if (NotificationMessage != null)
                {
                    var _NotificationMessage = _NotificationMessage_Assembler.WriteDto(NotificationMessage);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_NotificationMessage, 1);
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

        public async Task<OperationOutput> UpdateEntity(NotificationMessageObjDto entity)
        {
            try
            {
                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                //if (await IsExistActionEntity(entity.EntityId, entity.ActionId, entity.Id))
                //    return ResultOutputData.GenearetResultOutputIsRegester();

                _entity.TitleAr = entity.TitleAr;
                _entity.TitleEn = entity.TitleEn;
                _entity.BodyAr = entity.BodyAr;
                _entity.BodyEn = entity.BodyEn;
                _entity.EntityId = entity.EntityId;
                _entity.ActionId = entity.ActionId;
                _entity.Description = entity.Description;
                _entity.UpdatedDate = DateTime.Now;
                _entity.IsMobile = entity.IsMobile;
                _entity.IsActive = true;
                _entity.IsDelete = false;

                var _model = Update(_entity);
                await SaveChangesAsync();

                var resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(_model, 1);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(NotificationMessageDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<NotificationMessageDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<NotificationMessageDto> entities)
        {
            throw new NotImplementedException();
        }

        public Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            throw new NotImplementedException();
        }
    }
}
