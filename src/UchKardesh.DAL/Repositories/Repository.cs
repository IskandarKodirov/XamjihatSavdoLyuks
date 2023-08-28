﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;
using UchKardesh.DAL.Constexts;
using UchKardesh.DAL.IRepositories;
using UchKardesh.Domain.Commons;

namespace UchKardesh.DAL.Repositories;

public class Repository<T> : IRepository<T> where T : Auditable
{
    private readonly AppDbContext dbContext;
    private readonly DbSet<T> table;

    public Repository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
        table = dbContext.Set<T>();
    }

    public async ValueTask<T> AddAsync(T entity)
    {
        await table.AddAsync(entity);
        await this.SaveAsync();
        return entity;
    }

    public async ValueTask<bool> DeleteAsync(Expression<Func<T, bool>> expression)
    {
        var entity = await this.SelectAsync(expression);

        if (entity is not null)
        {
            entity.IsDeleted = true;
            await this.SaveAsync();
            return true;
        }

        return false;
    }

    public async ValueTask<T> SelectAsync(Expression<Func<T, bool>> expression, string[] includes = null)
            => await this.SelectAll(expression, includes).FirstOrDefaultAsync(t => !t.IsDeleted);

    public IQueryable<T> SelectAll(Expression<Func<T, bool>> expression = null, string[] includes = null, bool isTracking = true)
    {
        var query = expression is null ? isTracking ? table : table.AsNoTracking()
            : isTracking ? table.Where(expression) : table.Where(expression).AsNoTracking();

        if (includes is not null)
            foreach (var include in includes)
                query = query.Include(include);

        return query.Where(i => !i.IsDeleted);
    }

    public async ValueTask<T> UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        EntityEntry<T> entry = this.dbContext.Update(entity);
        await this.SaveAsync();

        return entry.Entity;
    }

    public async ValueTask SaveAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
