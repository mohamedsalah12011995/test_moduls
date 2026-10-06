using Core.Helpers;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    public class BackgroundJobHistoryRepository : BaseRepository<BackgroundJobHistory>, IBackgroundJobHistoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly BackgroundJobHistory_Assembler _BackgroundJobHistory_Assembler;
        public BackgroundJobHistoryRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _BackgroundJobHistory_Assembler = new BackgroundJobHistory_Assembler();
        }

        public Task<OperationOutput> ActivatedOrUnActivated(int id, bool activate)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationOutput> AddNewAsync(BackgroundJobHistoryDto entity)
        {
            try
            {
                var _model = _BackgroundJobHistory_Assembler.WriteDal(entity);
                var _entity = await AddAsync(_model);
                await _context.SaveChangesAsync();


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
                Delete(find);
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

        public Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {

                var entities = await GetAllAsync();
                var _entities = _BackgroundJobHistory_Assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
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
                var Group = await GetByIdAsync(id);
                if (Group != null)
                {
                    var _Group = _BackgroundJobHistory_Assembler.WriteDto(Group);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Group, 1);
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


        public async Task<OperationOutput> UpdateEntity(BackgroundJobHistoryDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.Name = entity.Name;
                    _entity.FireTime = entity.FireTime;

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
    }
}
