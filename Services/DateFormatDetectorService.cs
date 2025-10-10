using System.Globalization;
using System.Text.RegularExpressions;

namespace TAB.Web.Services
{
    public class DateFormatDetectionResult
    {
        public string DetectedFormat { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public List<string> SampleParsedDates { get; set; } = new();
        public List<string> AmbiguousDates { get; set; } = new();
        public List<string> InvalidDates { get; set; } = new();
        public Dictionary<string, double> FormatScores { get; set; } = new();
    }

    public interface IDateFormatDetectorService
    {
        DateFormatDetectionResult DetectFormat(List<string> sampleValues);
        DateTime? ParseDate(string dateString, string format);
        List<string> GetSupportedFormats();
    }

    public class DateFormatDetectorService : IDateFormatDetectorService
    {
        private readonly List<string> _supportedFormats = new()
        {
            // Full year formats
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "yyyy-MM-dd",
            "dd-MM-yyyy",
            "MM-dd-yyyy",
            "yyyy/MM/dd",
            "dd.MM.yyyy",
            "MM.dd.yyyy",

            // Short year formats (2-digit)
            "dd/MM/yy",
            "MM/dd/yy",
            "yy-MM-dd",
            "dd-MM-yy",
            "MM-dd-yy",
            "yy/MM/dd",

            // Single digit day/month supported
            "d/M/yyyy",
            "M/d/yyyy",
            "d-M-yyyy",
            "M-d-yyyy",
            "d/M/yy",
            "M/d/yy",

            // ISO format
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss"
        };

        public List<string> GetSupportedFormats() => _supportedFormats;

        public DateFormatDetectionResult DetectFormat(List<string> sampleValues)
        {
            var result = new DateFormatDetectionResult
            {
                FormatScores = new Dictionary<string, double>()
            };

            // Filter out empty/null values
            var validSamples = sampleValues
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(100)
                .ToList();

            if (!validSamples.Any())
            {
                result.DetectedFormat = "dd/MM/yyyy";
                result.ConfidenceScore = 0;
                return result;
            }

            // Try each format and score it
            foreach (var format in _supportedFormats)
            {
                var score = ScoreFormat(validSamples, format);
                result.FormatScores[format] = score;
            }

            // Get the best format
            var bestFormat = result.FormatScores.OrderByDescending(x => x.Value).First();
            result.DetectedFormat = bestFormat.Key;
            result.ConfidenceScore = bestFormat.Value;

            // Parse sample dates with detected format
            foreach (var sample in validSamples.Take(5))
            {
                var parsed = ParseDate(sample, result.DetectedFormat);
                if (parsed.HasValue)
                {
                    result.SampleParsedDates.Add($"{sample} → {parsed.Value:dd MMM yyyy}");
                }
                else
                {
                    result.InvalidDates.Add(sample);
                }
            }

            // Detect ambiguous dates
            result.AmbiguousDates = DetectAmbiguousDates(validSamples);

            return result;
        }

        private double ScoreFormat(List<string> samples, string format)
        {
            int successCount = 0;
            int totalCount = samples.Count;

            foreach (var sample in samples)
            {
                if (ParseDate(sample, format).HasValue)
                {
                    successCount++;
                }
            }

            return (double)successCount / totalCount * 100;
        }

        public DateTime? ParseDate(string dateString, string format)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            // Clean the input
            dateString = dateString.Trim();

            // Try exact parsing first
            if (DateTime.TryParseExact(dateString, format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime result))
            {
                return result;
            }

            // Try with AllowWhiteSpaces
            if (DateTime.TryParseExact(dateString, format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out result))
            {
                return result;
            }

            return null;
        }

        private List<string> DetectAmbiguousDates(List<string> samples)
        {
            var ambiguous = new List<string>();

            // Dates like 01/02/2024 could be Jan 2 or Feb 1
            var ambiguousPattern = new Regex(@"^(0?[1-9]|1[0-2])[\/\-\.](0?[1-9]|1[0-2])[\/\-\.](\d{2,4})$");

            foreach (var sample in samples)
            {
                if (ambiguousPattern.IsMatch(sample.Trim()))
                {
                    var parts = sample.Split(new[] { '/', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3)
                    {
                        if (int.TryParse(parts[0], out int first) &&
                            int.TryParse(parts[1], out int second))
                        {
                            // Both parts are ≤ 12, could be either day or month
                            if (first <= 12 && second <= 12 && first != second)
                            {
                                ambiguous.Add(sample);
                            }
                        }
                    }
                }
            }

            return ambiguous.Distinct().Take(5).ToList();
        }
    }
}
