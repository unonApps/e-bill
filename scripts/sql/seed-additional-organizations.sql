-- Script to seed additional organizations
USE [TABDB];
GO

-- Insert additional organizations
PRINT 'Seeding additional organizations...';

-- First, let's check what organizations already exist
PRINT 'Existing organizations:';
SELECT Code, Name FROM [dbo].[Organizations] ORDER BY Code;
PRINT '';

-- Check and insert organizations that don't already exist
INSERT INTO [dbo].[Organizations] (Code, Name, CreatedDate)
SELECT Code, Name, GETUTCDATE() FROM (
    VALUES
    ('DSS', 'Department of Safety and Security'),
    ('FAO', 'Food and Agriculture Organization of the United Nations'),
    ('ICAO', 'International Civil Aviation Organization'),
    ('ICC', 'International Criminal Court'),
    ('ICC-CPI', 'International Criminal Court (Cour Pénale Internationale)'),
    ('IFAD', 'International Fund for Agricultural Development'),
    ('ILO', 'International Labour Organization'),
    ('IMO', 'International Maritime Organization'),
    ('OHCHR', 'Office of the High Commissioner for Human Rights'),
    ('OHCHR/FFM', 'OHCHR Fact-Finding Mission'),
    ('OSESG', 'Office of the Special Envoy of the Secretary-General'),
    ('OSESG-GL', 'Office of the Special Envoy of the Secretary-General for the Great Lakes'),
    ('POESOM', 'Peace Operations in Somalia'),
    ('RCO', 'Resident Coordinator''s Office'),
    ('RCS', 'Regional Cooperation Section / Regional Commissions Support'),
    ('UN-WOMEN', 'United Nations Entity for Gender Equality and the Empowerment of Women'),
    ('UNAIDS', 'Joint United Nations Programme on HIV/AIDS'),
    ('UNAMIS', 'United Nations Advance Mission in the Sudan'),
    ('UNCRD', 'United Nations Centre for Regional Development'),
    ('UNDEFINED', 'Placeholder for unclassified/erroneous entry'),
    ('UNDP', 'United Nations Development Programme'),
    ('UNDP-REDD+', 'UNDP Programme on Reducing Emissions from Deforestation and Forest Degradation'),
    ('UNDRR', 'United Nations Office for Disaster Risk Reduction'),
    ('UNDSS', 'United Nations Department of Safety and Security'),
    ('UNDT', 'United Nations Dispute Tribunal'),
    ('UNESCO', 'United Nations Educational, Scientific and Cultural Organization'),
    ('UNFPA', 'United Nations Population Fund'),
    ('UNHCR', 'United Nations High Commissioner for Refugees'),
    ('UNICEN', 'United Nations Information Centre'),
    ('UNICRI', 'United Nations Interregional Crime and Justice Research Institute'),
    ('UNIDO', 'United Nations Industrial Development Organization'),
    ('UNITAMS', 'United Nations Integrated Transition Assistance Mission in Sudan'),
    ('UNKLESA', 'United Nations Kenya Local Expatriate Staff Association'),
    ('UNMC', 'United Nations Medical Centre'),
    ('UNOCHA', 'United Nations Office for the Coordination of Humanitarian Affairs'),
    ('UNODC', 'United Nations Office on Drugs and Crime'),
    ('UNOPS', 'United Nations Office for Project Services'),
    ('UNPD', 'United Nations Procurement Division'),
    ('UNPOS', 'United Nations Political Office for Somalia'),
    ('UNSOA', 'United Nations Support Office for AMISOM'),
    ('UNSOS', 'United Nations Support Office in Somalia'),
    ('WFP', 'World Food Programme'),
    ('WHO', 'World Health Organization'),
    ('WMO', 'World Meteorological Organization')
) AS NewOrgs(Code, Name)
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Organizations]
    WHERE [dbo].[Organizations].Code = NewOrgs.Code
       OR [dbo].[Organizations].Name = NewOrgs.Name
);

PRINT 'Organizations seeded successfully.';

-- Display count of organizations
DECLARE @TotalOrgs INT;
SELECT @TotalOrgs = COUNT(*) FROM [dbo].[Organizations];
PRINT 'Total organizations in database: ' + CAST(@TotalOrgs AS VARCHAR(10));

-- Display all organizations
SELECT Code, Name FROM [dbo].[Organizations] ORDER BY Code;
GO