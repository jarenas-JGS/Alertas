using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Alertas.Helpers
{
    public static class CrudRelationHelper
    {
        public static async Task<bool> TieneRelaciones<TEntity>(
            DbSet<TEntity> dbSet,
            Expression<Func<TEntity, bool>> condicion)
            where TEntity : class
        {
            return await dbSet.AnyAsync(condicion);
        }
    }
}