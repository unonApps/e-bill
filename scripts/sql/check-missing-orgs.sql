-- Check count of distinct missing organizations
SELECT COUNT(*) AS DistinctMissingOrgs
FROM (
    SELECT DISTINCT e.[Org]
    FROM [TABDB].[dbo].[EbillUsers_Staging] e
    LEFT JOIN [TABDB].[dbo].[Organizations] o
        ON e.[Org] = o.[Code]
    WHERE o.[Id] IS NULL
      AND e.[Org] IS NOT NULL
) t;

-- List the missing organizations
SELECT DISTINCT e.[Org] AS MissingOrgCode
FROM [TABDB].[dbo].[EbillUsers_Staging] e
LEFT JOIN [TABDB].[dbo].[Organizations] o
    ON e.[Org] = o.[Code]
WHERE o.[Id] IS NULL
  AND e.[Org] IS NOT NULL
ORDER BY e.[Org];