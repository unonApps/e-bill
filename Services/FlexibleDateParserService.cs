using System.Globalization;

namespace TAB.Web.Services
{
    public class DateParseResult
    {
        public bool Success { get; set; }
        public DateTime? ParsedDate { get; set; }
        public string? UsedFormat { get; set; }
        public string? ErrorMessage { get; set; }
        public string OriginalValue { get; set; } = string.Empty;
    }

    public interface IFlexibleDateParserService
    {
        DateParseResult ParseDate(string dateString, string? preferredFormat = null);
        DateParseResult ParseDateWithFallback(string dateString, List<string> formatPriority);
        bool ValidateDate(DateTime date, DateTime? minDate = null, DateTime? maxDate = null);
    }

    public class FlexibleDateParserService : IFlexibleDateParserService
    {
        private readonly IDateFormatDetectorService _formatDetector;

        public FlexibleDateParserService(IDateFormatDetectorService formatDetector)
        {
            _formatDetector = formatDetector;
        }

        public DateParseResult ParseDate(string dateString, string? preferredFormat = null)
        {
            var result = new DateParseResult
            {
                OriginalValue = dateString
            };

            if (string.IsNullOrWhiteSpace(dateString))
            {
                result.ErrorMessage = "Date string is empty";
                return result;
            }

            dateString = dateString.Trim();

            // If preferred format is provided, try it first
            if (!string.IsNullOrEmpty(preferredFormat))
            {
                var parsed = _formatDetector.ParseDate(dateString, preferredFormat);
                if (parsed.HasValue)
                {
                    result.Success = true;
                    result.ParsedDate = parsed.Value;
                    result.UsedFormat = preferredFormat;
                    return result;
                }
            }

            // Fallback: Try all supported formats
            var formats = _formatDetector.GetSupportedFormats();
            return ParseDateWithFallback(dateString, formats);
        }

        public DateParseResult ParseDateWithFallback(string dateString, List<string> formatPriority)
        {
            var result = new DateParseResult
            {
                OriginalValue = dateString
            };

            if (string.IsNullOrWhiteSpace(dateString))
            {
                result.ErrorMessage = "Date string is empty";
                return result;
            }

            dateString = dateString.Trim();

            // Try each format in priority order
            foreach (var format in formatPriority)
            {
                var parsed = _formatDetector.ParseDate(dateString, format);
                if (parsed.HasValue)
                {
                    // Validate the parsed date
                    if (ValidateDate(parsed.Value))
                    {
                        result.Success = true;
                        result.ParsedDate = parsed.Value;
                        result.UsedFormat = format;
                        return result;
                    }
                }
            }

            // If all formats fail, try generic parsing as last resort
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime genericParsed))
            {
                if (ValidateDate(genericParsed))
                {
                    result.Success = true;
                    result.ParsedDate = genericParsed;
                    result.UsedFormat = "Generic parsing";
                    return result;
                }
            }

            result.ErrorMessage = $"Unable to parse '{dateString}' with any known format";
            return result;
        }

        public bool ValidateDate(DateTime date, DateTime? minDate = null, DateTime? maxDate = null)
        {
            // Check basic validity
            if (date == DateTime.MinValue || date == DateTime.MaxValue)
                return false;

            // Date should not be too far in the past (before 1900)
            if (date.Year < 1900)
                return false;

            // Date should not be too far in the future (more than 10 years from now)
            if (date > DateTime.Now.AddYears(10))
                return false;

            // Check custom min/max dates
            if (minDate.HasValue && date < minDate.Value)
                return false;

            if (maxDate.HasValue && date > maxDate.Value)
                return false;

            return true;
        }
    }
}
