-- Fix NULL ProcessingStatus values in all source tables
-- ProcessingStatus should default to 0 (Staged)

-- Fix Safaricom table
UPDATE Safaricom
SET ProcessingStatus = 0
WHERE ProcessingStatus IS NULL;

-- Fix Airtel table
UPDATE Airtel
SET ProcessingStatus = 0
WHERE ProcessingStatus IS NULL;

-- Fix PSTNs table
UPDATE PSTNs
SET ProcessingStatus = 0
WHERE ProcessingStatus IS NULL;

-- Fix PrivateWires table
UPDATE PrivateWires
SET ProcessingStatus = 0
WHERE ProcessingStatus IS NULL;

PRINT 'Fixed ProcessingStatus NULL values in all source tables';

-- Verify the fix
SELECT
    'Safaricom' as TableName, COUNT(*) as NullCount FROM Safaricom WHERE ProcessingStatus IS NULL
UNION ALL
SELECT
    'Airtel', COUNT(*) FROM Airtel WHERE ProcessingStatus IS NULL
UNION ALL
SELECT
    'PSTNs', COUNT(*) FROM PSTNs WHERE ProcessingStatus IS NULL
UNION ALL
SELECT
    'PrivateWires', COUNT(*) FROM PrivateWires WHERE ProcessingStatus IS NULL;