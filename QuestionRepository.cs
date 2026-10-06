using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
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
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    internal class QuestionRepository :BaseRepository<Question>, IQuestionsRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Question_Assembler _Assembler;

        private string[] stringArray = { "Country" };


        public QuestionRepository(ApplicationDbContext context, ILogger logger) :base(context,logger)
        {
            _Assembler = new Question_Assembler();
            _logger = logger;
            _context = context;

        }

        public async Task<OperationOutput> CreateQuestion(QuestionObjDto model)
        {
            try
            {
                model.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(model);

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

        public async Task<OperationOutput> GetAllByPagenation(PagenationBy _filter)
        {
            try
            {
                var _entities = new List<Question>();
                if (_filter.pageNumber == null )
                {
                    if(!string.IsNullOrEmpty(_filter.From) && !string.IsNullOrEmpty(_filter.To))
                    {

                        var startDate = Dates.ConvertStringToDate(_filter.From);
                        var endDate = Dates.ConvertStringToDate(_filter.To);

                        _entities = await _context.Questions.Where(i => i.CreatedDate.Value.Date >= startDate.Value.Date && i.CreatedDate.Value.Date <= endDate.Value.Date)
                                                             .Include(i => i.CreatedByUsers)
                                                             .Include(i => i.Entity)
                                                             .OrderByDescending(o => o.Id)
                                                             .ToListAsync();
                    }

                    else
                    {
                        _entities = await _context.Questions
                                     .Include(i => i.CreatedByUsers)
                                     .Include(i => i.Entity)
                                     .OrderByDescending(o => o.Id)
                                     .ToListAsync();
                    }


                }
                else
                {


                    _entities = await _context.Questions
                      .Include(i => i.CreatedByUsers)
                      .OrderByDescending(o => o.Id)
                      .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                      .ToListAsync();
                }
                var QuestionsList = _Assembler.WriteListDto(_entities);



                var counts = await CountAsync();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(QuestionsList, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllQuestions()
        {
            try
            {
                var _entities = await GetAllAsync();
                var QuestionsList = _Assembler.WriteListDto(_entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(QuestionsList, count);
                return  _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllQuestionsbyUserId(string userId)
        {
            try
            {
                var _entities = await _context.Questions.Include(c => c.CreatedByUsers).ToListAsync();
                var QuestionsList = _Assembler.WriteListDto(_entities);


                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(QuestionsList, count);
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

    }
}
