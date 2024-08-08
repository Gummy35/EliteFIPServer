using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteFIPServer
{   
    public class NlogMemoryTarget : Target
    {
        public event EventHandler<string> OnLog;

        public NlogMemoryTarget(string name, LogLevel level) : this(name, level, level) { }
        public NlogMemoryTarget(string name, LogLevel minLevel, LogLevel maxLevel)
        {            
            // Add Target and Rule to their respective collections
            LogManager.Configuration.AddTarget(name, this);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", minLevel, maxLevel, this));

            LogManager.ReconfigExistingLoggers();
        }

        protected override void Write(AsyncLogEventInfo logEvent)
        {
            Write(logEvent.LogEvent);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            OnLog(this, logEvent.FormattedMessage);
        }

        // consider overriding WriteAsyncThreadSafe methods as well.
    }
}
