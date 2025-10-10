/**
 * CSV Validator and Parser
 * Handles CSV file parsing and validation with date format detection
 */

class CSVValidator {
    constructor() {
        this.detector = new DateFormatDetector();
    }

    /**
     * Parse CSV file and detect date columns
     * @param {File} file - CSV file to parse
     * @returns {Promise<Object>} Parse result with headers, data, and detected formats
     */
    async parseFile(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();

            reader.onload = (e) => {
                try {
                    const text = e.target.result;
                    const result = this.parseCSV(text);
                    resolve(result);
                } catch (error) {
                    reject(error);
                }
            };

            reader.onerror = () => reject(new Error('Failed to read file'));
            reader.readAsText(file);
        });
    }

    /**
     * Parse CSV text content
     * @private
     */
    parseCSV(text) {
        const lines = text.split('\n').filter(line => line.trim() !== '');

        if (lines.length === 0) {
            throw new Error('CSV file is empty');
        }

        // Parse header
        const headers = this.parseLine(lines[0]);

        // Parse data rows (sample first 100 for detection)
        const sampleSize = Math.min(100, lines.length - 1);
        const dataRows = [];

        for (let i = 1; i <= sampleSize; i++) {
            if (i < lines.length) {
                const values = this.parseLine(lines[i]);
                if (values.length === headers.length) {
                    dataRows.push(values);
                }
            }
        }

        return {
            headers,
            dataRows,
            totalRows: lines.length - 1
        };
    }

    /**
     * Parse a single CSV line (handles quoted values)
     * @private
     */
    parseLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;

        for (let i = 0; i < line.length; i++) {
            const char = line[i];

            if (char === '"') {
                inQuotes = !inQuotes;
            } else if (char === ',' && !inQuotes) {
                result.push(current.trim());
                current = '';
            } else {
                current += char;
            }
        }

        result.push(current.trim());
        return result;
    }

    /**
     * Detect date columns and their formats
     * @param {Array<string>} headers - Column headers
     * @param {Array<Array<string>>} dataRows - Data rows
     * @returns {Object} Map of column name to detection result
     */
    detectDateColumns(headers, dataRows) {
        const dateColumns = {};
        const dateKeywords = ['date', 'time', 'call_date', 'invoice', 'billing', 'period'];

        headers.forEach((header, index) => {
            const headerLower = header.toLowerCase();

            // Check if header suggests it's a date column
            const isLikelyDate = dateKeywords.some(keyword => headerLower.includes(keyword));

            if (isLikelyDate) {
                // Extract sample values for this column
                const sampleValues = dataRows.map(row => row[index]).filter(v => v);

                if (sampleValues.length > 0) {
                    const detection = this.detector.detectFormat(sampleValues);

                    if (detection.success || sampleValues.length > 5) {
                        dateColumns[header] = {
                            columnIndex: index,
                            ...detection
                        };
                    }
                }
            }
        });

        return dateColumns;
    }

    /**
     * Validate all rows with detected format
     * @param {Array<Array<string>>} allRows - All data rows
     * @param {string} columnName - Column to validate
     * @param {number} columnIndex - Column index
     * @param {string} format - Date format to use
     * @returns {Object} Validation results
     */
    validateColumn(allRows, columnName, columnIndex, format) {
        const formatDetails = this.detector.supportedFormats.find(f => f.pattern === format);

        if (!formatDetails) {
            return {
                success: false,
                message: 'Invalid format specified'
            };
        }

        const errors = [];
        let validCount = 0;

        allRows.forEach((row, index) => {
            const value = row[columnIndex];

            if (!value || value.trim() === '') {
                errors.push({
                    row: index + 2, // +2 for header and 0-based index
                    column: columnName,
                    value: value || '(empty)',
                    error: 'Missing required date value'
                });
                return;
            }

            const result = this.detector.tryParseDate(value, formatDetails);

            if (!result.valid) {
                errors.push({
                    row: index + 2,
                    column: columnName,
                    value: value,
                    error: result.reason
                });
            } else {
                validCount++;
            }
        });

        return {
            success: errors.length === 0,
            validCount,
            totalCount: allRows.length,
            errorCount: errors.length,
            errors: errors.slice(0, 100) // Limit to first 100 errors
        };
    }

    /**
     * Generate error report CSV
     * @param {Array<Object>} errors - Validation errors
     * @returns {string} CSV content
     */
    generateErrorReport(errors) {
        let csv = 'Row,Column,Value,Error\n';

        errors.forEach(error => {
            csv += `${error.row},"${error.column}","${error.value}","${error.error}"\n`;
        });

        return csv;
    }

    /**
     * Download error report
     * @param {Array<Object>} errors - Validation errors
     * @param {string} filename - Output filename
     */
    downloadErrorReport(errors, filename = 'validation_errors.csv') {
        const csv = this.generateErrorReport(errors);
        const blob = new Blob([csv], { type: 'text/csv' });
        const url = URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CSVValidator;
}
