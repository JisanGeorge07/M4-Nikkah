using Domain;
using System.Linq.Expressions;

namespace Application.Interfaces.Persistence;

public interface IRepository<T> where T : class
{
    IQueryable<T> GetQueryable();

    Task<T?> Get(long id);

    Task<List<T>> GetAllByIds(List<long> ids);
    Task<IReadOnlyList<T>> GetAll();
    Task<IReadOnlyList<T>> Where(Expression<Func<T, bool>> predicate);
    Task<T> First(Expression<Func<T, bool>>? predicate = null);
    Task<T?> FirstOrDefault(Expression<Func<T, bool>>? predicate = null);

    Task<T?> GetActive(long id);
    Task<IReadOnlyList<T>> GetAllActive();
    Task<IReadOnlyList<T>> WhereActive(Expression<Func<T, bool>> predicate);
    Task<T> FirstActive(Expression<Func<T, bool>>? predicate = null);
    Task<T?> FirstOrDefaultActive(Expression<Func<T, bool>>? predicate = null);

    Task<long> Count();
    Task<T> Add(T entity);
    Task AddRange(IEnumerable<T> entities);
    Task Update(T entity);
    Task UpdateRange(IEnumerable<T> entities);

    Task Remove(T entity);
    Task RemoveRange(IEnumerable<T> entities);

    Task SoftDelete(T entity);
    Task<int> SaveChanges();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task<T?> GetWithDeleted(long id);
    Task<IReadOnlyList<T>> GetAllWithDeleted();
    Task<T?> FirstOrDefaultWithDeleted(Expression<Func<T, bool>> predicate);
    Task<List<Nationality>> GetCountriesAsync();
    Task<List<State>> GetStatesByCountryIdAsync(long countryId);
    Task<List<District>> GetDistrictsByStateIdAsync(long stateId);
    Task<List<City>> GetCityByDistrictIdAsync(long districtId);
    Task<List<District>> GetDistrictByStateNameAsync(string stateName);
    Task<List<City>> GetCityByDistictNameAsync(string districtName);
}