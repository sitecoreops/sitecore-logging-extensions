# Sitecore Logging Extensions

JSON formatted log4net layout and a UTF8 file appender that does not emit BOM (byte order mark). Suitable for ingesting log data into ElasticSearch using for example Filebeat.

## Usage

1. Install with: `PM> Install-Package SitecoreLoggingExtensions` 
1. Configure log4net, see [example config](src/SitecoreLoggingExtensions.Website/App_Config/Include/SitecoreLoggingExtensions.config).