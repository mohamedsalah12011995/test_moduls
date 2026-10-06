using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
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
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;
using Comment = Nupco.DAL.Models.Comment;

namespace Nupco.EF.Repositories
{
    internal class VoteRepository : BaseRepository<Vote>, IVoteRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Vote_Assembler _Assembler;

        private string[] stringArray = null;

        private readonly Comment_Assembler _AssemblerComment = new Comment_Assembler();

        public VoteRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Assembler = new Vote_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(VoteDto entity)
        {
            try
            {
                if (!String.IsNullOrEmpty(entity.OriginalPicBase64))
                {
                    entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entity.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entity.OriginalPicBase64, 1024, entity.pathToSave, false, 0, entity.pathToSave) : entity.OriginalPic;
                }

                var _model = _Assembler.WriteDal(entity);




                    var _startDate = DateTime.Parse(entity.StartDate.ToString());
                    var _endDate = DateTime.Parse(entity.EndDate.ToString());

                    _model.StartDate = _startDate;
                    _model.EndDate = _endDate;

                    _model.IsActive = true;
                    _model.IsDeleted = false;
                    _model.CreatedBy = entity.CreatedBy;
                    _model.CreatedDate = DateTime.Now;
                    _model.CommentLable = entity.CommentLable;

                    var _entity = await _context.Votes.AddAsync(_model);
                    await _context.SaveChangesAsync();

                if (entity.VoteDataSources.Count > 0)
                {

                    foreach (var item in entity.VoteDataSources)
                    {
                        VoteDataSource dataSource = new()
                        {
                            VoteId = _entity.Entity.Id,
                            TitleAr = item.TitleAr,
                            TitleEn = item.TitleEn,
                            IsActive = true,
                            IsDeleted = false,
                            ContentAr = item.ContentAr,
                            ContentEn = item.ContentEn,
                        };
                        await _context.VoteDataSources.AddAsync(dataSource);
                    }

                    await _context.SaveChangesAsync();
                }

                var _entityDto = _Assembler.WriteDto(_entity.Entity);
                _entityDto.VoteDataSources = _entity.Entity.VoteDataSource.Select(s => new VoteDataSourceDto
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    ContentAr = s.ContentAr,
                    ContentEn = s.ContentEn,
                    IsActive = s.IsActive,
                    IsDeleted = s.IsDeleted,
                    VoteId = s.VoteId,
                    OriginalPic = s.OriginalPic,
                }).ToList();

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

        public async Task<OperationOutput> CreateVoteDataSources(List<VoteDataSourceDto> modelDtos)
        {
            try
            {
                var _modelList = new List<VoteDataSource>();
                if (modelDtos.Count > 0)
                {
                    foreach (var item in modelDtos)
                    {
                        var _model = new VoteDataSource
                        {
                            TitleAr = item.TitleAr ?? "",
                            TitleEn = item.TitleEn,
                            ContentAr = item.ContentAr,
                            ContentEn = item.ContentEn,
                            IsActive = true,
                            IsDeleted = false,
                            VoteId = item.VoteId
                        };
                        _modelList.Add(_model);
                    }
                    await _context.VoteDataSources.AddRangeAsync(_modelList);
                    await _context.SaveChangesAsync();

                    var _listAfterSaved = await _context.VoteDataSources.Where(s => s.VoteId == modelDtos[0].VoteId)
                       .Select(item => new VoteDataSourceDto
                       {
                           Id = item.Id,
                           TitleAr = item.TitleAr,
                           TitleEn = item.TitleEn,
                           ContentAr = item.ContentAr,
                           ContentEn = item.ContentEn,
                           IsActive = true,
                           IsDeleted = false,
                           VoteId = item.VoteId
                       }).ToListAsync();



                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_listAfterSaved, _listAfterSaved.Count);
                    return _result;

                }
                else
                {
                    var Result = ResultOutputData.GenearetResultOutputCatch();
                    return Result;
                }
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> DeleteDataSource(int id, string userId)
        {
            try
            {
                var find = await _context.VoteDataSources.FirstOrDefaultAsync(f => f.Id == id);
                find.IsDeleted = true;

                _context.VoteDataSources.Update(find);

                var dataSourceInVoteUser = await _context.VoteUsers.Where(f => f.VoteDataSourceId == id).ToListAsync();
                _context.VoteUsers.RemoveRange(dataSourceInVoteUser);

                await _context.SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
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
                var find = await _context.Votes.FirstOrDefaultAsync(f => f.Id == id);
                var votsDataSources = await _context.VoteDataSources.Where(f => f.VoteId == find.Id).ToListAsync();
                foreach (var item in votsDataSources)
                {
                    item.IsDeleted = true;
                }
                _context.VoteDataSources.UpdateRange(votsDataSources);

                find.IsDeleted = true;
                find.DeletedDate = DateTime.Now;
                find.DeletedBy = userId;

                _context.Votes.Update(find);

                var notificationHistory = await _context.NotificationHistories.Where(f => f.RecordId == id.ToString() && f.EntityId == find.EntityId).ToListAsync();
                if (notificationHistory.Any())
                    _context.NotificationHistories.RemoveRange(notificationHistory);


                await _context.SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> export(int voteId, int? dataSourceId)
        {
            try
            {
                var votes = await _context.VoteUsers
                    .Where(i => i.VoteId == voteId)
                    .Include(i => i.Vote).ThenInclude(i => i.Entity)
                    .Include(i => i.VoteDataSource)
                    .Include(i => i.User).OrderBy(i => i.Id).Select(s => new
                    {
                        VoteAnswerId = s.VoteDataSourceId,
                        VoteNameAr = s.Vote.TitleAr,
                        VoteNameEn = s.Vote.TitleEn,
                        VoteAnswerAr = s.VoteDataSource.TitleAr,
                        VoteAnswerEn = s.VoteDataSource.TitleEn,
                        UserName = s.User.UserName,
                        FullName = s.User.FirstName + " " + s.User.LastName,
                        entityAr = s.Vote.Entity.NameAr,
                        entityEn = s.Vote.Entity.NameEn,
                    }).ToListAsync();

                if (dataSourceId != 0)
                {
                    votes = votes.Where(i => i.VoteAnswerId == dataSourceId).ToList();
                }

                

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(votes, votes.Count());
                return _result;
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
                var spec = Specification<Vote>.All.And(new VoteSpecification(_filter));

                var _votes = await _context.Votes
                    .Where(spec.ToExpression()).Where(i => i.IsDeleted == false)
                    .OrderByDescending(o => o.CreatedDate)
                    .Include(x => x.CreatedByUser)
                    .Include(x => x.UpdatedByUser)
                    .Include(x => x.DeletedByUser)
                    .Skip((int)_filter.pageSize * (int)_filter.pageNumber).Take((int)_filter.pageSize)   
                    .ToListAsync();

                var votes = _Assembler.WriteListDto(_votes);

                var counts = _context.Votes.Count(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(votes, counts);
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
                var _entities = await _context.Votes.Where(i => i.IsDeleted == false)
                    .Include(x => x.CreatedByUser)
                    .Include(x => x.UpdatedByUser)
                    .Include(x => x.DeletedByUser)
                    .AsNoTracking().ToListAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var _entitiesMap = _Assembler.WriteListDto(_entities);
                var count = await _context.Votes.CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entitiesMap, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllVotesTypes()
        {

            try
            {
                var _entities = await _context.VotesTypes.AsNoTracking().Select(s => new
                {
                    id = s.Id,
                    nameAr = s.NameAr,
                    nameEn = s.NameEn,
                }).ToListAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await _context.VotesTypes.CountAsync();
                var _result = resultOutput.GenearetResultOutput(_entities.ToList(), count);
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
                var vote = await _context.Votes.Include(i => i.VoteDataSource).FirstOrDefaultAsync(i => i.Id == id);
                if (vote is not null)
                {


                    var _Vote = _Assembler.WriteDto(vote);
                    var _commentsVote = await _context.Comments.Where(i => i.EntityId == vote.EntityId && i.ItemId == vote.Id.ToString()).AsNoTrackingWithIdentityResolution().Include(i=> i.CreatedUser).ToListAsync();
                    _Vote.Comments = _AssemblerComment.WriteListDto(_commentsVote);
                    _Vote.VoteDataSources = vote.VoteDataSource.Where(i => i.IsDeleted == false).Select(s => new VoteDataSourceDto
                    {
                        Id = s.Id,
                        TitleAr = s.TitleAr,
                        TitleEn = s.TitleEn,
                        ContentAr = s.ContentAr,
                        ContentEn = s.ContentEn,
                        IsActive = s.IsActive,
                        IsDeleted = s.IsDeleted,
                        VoteId = s.VoteId,
                        OriginalPic = s.OriginalPic,
                        CountVote = _context.VoteUsers.Count(i => i.VoteId == s.VoteId && i.VoteDataSourceId == s.Id),
                    }).ToList();
                    _Vote.TotalVote = _Vote.VoteDataSources.Sum(s => s.CountVote).Value;
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Vote, 1);
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

        public async Task<OperationOutput> GetVotesByAnswerUserId(string userId)
        {
            try
            {
                var votes = await _context.VoteUsers.Include(i => i.Vote).Where(i => i.UserId == userId).Select(s=> s.Vote).ToListAsync();
                if (votes.Count > 0 )
                {


                    var obj = new
                    {
                        votes = votes ,
                        votesCount = votes.Count()
                    };
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(obj, 1);
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


        public async Task<OperationOutput> GetVoteByUserId(int voteId, string userId)
        {
            try
            {
                var vote = await _context.Votes.Include(i => i.VoteDataSource).FirstOrDefaultAsync(i => i.Id == voteId);
                if (vote is not null)
                {


                    var _Vote = _Assembler.WriteDto(vote);
                    _Vote.IsVote = _context.VoteUsers.Any(i => i.VoteId == voteId && i.UserId == userId);

                    _Vote.VoteDataSources = vote.VoteDataSource.Where(i => i.IsDeleted == false).Select(s => new VoteDataSourceDto
                    {
                        Id = s.Id,
                        TitleAr = s.TitleAr,
                        TitleEn = s.TitleEn,
                        ContentAr = s.ContentAr,
                        ContentEn = s.ContentEn,
                        IsActive = s.IsActive,
                        IsDeleted = s.IsDeleted,
                        VoteId = s.VoteId,
                        OriginalPic = s.OriginalPic,
                        IsVote = _context.VoteUsers.Any(i => i.VoteId == s.VoteId && i.VoteDataSourceId == s.Id && i.UserId == userId),
                        CountVote = _context.VoteUsers.Count(i => i.VoteId == s.VoteId && i.VoteDataSourceId == s.Id),
                    }).ToList();
                    _Vote.TotalVote = _Vote.VoteDataSources.Where(i => i.IsDeleted == false).Sum(s => s.CountVote).Value;

                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Vote, 1);
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

        public async Task<OperationOutput> GetVotesNotAnsweredByUser(string userId)
        {
            try
            {
                // Step 1. Get the IDs of votes that the user has already answered.
                var userAnsweredVoteIds = await _context.VoteUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(vu => vu.UserId == userId && vu.VoteDataSource.IsDeleted == false)
                    .Select(vu => vu.Vote.Id)
                    .Distinct()
                    .ToListAsync();

                // Step 2. Retrieve all active votes (active and within the start/end window) including their VoteDataSource.
                var activeVotes = await _context.Votes
                    .AsNoTrackingWithIdentityResolution()
                    .Where(v => v.IsDeleted == false
                                && v.IsActive == true
                                && v.StartDate <= DateTime.Now
                                && DateTime.Now < v.EndDate)
                    .Include(v => v.VoteDataSource)
                    .ToListAsync();

                // Step 3. Determine votes not answered by the user.
                List<Vote> voteList;
                if (userAnsweredVoteIds.Any())
                {
                    voteList = activeVotes.Where(v => !userAnsweredVoteIds.Contains(v.Id)).ToList();
                }
                else
                {
                    voteList = activeVotes;
                }

                // Step 4. Pre-calculate vote counts per VoteDataSource for the votes in voteList.
                var voteIds = voteList.Select(v => v.Id).ToList();
                var voteDataSourceCountsList = await _context.VoteUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(vu => voteIds.Contains((int)vu.VoteId))
                    .GroupBy(vu => new { vu.VoteId, vu.VoteDataSourceId })
                    .Select(g => new
                    {
                        VoteId = g.Key.VoteId,
                        VoteDataSourceId = g.Key.VoteDataSourceId,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Build a dictionary keyed by (VoteId, VoteDataSourceId) for fast lookups.
                var voteCountDict = voteDataSourceCountsList
                    .ToDictionary(x => (x.VoteId, x.VoteDataSourceId), x => x.Count);

                // Step 5. Map each Vote to a DTO and populate the VoteDataSources using the pre-calculated counts.
                var voteListDto = new List<VoteDto>();
                foreach (var vote in voteList)
                {
                    var voteDto = _Assembler.WriteDto(vote);

                    voteDto.VoteDataSources = vote.VoteDataSource
                        .Where(ds => ds.IsDeleted== false)
                        .Select(ds => new VoteDataSourceDto
                        {
                            Id = ds.Id,
                            TitleAr = ds.TitleAr,
                            TitleEn = ds.TitleEn,
                            ContentAr = ds.ContentAr,
                            ContentEn = ds.ContentEn,
                            IsActive = ds.IsActive,
                            IsDeleted = ds.IsDeleted,
                            VoteId = ds.VoteId,
                            OriginalPic = ds.OriginalPic,
                            IsVote = false,
                            // Lookup the count in the precomputed dictionary.
                            CountVote = voteCountDict.TryGetValue((ds.VoteId, ds.Id), out int count) ? count : 0
                        })
                        .ToList();

                    voteDto.TotalVote = voteDto.VoteDataSources
                        .Where(ds => ds.IsDeleted == false)
                        .Sum(ds => ds.CountVote) ?? 0;

                    voteListDto.Add(voteDto);
                }

                // Optional: order the result as needed (here by Id in ascending order)
                var resultVotes = voteListDto.OrderBy(v => v.Id).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(resultVotes, 1);
                return _result;
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<VoteDto> GetLastVoteByUserId(string userId)
        {
            try
            {
                // 1. Get the votes the user has already interacted with.
                var voteListByUser = await _context.VoteUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(f => f.UserId == userId)
                    .Select(i => i.Vote)
                    .Distinct()
                    .ToListAsync();

                // Build a HashSet for fast lookup.
                var userVoteIds = new HashSet<int>(voteListByUser.Select(v => v.Id));

                // 2. Query all available votes (only one call).
                var allVotes = await _context.Votes
                    .AsNoTrackingWithIdentityResolution()
                    .Where(f => f.IsDeleted == false && f.IsActive == true && f.StartDate <= DateTime.Now && DateTime.Now < f.EndDate)
                    .Include(i => i.VoteDataSource)
                    .ToListAsync();

                // 3. Filter out votes that the user has already voted on (if any exist).
                List<Vote> filteredVotes;
                if (userVoteIds.Any())
                {
                    filteredVotes = allVotes.Where(v => !userVoteIds.Contains(v.Id)).ToList();
                }
                else
                {
                    filteredVotes = allVotes;
                }

                // 4. Pre-calculate vote counts for each vote data source.
                //    This runs one query to count VoteUsers grouped by VoteId and VoteDataSourceId.
                var voteDataSourceCounts = await _context.VoteUsers
                    .AsNoTrackingWithIdentityResolution()
                    .Where(vu => userVoteIds.Any() ? userVoteIds.Contains((int)vu.VoteId) : true) // adjust condition as needed
                    .GroupBy(vu => new { vu.VoteId, vu.VoteDataSourceId })
                    .Select(g => new
                    {
                        g.Key.VoteId,
                        g.Key.VoteDataSourceId,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // 5. Build DTOs for each vote.
                var voteListDto = new List<VoteDto>();
                foreach (var vote in filteredVotes)
                {
                    // Convert the Vote to a VoteDto.
                    var voteDto = _Assembler.WriteDto(vote);

                    // Process each VoteDataSource
                    voteDto.VoteDataSources = vote.VoteDataSource
                        .Where(ds => ds.IsDeleted == false)
                        .Select(ds =>
                        {
                            // Look up the count from our pre-calculated results.
                            var countEntry = voteDataSourceCounts.FirstOrDefault(x =>
                                x.VoteId == ds.VoteId && x.VoteDataSourceId == ds.Id);
                            int countVote = countEntry?.Count ?? 0;

                            return new VoteDataSourceDto
                            {
                                Id = ds.Id,
                                TitleAr = ds.TitleAr,
                                TitleEn = ds.TitleEn,
                                ContentAr = ds.ContentAr,
                                ContentEn = ds.ContentEn,
                                IsActive = ds.IsActive,
                                IsDeleted = ds.IsDeleted,
                                VoteId = ds.VoteId,
                                OriginalPic = ds.OriginalPic,
                                IsVote = false,
                                CountVote = countVote
                            };
                        })
                        .ToList();

                    // Calculate TotalVote from the data sources.
                    voteDto.TotalVote = voteDto?.VoteDataSources.Where(c => c.CountVote != null).ToList().Sum(ds => ds.CountVote)??0;
                    if(voteDto!=null)
                        voteListDto.Add(voteDto);
                }

                // 6. Return the "last" vote based on Id.
                //     Ordering descending and taking the first is equivalent to last ascending.
                var lastVote = voteListDto.OrderByDescending(v => v.Id).FirstOrDefault();
                return lastVote;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<OperationOutput> SubmitVote(VoteUsersDto model)
        {
            try
            {
                var _model = new VoteUsers
                {
                    DeviceId = model.DeviceId,
                    UserId = model.UserId,
                    VoteId = model.VoteId,
                    VoteDataSourceId = model.DataSourceId

                };

                var _entity = await _context.VoteUsers.AddAsync(_model);
                await _context.SaveChangesAsync();


                var entityComment = new Comment();
                if (!String.IsNullOrEmpty(model.Comment?.Message))
                {
                    var _comment = _AssemblerComment.WriteDal(model.Comment);
                    _comment.ItemId = model.VoteId.ToString();
                    _comment.EntityId = model.Comment.EntityId;
                    _comment.CreatedBy = model.UserId;
                    _comment.IsAgreeTerms = true;
                    _comment.CreatedDate = DateTime.Now;

                    await _context.Comments.AddAsync(_comment);
                    await _context.SaveChangesAsync();
                }

                var findComment =await _context.Comments.FirstOrDefaultAsync(f => f.EntityId == model.Comment.EntityId && f.ItemId== model.VoteId.ToString());

                var obj = new
                {
                    voteUser = _entity.Entity,
                    comment = findComment
                };
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(obj, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(VoteDto entity)
        {
            try
            {
                var model = entity;
                var _entity = await _context.Votes.Include(i => i.VoteDataSource).FirstOrDefaultAsync(f => f.Id == model.Id);
                var _entityDataSource = await _context.VoteDataSources.Where(f => f.VoteId == model.Id).ToListAsync();

                if (_entity is not null)
                {
                    if (!String.IsNullOrEmpty(entity.OriginalPicBase64))
                    {
                        _entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entity.OriginalPicBase64) ?
                                Images.SaveSingleImageOnServer(entity.OriginalPicBase64, 1024, entity.pathToSave, false, 0, entity.pathToSave) : _entity.OriginalPic;
                    }


                    var _startDate = DateTime.Parse(model.StartDate.ToString());
                    var _endDate = DateTime.Parse(model.EndDate.ToString());

                    _entity.StartDate = _startDate;
                    _entity.EndDate = _endDate;

                    _entity.TitleAr = model.TitleAr;
                    _entity.TitleEn = model.TitleEn;
                    _entity.ContentAr = model.ContentAr;
                    _entity.ContentEn = model.ContentEn;
                    _entity.EntityId = model.EntityId;
                    _entity.ReferenceId = model.ReferenceId;
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.UpdatedBy = model.UpdatedBy;
                    _entity.UpdatedDate = DateTime.Now;
                    _entity.TypeId = model.TypeId;
                    _entity.ActionName = model.ActionName;
                    _entity.CommentLable = model.CommentLable;


                    var _model = _context.Votes.Update(_entity);


                    // remove data source when is typeId rate or feedback
                    if ((model.TypeId == 3 || model.TypeId == 5))
                    {
                        if (_entityDataSource.Count > 0)
                            _context.VoteDataSources.RemoveRange(_entityDataSource);
                    }

                    foreach (var _ds in model.VoteDataSources)
                    {

                        var item = _entityDataSource.FirstOrDefault(f => f.Id == _ds.Id);
                        //var item = await _context.VoteDataSources.FirstOrDefaultAsync(f => f.Id == _ds.Id);
                        if (item is not null)
                        {
                            item.TitleAr = _ds.TitleAr;
                            item.TitleEn = _ds.TitleEn;
                            item.IsActive = true;
                            item.IsDeleted = false;
                            item.ContentAr = _ds.ContentAr;
                            item.ContentEn = _ds.ContentEn;


                            _context.VoteDataSources.Update(item);
                        }
                        else
                        {
                            VoteDataSource _dataSource = new()
                            {
                                VoteId = _model.Entity.Id,
                                TitleAr = _ds.TitleAr,
                                TitleEn = _ds.TitleEn,
                                IsActive = true,
                                IsDeleted = false,
                                ContentAr = _ds.ContentAr,
                                ContentEn = _ds.ContentEn,

                            };
                            await _context.VoteDataSources.AddAsync(_dataSource);
                        }
                    }
                    

                    await _context.SaveChangesAsync();

                    var _entityDto = _Assembler.WriteDto(_model.Entity);
                    _entityDto.VoteDataSources = _model.Entity.VoteDataSource.Where(i => i.IsDeleted == false).Select(s => new VoteDataSourceDto
                    {
                        Id = s.Id,
                        TitleAr = s.TitleAr,
                        TitleEn = s.TitleEn,
                        ContentAr = s.ContentAr,
                        ContentEn = s.ContentEn,
                        IsActive = s.IsActive,
                        IsDeleted = s.IsDeleted,
                        VoteId = s.VoteId,
                        OriginalPic = s.OriginalPic,
                    }).ToList();


                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_entityDto, 1);
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



    }
}
