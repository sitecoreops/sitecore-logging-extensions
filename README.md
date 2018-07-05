# Sitecore Logging Extensions

JSON formatted log4net layout and a UTF8 FileAppender that does not emit BOM (byte order mark). Suitable for ingestion of log data into ElasticSearch using for example Filebeat.

## Usage

1. Install with: `PM> Install-Package SitecoreLoggingExtensions` 
1. Configure log4net, see [src/SitecoreLoggingExtensions.Website/App_Config/Include/SitecoreLoggingExtensions.config](example config).