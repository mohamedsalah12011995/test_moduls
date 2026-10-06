using Core.Helpers;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.GeneralMessages.Interfaces;
using System.Data;
using System.Data.Entity;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Wordprocessing;



namespace Nupco.EF.Repositories
{
    internal class GeneralMessagesRepository : BaseRepository<Nupco.DAL.Models.GeneralMessage> , IGeneralMessagesRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly GeneralMessage_Assembler _GeneralMessages_Assembler =new GeneralMessage_Assembler();

        public GeneralMessagesRepository(ApplicationDbContext context, ILogger logger, ApplicationDbContextAttendance contextAttendance) : base(context, logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<OperationOutput> ActivateGeneralMessages(string key, string valueAr,string valueEn)

        {
            try
            {
                var find = await _context.GeneralMessages.FirstOrDefaultAsync(f => f.Key == key);
                if (find != null)
                {
                    find.ValueAr = valueAr;
                    find.ValueEn = valueEn;
                    var entity = _context.GeneralMessages.Update(find);
                    await _context.SaveChangesAsync();

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


        public async Task<OperationOutput> GetAllGeneralMessages()
        {
            try
            {
                var GeneralMessages = await FindAllAsync(i=> i.IsDeleted==false);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(GeneralMessages, GeneralMessages.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetSettingByKey(string key)
        {
            try
            {
                var find = await _context.GeneralMessages.FirstOrDefaultAsync(f => f.Key == key);
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

        public async Task<OperationOutput> AddNewAsync(GeneralMessageDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                var _model = _GeneralMessages_Assembler.WriteDal(entity);
                var _entity = await AddAsync(_model);
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

        public async Task<OperationOutput> GetAllByPagenationAsync(FilterGeneralMessages _filter)
        {
            try
            {
                List<GeneralMessageDto> ListGeneralMessages = new List<GeneralMessageDto>();
                int counts = 0;
                if (_filter.parentId != null)
                {
                    var spec = Specification<GeneralMessage>.All.And(new GeneralMessagesSpecification(_filter));

                    var GeneralMessages = await FindAllAsync(i => i.IsDeleted == false && i.ParentId == _filter.parentId);

                    ListGeneralMessages = _GeneralMessages_Assembler.WriteListDto(GeneralMessages.ToList());
                    counts = await CountAsync(i => i.IsDeleted == false && i.ParentId == _filter.parentId);
                }
                else
                {

                    var spec = Specification<GeneralMessage>.All.And(new GeneralMessagesSpecification(_filter));

                    var GeneralMessages = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                    ListGeneralMessages = _GeneralMessages_Assembler.WriteListDto(GeneralMessages.ToList());
                    counts = await CountAsync(i => i.IsDeleted == false);
                }

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(ListGeneralMessages, counts);
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

                var entities = await FindAllAsync(i => i.IsDeleted == false);
                var _entities = _GeneralMessages_Assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var GeneralMessages = await GetByIdAsync(id);
                if (GeneralMessages != null)
                {
                    var _GeneralMessages = _GeneralMessages_Assembler.WriteDto(GeneralMessages);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_GeneralMessages, 1);
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

        public async Task<OperationOutput> UpdateEntity(GeneralMessageDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.ValueAr = entity.ValueAr;
                    _entity.ValueEn = entity.ValueEn;
                    _entity.ParentId = entity.ParentId;

                    var _model = Update(_entity);
                    await SaveChangesAsync();

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_model, 1);
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



        public async Task<OperationOutput> GetGeneralMessgesParent()
        {
            var entities = await FindAllAsync(i => i.IsDeleted == false&&i.ParentId==null);
            var _entities = _GeneralMessages_Assembler.WriteListDto(entities);

            ResultOutputData resultOutput = new ResultOutputData();
            var count = await CountAsync(i => i.IsDeleted == false);
            var _result = resultOutput.GenearetResultOutput(_entities, count);
            return _result;
        }
        public async Task<OperationOutput> GetGeneralMessgesGroupBy()
        {
            try
            {
                var results = await FindAllAsync(i => i.IsDeleted == false);

                //var ListParent = results.Where(i => i.ParentId == null).ToList();
                //var ListChaild = results.Where(i => i.ParentId != null).ToList();

                var listOfResult = results.Where(i => i.ParentId == null).Select(s => new
                {
                    data = _GeneralMessages_Assembler.WriteDto(s),
                    children =_GeneralMessages_Assembler.WriteListDto( results.Where(i => i.ParentId != null && i.ParentId == s.Id).ToList())
                }).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(listOfResult, listOfResult.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }


        }

        public Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            throw new NotImplementedException();
        }
    }
}
