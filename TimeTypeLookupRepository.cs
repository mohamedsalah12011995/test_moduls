using Core.Helpers;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.Extensions.Logging;
using Nupco.Core.Assembler;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    public class TimeTypeLookupRepository : BaseRepository<EmployeeOfTheMonth>, ITimeTypeLookupRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        public TimeTypeLookupRepository(
            ApplicationDbContext context,
            ILogger logger) : base(context, logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TimeTypeLookup>> GetAllDataAsync()
        {
            return await _context.Set<TimeTypeLookup>().ToListAsync();
        }

        public async Task<TimeTypeLookup> GetByKeyAsync(string Key)
        {
            return await _context.Set<TimeTypeLookup>().FirstOrDefaultAsync(E => E.Key == Key);
        }
    }
}
