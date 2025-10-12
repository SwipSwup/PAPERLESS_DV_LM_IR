using Core.Exceptions;

namespace DAL.Repositories
{
    public class RepositoryBase
    {
        protected async Task<T> ExecuteRepositoryActionAsync<T>(Func<Task<T>> func, string errorMessage)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                throw new DataAccessException(errorMessage, ex);
            }
        }

        protected async Task ExecuteRepositoryActionAsync(Func<Task> func, string errorMessage)
        {
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                throw new DataAccessException(errorMessage, ex);
            }
        }
    }
}