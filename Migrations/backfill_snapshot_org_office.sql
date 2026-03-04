-- Backfill snapshot org/office on existing CallRecords from current EbillUser assignments.
-- Run this manually after the migration completes. May take several minutes on large tables.
-- Safe to re-run (only updates rows where snapshot_org_id IS NULL).

UPDATE cr SET
    cr.snapshot_org_id = eu.OrganizationId,
    cr.snapshot_org_name = o.Name,
    cr.snapshot_office_id = eu.OfficeId,
    cr.snapshot_office_name = ofc.Name,
    cr.snapshot_suboffice_id = eu.SubOfficeId,
    cr.snapshot_suboffice_name = so.Name
FROM ebill.CallRecords cr
INNER JOIN ebill.EbillUsers eu ON cr.ext_resp_index = eu.IndexNumber
LEFT JOIN ebill.Organizations o ON eu.OrganizationId = o.Id
LEFT JOIN ebill.Offices ofc ON eu.OfficeId = ofc.Id
LEFT JOIN ebill.SubOffices so ON eu.SubOfficeId = so.Id
WHERE cr.snapshot_org_id IS NULL;
