using PracticeProject.Iservices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Services
{
    /// <summary>
    /// 指数退避重试策略
    /// </summary>
    public class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public int InitialDelayMs { get; set; } = 100;
        public int MaxDelayMs { get; set; } = 5000;
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            Exception? lastException = null;
            var delay = InitialDelayMs;

            for (int i = 0; i <= MaxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (i < MaxRetries)
                {
                    lastException = ex;

                    if (i < MaxRetries - 1)
                    {
                        await Task.Delay(delay);
                        delay = Math.Min(delay * 2, MaxDelayMs);
                    }
                }
            }

            throw lastException ?? new Exception("重试失败");
        }
        public async Task ExecuteAsync(Func<Task> operation)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            });
        }
    }
}
