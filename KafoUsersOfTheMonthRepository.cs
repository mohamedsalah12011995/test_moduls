using Core.Helpers;
using Microsoft.Extensions.Logging;
using Nupco.Core.Consts;
using Nupco.Core.Dto.Kafo;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models.Kafo;
using System.Data;
using System.Linq.Expressions;
using static Core.Helpers.Enums;

namespace Nupco.EF.Repositories
{
    public class KafoUsersOfTheMonthRepository : BaseRepository<KafoUsersOfMonth>, IKafoUserOfTheMonthRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        private string[] stringArray = { "User" };


        public KafoUsersOfTheMonthRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<OperationOutput> GetTopKafoUserOfTheMonth(KafoUserOfTheMonthOrYearParams param)
        {
            try
            {
               var _entities = await GetTopUsersByDate(param);
                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(_entities, _entities.Count());
            }
            catch (Exception ex)
            {
                return ResultOutputData.GenearetResultOutputCatch();

            }
        }
        public async Task<OperationOutput> GetTopKafoUserOfTheYear(KafoUserOfTheMonthOrYearParams param)
        {
            try
            {
                param.isYearly = true;
                var _entities = await GetTopUsersByDate(param);
                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(_entities, _entities.Count());
            }
            catch (Exception ex)
            {
                return ResultOutputData.GenearetResultOutputCatch();

            }
        }
        private async Task<List<KafoUsersOfMonthResponse>> GetTopUsersByDate(KafoUserOfTheMonthOrYearParams param)
        {
            try
            {
                var parsedDate = Dates.ConvertStringToDate(param.date);
                if (parsedDate == null) return new List<KafoUsersOfMonthResponse>();

                Expression<Func<KafoUsersOfMonth, bool>> criteria = i =>
                    i.CreatedDate.Value.Year == parsedDate.Value.Year &&
                    (param.isYearly || i.CreatedDate.Value.Month == parsedDate.Value.Month);

                var entities = await FindAllAsync(
                    criteria: criteria,
                    skip: 0,
                    take: param.take,
                    includes: new[] { "User" },
                    orderBy: i => i.Count,
                    orderByDirection: OrderBy.Descending
                );

                return entities.Select(s => new KafoUsersOfMonthResponse
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    FullName = $"{s.User.FirstName} {s.User.LastName}",
                    Count = s.Count
                }).ToList();
            }
            catch (Exception)
            {
                return new List<KafoUsersOfMonthResponse>();
            }
        }
    }
}
