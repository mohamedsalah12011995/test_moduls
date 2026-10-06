using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    internal class SearchRepository : BaseRepository<object>, ISearchRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        public SearchRepository(ApplicationDbContext context, ILogger logger)
            : base(context, logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OperationOutput> SearchInAllTablesAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var results = new List<SearchResultDto>();

                // Call stored procedure
                var conn = _context.Database.GetDbConnection();
                await using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SearchAllTables";
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@SearchStr", keyword));

                    if (conn.State != System.Data.ConnectionState.Open)
                        await conn.OpenAsync();

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new SearchResultDto
                            {
                                TableName = reader.IsDBNull(0) ? "" : reader.GetString(0).Split('.')[1].Trim('[', ']'),   // TableName
                                EntityId = reader.IsDBNull(1) ? "" : reader.GetString(1),   // EntityId
                                RowData = reader.IsDBNull(2) ? "" : reader.GetString(2)    // Full row as XML/JSON
                            });
                        }
                    }
                }

                var resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(results, results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching in all tables");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
    }
}
