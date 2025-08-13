using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Core.Utility {
    public static class Retry {
        public static Task Do(Action action, TimeSpan retryInterval, int maxAttemptCount = 3) {
            return Do<object>(() => {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static async Task<T> Do<T>(Func<T> action, TimeSpan retryInterval, int maxAttemptCount = 3) {
            return await Do(async () => {
                return await Task.FromResult(action());
            }, retryInterval, maxAttemptCount);
        }

        public static Task Do(Func<Task> action, TimeSpan retryInterval, int maxAttemptCount = 3) {
            return Do(async () => {
                await action();
                return Task.CompletedTask;
            }, retryInterval, maxAttemptCount);
        }

        public static async Task<T> Do<T>(Func<Task<T>> action, TimeSpan retryInterval, int maxAttemptCount = 3) {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++) {
                try {
                    if (attempted > 0) {
                        await Task.Delay(retryInterval);
                    }
                    return await action();
                } catch (Exception ex) {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
