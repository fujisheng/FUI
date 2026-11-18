using System;
using System.Collections.Generic;

namespace FUI.Testing
{
    /// <summary>Completes every owned cleanup before returning all observed errors.</summary>
    internal static class HeadlessCleanup
    {
        internal static Exception Collect(IEnumerable<Action> actions)
        {
            var failures = new List<Exception>();
            foreach (var action in actions)
            {
                try { action(); }
                catch (Exception error) { failures.Add(error); }
            }
            if (failures.Count == 0) { return null; }
            if (failures.Count == 1) { return failures[0]; }
            return new AggregateException("Multiple cleanup failures.", failures);
        }
    }
}
