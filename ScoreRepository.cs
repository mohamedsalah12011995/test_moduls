using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto.Game;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;

namespace Nupco.EF.Repositories
{
    internal class ScoreRepository : BaseRepository<Score>, IScoreRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Score_Assembler _Score_Assembler;

        private string[] stringArray = { "User","Game" };


        public ScoreRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Score_Assembler = new Score_Assembler();
            _logger = logger;
            _context = context;

        }

        public async Task<OperationOutput> ActivatedOrUnActivated(int id, bool activate)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
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

        public async Task<OperationOutput> AddNewAsync(ScoreDto entity)
        {
            try
            {
                var _model = _Score_Assembler.WriteDal(entity);
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

        public async Task<OperationOutput> DeleteEntity(int id,string userId)
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

        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                var spec = Specification<Score>.All.And(new ScoreSpecification(_filter));

                var Score = await FindAllAsync(i => i.GameId > 0, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize,stringArray);
                var _Score = _Score_Assembler.WriteListDto(Score);
                var counts = await CountAsync();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Score, counts);
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

                var entities = await FindAllAsync(i => i.GameId > 0, stringArray);
                var _entities = _Score_Assembler.WriteListDto(entities);

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
                var Score = await GetByIdAsync(id);
                if (Score != null)
                {
                    var _Score = _Score_Assembler.WriteDto(Score);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Score, 1);
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

        public async Task<OperationOutput> UpdateEntity(ScoreDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.UpdatedDate = DateTime.Now;
                    _entity.UserId = entity.UserId;
                    _entity.GameId = entity.GameId;
                    _entity.Points = entity.Points;


                    var _model =  Update(_entity);
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


        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(ScoreDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<ScoreDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<ScoreDto> entities)
        {
            throw new NotImplementedException();
        }



        public async Task<OperationOutput> StartOrEndScore(StartEndScore scoreDto)
        {
            if (Guid.TryParse(scoreDto.SessionId, out Guid sessionGuid))
            {

                // Check if a score already exists for the user/game/today
                var existingScore = await _context.Scores
                        .FirstOrDefaultAsync(s => s.SessionId == sessionGuid);


                Score entity;

                if (existingScore == null)
                {
                    entity = await StartGameAsync(scoreDto);
                }
                else
                {
                    entity = await EndGameAsync(scoreDto, existingScore);
                }

                // Build output
                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(entity, 1);
                return result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> TopUserScore(int size = 5)
        {


                var TopScore  = await _context.Scores
                                .GroupBy(s => new { s.UserId, s.User.UserName })
                                .Select(g => new
                                {
                                    UserId = g.Key.UserId,
                                    UserName = g.Key.UserName,
                                    TotalPoints = g.Sum(s => s.Points),
                                    TopPoint = g.Max(s => s.Points)

                                })
                                .OrderByDescending(x => x.TotalPoints)
                                .Take(size)
                                .ToListAsync();


            // Build output
            ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(TopScore, TopScore.Count());
                return result;
           
        }

        public async Task<OperationOutput> TopUserScoreByGameId(int size = 5,int gameId=0)
        {


            var TopScore = await _context.Scores.Include(i=> i.Game).Where(i=> i.GameId ==gameId)
                            .GroupBy(s => new { s.UserId, s.User.UserName })
                            .Select(g => new
                            {
                                UserId = g.Key.UserId,
                                UserName = g.Key.UserName,
                                TotalPoints = g.Sum(s => s.Points),
                                TopPoint = g.Max(s => s.Points)

                            })
                            .OrderByDescending(x => x.TotalPoints)
                            .Take(size)
                            .ToListAsync();


            // Build output
            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(TopScore, TopScore.Count());
            return result;

        }

        public async Task<Score> StartGameAsync(StartEndScore scoreDto)
        {
            var sessionId = Guid.NewGuid(); // generate a unique session for this play

            // Create new Score record when starting a game
            var entity = new Score
            {
                GameId = scoreDto.GameId,
                UserId = scoreDto.UserId,
                SessionId = sessionId,
                Points = 0, // start with 0
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

           var result= await _context.Scores.AddAsync(entity);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Score> EndGameAsync(StartEndScore scoreDto,Score existingScore)
        {
            // Update score and time
            existingScore.Points = scoreDto.Points;
            existingScore.UpdatedDate = DateTime.UtcNow;

           var result = _context.Scores.Update(existingScore);
            await _context.SaveChangesAsync();

            return result.Entity;
        }


    }
}
