using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Pic;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;
using Comment = Nupco.DAL.Models.Comment;

namespace Nupco.EF.Repositories
{
    public class ReleaseKnownIssueRepository : BaseRepository<ReleaseKnownIssue>, IReleaseKnownIssueRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ReleaseKnownIssue_Assembler _Assembler;

        public ReleaseKnownIssueRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _Assembler = new ReleaseKnownIssue_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(ReleaseKnownIssueDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(entity);
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

        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                var ReleaseKnownIssues = new List<ReleaseKnownIssue>();
                var _ReleaseKnownIssues = await FindAllAsync(i => i.IsDeleted == false && i.PicReleaseId == _filter.PicReleaseId, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                ReleaseKnownIssues = _ReleaseKnownIssues.Where(i => i.IsDeleted == false).ToList();
                var _ReleaseKnownIssuesDto = _Assembler.WriteListDto(ReleaseKnownIssues);
                var counts = Count(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_ReleaseKnownIssuesDto, counts);
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
                var _entities = _Assembler.WriteListDto(entities);

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
                var ReleaseKnownIssue = await FindAsync(f => f.Id == id);
                if (ReleaseKnownIssue != null)
                {
                    var _ReleaseKnownIssue = _Assembler.WriteDto(ReleaseKnownIssue);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_ReleaseKnownIssue, 1);
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

        public async Task<OperationOutput> UpdateEntity(ReleaseKnownIssueDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {

                    _Assembler.WriteDal(entity, _entity);
                    _entity.UpdatedDate = DateTime.Now;
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
