using System;
using System.Reflection;
using System.Reflection.Emit;
using log4net.spi;

namespace SitecoreLoggingExtensions
{
    internal static class LoggingEventExtensions
    {
        private static Invoker _invoker;

        public static Exception GetException(this LoggingEvent loggingEvent)
        {
            if (string.IsNullOrEmpty(loggingEvent.GetExceptionStrRep()))
            {
                return null;
            }

            var invoker = GetInvoker();

            return invoker?.Invoke(loggingEvent);
        }

        private static Invoker GetInvoker()
        {
            if (_invoker != null)
            {
                return _invoker;
            }

            var loggingEventType = typeof(LoggingEvent);
            var field = loggingEventType.GetField("m_thrownException", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                return null;
            }

            var method = new DynamicMethod("", typeof(Exception), new[] {loggingEventType}, loggingEventType);
            var generator = method.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0); // load the instance itself
            generator.Emit(OpCodes.Ldfld, field); // load the private field to stack
            generator.Emit(OpCodes.Ret); // return the value

            _invoker = (Invoker)method.CreateDelegate(typeof(Invoker));

            return _invoker;
        }

        private delegate Exception Invoker(LoggingEvent target);
    }
}