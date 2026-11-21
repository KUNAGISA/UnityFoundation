using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Gameplay.Common
{
    public struct CleanupExceptionCollector
    {
        private List<Exception> _exceptions;

        public void Add(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                var innerExceptions = aggregateException.Flatten().InnerExceptions;
                (_exceptions ??= new List<Exception>(innerExceptions.Count)).AddRange(innerExceptions);
                return;
            }

            (_exceptions ??= new List<Exception>()).Add(exception);
        }

        public Exception ToException()
        {
            if (_exceptions == null)
            {
                return null;
            }

            return _exceptions.Count == 1 ? _exceptions[0] : new AggregateException(_exceptions);
        }

        public void ThrowIfAny()
        {
            if (_exceptions == null)
            {
                return;
            }

            if (_exceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(_exceptions[0]).Throw();
            }

            throw new AggregateException(_exceptions);
        }
    }
}
