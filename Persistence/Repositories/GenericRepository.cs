using System.Linq.Expressions;
using System.Security.Claims;
using Application.Interfaces.Persistence;
using Domain;
using Domain.Common;
using LinqKit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GenericRepository(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public IQueryable<T> GetQueryable()
    {
        return _dbContext.Set<T>().Where(x => !x.IsDeleted);
    }

    public Task<T?> Get(long id)
    {
        return Task.FromResult(_dbContext.Set<T>().FirstOrDefault(x => !x.IsDeleted && x.Id == id));
    }
    public Task<List<T>> GetAllByIds(List<long> ids)
    {
        return Task.FromResult(_dbContext.Set<T>()
            .Where(x => !x.IsDeleted && ids.Contains(x.Id))
            .ToList());
    }
    public Task<IReadOnlyList<T>> GetAll()
    {
        if (typeof(T).IsSubclassOf(typeof(OrderableBaseEntity)))
            return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>().Where(x => !x.IsDeleted)
                .OrderBy(x => (x as OrderableBaseEntity)!.DisplayOrder).ToList());

        return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>().Where(x => !x.IsDeleted).ToList());
    }

    public Task<IReadOnlyList<T>> Where(Expression<Func<T, bool>> predicate)
    {
        if (typeof(T).IsSubclassOf(typeof(OrderableBaseEntity)))
            return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>().Where(predicate.And(x => !x.IsDeleted))
                .OrderBy(x => (x as OrderableBaseEntity)!.DisplayOrder).ToList());
        return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>().Where(predicate.And(x => !x.IsDeleted)).ToList());
    }

    public Task<long> Count()
    {
        return Task.FromResult<long>(_dbContext.Set<T>().Count(x => !x.IsDeleted));
    }

    public Task<T> First(Expression<Func<T, bool>>? predicate = null)
    {
        predicate = predicate?.And(x => !x.IsDeleted);
        return _dbContext.Set<T>().FirstAsync(predicate ?? (T => true));
    }

    public Task<T?> FirstOrDefault(Expression<Func<T, bool>>? predicate = null)
    {
        predicate = predicate?.And(x => !x.IsDeleted);
        return _dbContext.Set<T>().FirstOrDefaultAsync(predicate ?? (T => true));
    }


    public Task<T?> GetActive(long id)
    {
        return Task.FromResult(_dbContext.Set<T>().FirstOrDefault(x => !x.IsDeleted && x.Id == id && x.IsActive));
    }

    public Task<IReadOnlyList<T>> GetAllActive()
    {
        if (typeof(T).IsSubclassOf(typeof(OrderableBaseEntity)))
            return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>().Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => (x as OrderableBaseEntity)!.DisplayOrder).ToList());

        return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>().Where(x => !x.IsDeleted && x.IsActive).ToList());
    }

    public Task<IReadOnlyList<T>> WhereActive(Expression<Func<T, bool>> predicate)
    {
        if (typeof(T).IsSubclassOf(typeof(OrderableBaseEntity)))
            return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>()
                .Where(predicate.And(x => !x.IsDeleted && x.IsActive))
                .OrderBy(x => (x as OrderableBaseEntity)!.DisplayOrder).ToList());
        return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>()
            .Where(predicate.And(x => !x.IsDeleted && x.IsActive)).ToList());
    }

    public Task<T> FirstActive(Expression<Func<T, bool>>? predicate = null)
    {
        predicate = predicate?.And(x => !x.IsDeleted && x.IsActive);
        return _dbContext.Set<T>().FirstAsync(predicate ?? (T => true));
    }

    public Task<T?> FirstOrDefaultActive(Expression<Func<T, bool>>? predicate = null)
    {
        predicate = predicate?.And(x => !x.IsDeleted && x.IsActive);
        return _dbContext.Set<T>().FirstOrDefaultAsync(predicate ?? (T => true));
    }


    public async Task<T> Add(T entity)
    {
        await _dbContext.AddAsync(entity);
        return entity;
    }

    public async Task AddRange(IEnumerable<T> entities)
    {
        await _dbContext.AddRangeAsync(entities);
    }

    public Task Update(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task UpdateRange(IEnumerable<T> entities)
    {
        _dbContext.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task Remove(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public Task RemoveRange(IEnumerable<T> entities)
    {
        _dbContext.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public Task SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        return Task.CompletedTask;
    }
    public async Task<int> SaveChanges()
    {
        var user = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        return user is null ? await _dbContext.SaveChangesAsync() : await _dbContext.SaveChangesAsync(user.Value);
    }
    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbContext.Set<T>().CountAsync(predicate);
    }
    public Task<T?> GetWithDeleted(long id)
    {
        return Task.FromResult(_dbContext.Set<T>().FirstOrDefault(x => x.Id == id));
    }
    public Task<IReadOnlyList<T>> GetAllWithDeleted()
    {
        if (typeof(T).IsSubclassOf(typeof(OrderableBaseEntity)))
            return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>()
                .OrderBy(x => (x as OrderableBaseEntity)!.DisplayOrder).ToList());

        return Task.FromResult<IReadOnlyList<T>>(_dbContext.Set<T>().ToList());
    }
    public Task<T?> FirstOrDefaultWithDeleted(Expression<Func<T, bool>> predicate)
    {
        return _dbContext.Set<T>().FirstOrDefaultAsync(predicate);
    }
    public async Task<List<Nationality>> GetCountriesAsync()
    {
        return await _dbContext.Nationality
            .Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.Title) 
            .ToListAsync();
    }
    public async Task<List<State>> GetStatesByCountryIdAsync(long countryId)
    {
        return await _dbContext.States
            .Where(s => s.CountryId == countryId && s.IsActive && !s.IsDeleted)
            .ToListAsync();
    }
    public async Task<List<District>> GetDistrictsByStateIdAsync(long stateId)
    {
        return await _dbContext.Districts
            .Where(d => d.StateId == stateId && d.IsActive && !d.IsDeleted).OrderBy(d => d.Name)
            .ToListAsync();
    }
    public async Task<List<City>> GetCityByDistrictIdAsync(long districtId)
    {
        return await _dbContext.Cities
            .Where(d => d.DistrictId == districtId && d.IsActive && !d.IsDeleted).OrderBy(d =>d.Name)
            .ToListAsync();
    }
    public async Task<List<District>> GetDistrictByStateNameAsync(string stateName)
    {
        var state = _dbContext.States.FirstOrDefault(s => s.Name == stateName);
        if (state != null)
        {
            return await _dbContext.Districts
                .Where(d => d.StateId == state.Id && d.IsActive && !d.IsDeleted).OrderBy(d => d.Name)
                .ToListAsync();
        }
        else
        {
            return new List<District>();
        }
    } 
    
    public async Task<List<City>> GetCityByDistictNameAsync(string districtName)
    {
        var district = _dbContext.Districts.FirstOrDefault(s => s.Name == districtName);
        if (district != null)
        {
            return await _dbContext.Cities
                .Where(d => d.DistrictId == district.Id && d.IsActive && !d.IsDeleted).OrderBy(d => d.Name)
                .ToListAsync();
        }
        else
        {
            return new List<City>();
        }
    }
}