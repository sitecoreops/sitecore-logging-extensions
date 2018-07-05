using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using log4net.Layout;
using log4net.spi;
using Newtonsoft.Json;
using Sitecore.Configuration;

namespace SitecoreLoggingExtensions
{
    public class JsonLayout : LayoutSkeleton
    {
        private static EnvironmentInfoModel _environmentInfoModel;
        private readonly bool _logHosting;
        private readonly bool _logIdentity;
        private readonly bool _logUserName;
        private readonly string _role;
        private readonly JsonSerializer _serializer;
        private readonly string _stack;

        public JsonLayout() : this(false)
        {
        }

        public JsonLayout(bool locationInfo)
        {
            LocationInfo = locationInfo;

            _serializer = new JsonSerializer();
            _role = ConfigurationManager.AppSettings["role:define"].ToLowerInvariant();
            _stack = Settings.GetSetting("SitecoreLoggingExtensions.Data.StackName", string.Empty).ToLowerInvariant();
            _logHosting = Settings.GetBoolSetting("SitecoreLoggingExtensions.Options.LogHosting", false);
            _logUserName = Settings.GetBoolSetting("SitecoreLoggingExtensions.Options.LogUserName", false);
            _logIdentity = Settings.GetBoolSetting("SitecoreLoggingExtensions.Options.LogIdentity", false);
        }

        public bool LocationInfo { get; set; }

        public override string ContentType => "text/json";

        public override bool IgnoresException => false;

        private static EnvironmentInfoModel EnvironmentInfo
        {
            get
            {
                if (_environmentInfoModel != null)
                {
                    return _environmentInfoModel;
                }

                var applicationId = HostingEnvironment.ApplicationID;
                var siteIdMatch = Regex.Match(applicationId, "/w3svc/(?<SiteId>\\d+)", RegexOptions.IgnoreCase);
                var siteId = siteIdMatch.Success ? "W3SVC" + siteIdMatch.Groups["SiteId"] : applicationId;

                _environmentInfoModel = new EnvironmentInfoModel
                {
                    EnvironmentMachineName = Environment.MachineName.ToLowerInvariant(),
                    HostingSiteId = siteId.ToUpperInvariant(),
                    HostingSiteName = HostingEnvironment.SiteName.ToLowerInvariant()
                };

                return _environmentInfoModel;
            }
        }

        public override void ActivateOptions()
        {
        }

        public override string Format(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                throw new ArgumentNullException(nameof(loggingEvent));
            }

            var data = new Dictionary<string, object>();
            var properties = loggingEvent.Properties;

            if (properties != null)
            {
                foreach (var key in properties.GetKeys().Where(key => key != null && key != "log4net:HostName"))
                {
                    var value = properties[key];

                    if (value != null)
                    {
                        data[key] = value.ToString();
                    }
                }
            }

            data["sc.timestamp"] = loggingEvent.TimeStamp.ToString("O");
            data["sc.level"] = loggingEvent.Level?.Name;
            data["sc.thread"] = loggingEvent.ThreadName;
            data["sc.logger"] = loggingEvent.LoggerName;
            data["sc.message"] = loggingEvent.RenderedMessage;

            var info = EnvironmentInfo;

            data["sc.instance.role"] = _role;
            data["sc.environment.machinename"] = info.EnvironmentMachineName;

            if (!string.IsNullOrEmpty(_stack))
            {
                data["sc.instance.stackname"] = _stack;
            }

            if (_logHosting)
            {
                data["sc.hosting.siteid"] = info.HostingSiteId;
                data["sc.hosting.sitename"] = info.HostingSiteName;
            }

            if (_logIdentity)
            {
                data["sc.identity"] = loggingEvent.Identity;
            }

            if (_logUserName)
            {
                data["sc.username"] = loggingEvent.UserName;
            }

            if (loggingEvent.Level != null && loggingEvent.Level >= Level.WARN)
            {
                var httpContext = HttpContext.Current;

                if (httpContext != null)
                {
                    data["sc.httpcontext.request.url"] = httpContext.Request.RawUrl;
                    data["sc.httpcontext.request.method"] = httpContext.Request.HttpMethod;
                }
            }

            var exceptionString = loggingEvent.GetExceptionStrRep();

            if (!string.IsNullOrEmpty(exceptionString))
            {
                data["sc.exception.raw"] = exceptionString;

                var exception = loggingEvent.GetException();

                if (exception != null)
                {
                    var model = GetExceptionModel(exception);

                    data["sc.exception.type"] = model.Type;
                    data["sc.exception.message"] = model.Message;
                }
            }

            var builder = new StringBuilder();

            using (var writer = new StringWriter(builder, CultureInfo.InvariantCulture))
            {
                _serializer.Serialize(new JsonTextWriter(writer), data);
            }

            return builder + Environment.NewLine;
        }

        private ExceptionModel GetExceptionModel(Exception exception)
        {
            var exceptions = FlattenExceptions(exception).ToArray();

            // Pick the last exception as the root since it's the most specific one - just like the YSOD and ELMAH does
            var root = exceptions.Last();

            var model = new ExceptionModel
            {
                Type = root.Type,
                Message = root.Message
            };

            return model;
        }

        private IEnumerable<ExceptionModel> FlattenExceptions(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            var exceptions = new List<ExceptionModel> {MapException(exception)};
            var inner = exception.InnerException;

            while (inner != null)
            {
                exceptions.Add(MapException(inner));

                inner = inner.InnerException;
            }

            return exceptions;
        }

        private ExceptionModel MapException(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            return new ExceptionModel
            {
                Type = exception.GetType().FullName,
                Message = exception.Message
            };
        }

        private class EnvironmentInfoModel
        {
            public string EnvironmentMachineName { get; set; }
            public string HostingSiteId { get; set; }
            public string HostingSiteName { get; set; }
        }

        private class ExceptionModel
        {
            public string Type { get; set; }
            public string Message { get; set; }
        }
    }
}