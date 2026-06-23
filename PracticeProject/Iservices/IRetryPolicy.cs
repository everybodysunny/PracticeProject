using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeProject.Iservices
{
    public interface IRetryPolicy
    {
        /// <summary>
        /// 执行带重试的操作（有返回值）
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);

        /// <summary>
        /// 执行带重试的操作（无返回值）
        /// </summary>
        Task ExecuteAsync(Func<Task> operation);
    }
}
