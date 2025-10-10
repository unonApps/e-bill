/**
 * Enterprise Date Format Detector
 * Automatically detects date formats from CSV data with confidence scoring
 */

class DateFormatDetector {
    constructor() {
        this.supportedFormats = [
            { pattern: 'dd/MM/yyyy', example: '25/08/2025', regex: /^\d{1,2}\/\d{1,2}\/\d{4}$/, order: 'DMY', separator: '/' },
            { pattern: 'MM/dd/yyyy', example: '08/25/2025', regex: /^\d{1,2}\/\d{1,2}\/\d{4}$/, order: 'MDY', separator: '/' },
            { pattern: 'd/M/yyyy', example: '8/8/2025', regex: /^\d{1,2}\/\d{1,2}\/\d{4}$/, order: 'DMY', separator: '/' },
            { pattern: 'M/d/yyyy', example: '8/8/2025', regex: /^\d{1,2}\/\d{1,2}\/\d{4}$/, order: 'MDY', separator: '/' },
            { pattern: 'dd/MM/yy', example: '25/08/25', regex: /^\d{1,2}\/\d{1,2}\/\d{2}$/, order: 'DMY', separator: '/' },
            { pattern: 'MM/dd/yy', example: '08/25/25', regex: /^\d{1,2}\/\d{1,2}\/\d{2}$/, order: 'MDY', separator: '/' },
            { pattern: 'd/M/yy', example: '8/8/25', regex: /^\d{1,2}\/\d{1,2}\/\d{2}$/, order: 'DMY', separator: '/' },
            { pattern: 'yyyy-MM-dd', example: '2025-08-25', regex: /^\d{4}-\d{1,2}-\d{1,2}$/, order: 'YMD', separator: '-' },
            { pattern: 'dd-MM-yyyy', example: '25-08-2025', regex: /^\d{1,2}-\d{1,2}-\d{4}$/, order: 'DMY', separator: '-' }
        ];
    }

    /**
     * Detect date format from sample values
     * @param {Array<string>} sampleValues - Array of date strings to analyze
     * @param {number} maxSamples - Maximum number of samples to analyze (default: 100)
     * @returns {Object} Detection result with format, confidence, and details
     */
    detectFormat(sampleValues, maxSamples = 100) {
        // Filter out empty values
        const validSamples = sampleValues
            .filter(v => v && v.trim() !== '')
            .slice(0, maxSamples);

        if (validSamples.length === 0) {
            return {
                success: false,
                format: null,
                confidence: 0,
                message: 'No valid date values found'
            };
        }

        // Try each format and score them
        const formatScores = this.supportedFormats.map(format => {
            const score = this.scoreFormat(validSamples, format);
            return { format, ...score };
        });

        // Sort by confidence score
        formatScores.sort((a, b) => b.confidence - a.confidence);

        const best = formatScores[0];

        return {
            success: best.confidence > 0,
            format: best.format.pattern,
            formatDetails: best.format,
            confidence: best.confidence,
            validCount: best.validCount,
            totalCount: validSamples.length,
            invalidSamples: best.invalidSamples,
            ambiguousSamples: best.ambiguousSamples,
            parsedSamples: best.parsedSamples,
            alternatives: formatScores.slice(1, 3).map(f => ({
                format: f.format.pattern,
                confidence: f.confidence
            }))
        };
    }

    /**
     * Score a format against sample values
     * @private
     */
    scoreFormat(samples, format) {
        let validCount = 0;
        let invalidSamples = [];
        let ambiguousSamples = [];
        let parsedSamples = [];

        samples.forEach((sample, index) => {
            const result = this.tryParseDate(sample, format);

            if (result.valid) {
                validCount++;
                if (index < 5) { // Store first 5 for preview
                    parsedSamples.push({
                        original: sample,
                        parsed: result.date,
                        formatted: this.formatDate(result.date)
                    });
                }
            } else {
                if (invalidSamples.length < 5) {
                    invalidSamples.push({
                        value: sample,
                        reason: result.reason
                    });
                }
            }

            if (result.ambiguous && ambiguousSamples.length < 5) {
                ambiguousSamples.push(sample);
            }
        });

        const confidence = samples.length > 0 ? (validCount / samples.length) * 100 : 0;

        return {
            validCount,
            confidence: Math.round(confidence * 10) / 10,
            invalidSamples,
            ambiguousSamples,
            parsedSamples
        };
    }

    /**
     * Try to parse a date string with a specific format
     * @private
     */
    tryParseDate(dateString, format) {
        if (!format.regex.test(dateString)) {
            return { valid: false, reason: 'Pattern mismatch' };
        }

        const parts = dateString.split(format.separator);
        if (parts.length !== 3) {
            return { valid: false, reason: 'Invalid number of components' };
        }

        let day, month, year;

        // Parse based on format order
        if (format.order === 'DMY') {
            [day, month, year] = parts.map(p => parseInt(p, 10));
        } else if (format.order === 'MDY') {
            [month, day, year] = parts.map(p => parseInt(p, 10));
        } else if (format.order === 'YMD') {
            [year, month, day] = parts.map(p => parseInt(p, 10));
        }

        // Handle 2-digit years
        if (year < 100) {
            year += year < 50 ? 2000 : 1900;
        }

        // Validate ranges
        if (month < 1 || month > 12) {
            return { valid: false, reason: `Invalid month: ${month}` };
        }

        if (day < 1 || day > 31) {
            return { valid: false, reason: `Invalid day: ${day}` };
        }

        // Check for valid date
        const date = new Date(year, month - 1, day);
        if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) {
            return { valid: false, reason: 'Invalid date (e.g., Feb 30)' };
        }

        // Check reasonable year range
        if (year < 1900 || year > 2100) {
            return { valid: false, reason: `Year out of range: ${year}` };
        }

        // Check for ambiguous dates (could be either DMY or MDY)
        const ambiguous = day <= 12 && month <= 12 && day !== month;

        return { valid: true, date, ambiguous };
    }

    /**
     * Format a date for display
     * @private
     */
    formatDate(date) {
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
                       'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        return `${months[date.getMonth()]} ${date.getDate()}, ${date.getFullYear()}`;
    }

    /**
     * Get format example by pattern
     */
    getFormatExample(pattern) {
        const format = this.supportedFormats.find(f => f.pattern === pattern);
        return format ? format.example : pattern;
    }
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DateFormatDetector;
}
