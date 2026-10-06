using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Nupco.DAL;
using Microsoft.Extensions.Logging;
using System.Data;
using System.ComponentModel;
using ClosedXML.Excel;
using System.IO;
using Nupco.Core.Interfaces;
using Nupco.Core.Consts;
using Nupco.Core.Helpers;
using Microsoft.AspNetCore.Http;
namespace Nupco.EF.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected ApplicationDbContext _context;
        public ILogger _logger;
        protected JWTHelper.Users RequestOwner;
        protected string Token = string.Empty;
        protected IHttpContextAccessor _httpContextAccessor;

        public BaseRepository(ApplicationDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public IEnumerable<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public T GetById(int id)
        {
            return _context.Set<T>().Find(id);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public T Find(Expression<Func<T, bool>> criteria, string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var incluse in includes)
                    query = query.Include(incluse);

            return query.SingleOrDefault(criteria);
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> criteria, string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var incluse in includes)
                    query = query.Include(incluse);

            return await query.SingleOrDefaultAsync(criteria);
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return query.Where(criteria).ToList();
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, string[] includes, int skip, int take)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return query.Where(criteria).Skip(skip).Take(take).ToList();
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int? skip, int? take, string[] includes = null,
            Expression<Func<T, object>> orderBy = null, string orderByDirection = OrderBy.Ascending)
        {
            IQueryable<T> query = _context.Set<T>().Where(criteria);

            if (skip.HasValue)
                query = query.Skip(skip.Value);

            if (take.HasValue)
                query = query.Take(take.Value);

            if (orderBy != null)
            {
                if (orderByDirection == OrderBy.Ascending)
                    query = query.OrderBy(orderBy);
                else
                    query = query.OrderByDescending(orderBy);
            }

            return query.ToList();
        }


        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, params string[] include)
        {
            IQueryable<T> query = this._context.Set<T>();
            query = include.Aggregate(query, (current, inc) => current.Include(inc));
            return await query.Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria,  int skip, int take, params string[] includes)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                query = includes.Aggregate(query, (current, inc) => current.Include(inc));
            return await query.Where(criteria).Skip(skip).Take(take).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, int? skip, int? take,  string[] includes = null,
            Expression<Func<T, object>> orderBy = null, string orderByDirection = OrderBy.Ascending)
        {
            IQueryable<T> query = _context.Set<T>().Where(criteria);

            if (take.HasValue)
                query = query.Take(take.Value);

            if (skip.HasValue)
                query = query.Skip(skip.Value);

            if (orderBy != null)
            {
                if (orderByDirection == OrderBy.Ascending)
                    query = query.OrderBy(orderBy);
                else
                    query = query.OrderByDescending(orderBy);
            }

            return await query.ToListAsync();
        }

        public T Add(T entity)
        {
            _context.Set<T>().Add(entity);
            return entity;
        }

        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            return entity;
        }

        public IEnumerable<T> AddRange(IEnumerable<T> entities)
        {
            _context.Set<T>().AddRange(entities);
            return entities;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
            return entities;
        }

        public T Update(T entity)
        {
            _context.Set<T>().Update(entity);
            return entity;
        }

        public async Task<T> UpdateAsync(T t, object key)
        {
            await Task.Run(() =>
            {
                _context.Entry(key).CurrentValues.SetValues(t);


            });
            return t;
        }

        public void Delete(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }

        public void Attach(T entity)
        {
            _context.Set<T>().Attach(entity);
        }

        public void AttachRange(IEnumerable<T> entities)
        {
            _context.Set<T>().AttachRange(entities);
        }

        public int Count()
        {
            return _context.Set<T>().Count();
        }

        public int Count(Expression<Func<T, bool>> criteria)
        {
            return _context.Set<T>().Count(criteria);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Set<T>().CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> criteria)
        {
            return await _context.Set<T>().CountAsync(criteria);
        }

        public async Task<IList<string>> CheckField(Expression<Func<T, bool>> where, Expression<Func<T, string>> select)
        {
            return await _context.Set<T>().Where(where).Select(select).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllPagenationAsync(Expression<Func<T, bool>> criteria, string[] includes = null, int skip=0, int take=10)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                query = includes.Aggregate(query, (current, inc) => current.Include(inc));
            return await _context.Set<T>().Where(criteria).Skip(skip).Take(take).ToListAsync();
        }

        public byte[] GetFileArray(DataTable dataTable)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                dataTable.TableName = "Sheet1";
                wb.Worksheets.Add(dataTable);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria)
        {
            IQueryable<T> query = this._context.Set<T>();
            return await query.Where(criteria).AsNoTrackingWithIdentityResolution().ToListAsync();
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> criteria)
        {
            IQueryable<T> query = _context.Set<T>();
            return await query.SingleOrDefaultAsync(criteria);
        }

        private void SetRequestOwner()
        {
            Token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
            RequestOwner = JWTHelper.DecryptTokenUser(Token);

        }
    }
}