-- Script to update organization descriptions
USE [TABDB];
GO

PRINT 'Updating organization descriptions...';

-- Update descriptions for existing organizations
UPDATE [dbo].[Organizations] SET [Description] = 'Department responsible for safety and security of UN personnel and premises' WHERE Code = 'DSS';
UPDATE [dbo].[Organizations] SET [Description] = 'UN specialized agency leading international efforts to defeat hunger and improve nutrition and food security' WHERE Code = 'FAO';
UPDATE [dbo].[Organizations] SET [Description] = 'UN specialized agency for international civil aviation standards and regulations' WHERE Code = 'ICAO';
UPDATE [dbo].[Organizations] SET [Description] = 'International court prosecuting individuals for genocide, crimes against humanity, and war crimes' WHERE Code = 'ICC';
UPDATE [dbo].[Organizations] SET [Description] = 'French designation for the International Criminal Court' WHERE Code = 'ICC-CPI';
UPDATE [dbo].[Organizations] SET [Description] = 'International financial institution and UN specialized agency dedicated to eradicating rural poverty' WHERE Code = 'IFAD';
UPDATE [dbo].[Organizations] SET [Description] = 'UN agency dealing with labour issues, particularly international labour standards and decent work' WHERE Code = 'ILO';
UPDATE [dbo].[Organizations] SET [Description] = 'UN specialized agency responsible for maritime safety and security' WHERE Code = 'IMO';
UPDATE [dbo].[Organizations] SET [Description] = 'UN office mandated to promote and protect human rights for all' WHERE Code = 'OHCHR';
UPDATE [dbo].[Organizations] SET [Description] = 'Fact-finding missions established by the Office of the High Commissioner for Human Rights' WHERE Code = 'OHCHR/FFM';
UPDATE [dbo].[Organizations] SET [Description] = 'Office supporting special envoys appointed by the UN Secretary-General' WHERE Code = 'OSESG';
UPDATE [dbo].[Organizations] SET [Description] = 'Office supporting the Special Envoy for the Great Lakes region of Africa' WHERE Code = 'OSESG-GL';
UPDATE [dbo].[Organizations] SET [Description] = 'UN peace operations supporting stability and security in Somalia' WHERE Code = 'POESOM';
UPDATE [dbo].[Organizations] SET [Description] = 'Office coordinating UN development activities at the country level' WHERE Code = 'RCO';
UPDATE [dbo].[Organizations] SET [Description] = 'Section supporting regional cooperation and commissions' WHERE Code = 'RCS';
UPDATE [dbo].[Organizations] SET [Description] = 'UN entity dedicated to gender equality and women empowerment' WHERE Code = 'UN-WOMEN';
UPDATE [dbo].[Organizations] SET [Description] = 'Joint UN programme on HIV/AIDS bringing together 11 UN organizations' WHERE Code = 'UNAIDS';
UPDATE [dbo].[Organizations] SET [Description] = 'Former UN peacekeeping mission in Sudan (historical)' WHERE Code = 'UNAMIS';
UPDATE [dbo].[Organizations] SET [Description] = 'UN centre providing training and research for regional development' WHERE Code = 'UNCRD';
UPDATE [dbo].[Organizations] SET [Description] = 'Placeholder category for unclassified or erroneous organizational entries' WHERE Code = 'UNDEFINED';
UPDATE [dbo].[Organizations] SET [Description] = 'UN development agency helping countries eliminate poverty and achieve sustainable development' WHERE Code = 'UNDP';
UPDATE [dbo].[Organizations] SET [Description] = 'UNDP programme addressing climate change through forest conservation' WHERE Code = 'UNDP-REDD+';
UPDATE [dbo].[Organizations] SET [Description] = 'UN office for disaster risk reduction (formerly UNISDR)' WHERE Code = 'UNDRR';
UPDATE [dbo].[Organizations] SET [Description] = 'Department providing leadership, operational support and oversight for security management' WHERE Code = 'UNDSS';
UPDATE [dbo].[Organizations] SET [Description] = 'UN tribunal for resolving staff disputes' WHERE Code = 'UNDT';
UPDATE [dbo].[Organizations] SET [Description] = 'UN specialized agency promoting international collaboration in education, science, and culture' WHERE Code = 'UNESCO';
UPDATE [dbo].[Organizations] SET [Description] = 'UN sexual and reproductive health agency' WHERE Code = 'UNFPA';
UPDATE [dbo].[Organizations] SET [Description] = 'UN refugee agency protecting refugees, forcibly displaced communities and stateless people' WHERE Code = 'UNHCR';
UPDATE [dbo].[Organizations] SET [Description] = 'Regional UN information centre for public outreach' WHERE Code = 'UNICEN';
UPDATE [dbo].[Organizations] SET [Description] = 'UN institute for crime and justice research' WHERE Code = 'UNICRI';
UPDATE [dbo].[Organizations] SET [Description] = 'UN specialized agency promoting inclusive and sustainable industrial development' WHERE Code = 'UNIDO';
UPDATE [dbo].[Organizations] SET [Description] = 'UN integrated mission supporting political transition in Sudan' WHERE Code = 'UNITAMS';
UPDATE [dbo].[Organizations] SET [Description] = 'Association representing local expatriate staff at UN Kenya' WHERE Code = 'UNKLESA';
UPDATE [dbo].[Organizations] SET [Description] = 'Medical centre providing healthcare services to UN personnel' WHERE Code = 'UNMC';
UPDATE [dbo].[Organizations] SET [Description] = 'UN office coordinating global humanitarian affairs' WHERE Code = 'UNOCHA';
UPDATE [dbo].[Organizations] SET [Description] = 'UN office fighting drugs and crime worldwide' WHERE Code = 'UNODC';
UPDATE [dbo].[Organizations] SET [Description] = 'UN office providing project management and infrastructure services' WHERE Code = 'UNOPS';
UPDATE [dbo].[Organizations] SET [Description] = 'Former UN procurement division (now under Department of Operational Support)' WHERE Code = 'UNPD';
UPDATE [dbo].[Organizations] SET [Description] = 'Former UN political office for Somalia (historical)' WHERE Code = 'UNPOS';
UPDATE [dbo].[Organizations] SET [Description] = 'Former UN support office for AMISOM (replaced by UNSOS)' WHERE Code = 'UNSOA';
UPDATE [dbo].[Organizations] SET [Description] = 'UN support office providing logistical support to peace operations in Somalia' WHERE Code = 'UNSOS';
UPDATE [dbo].[Organizations] SET [Description] = 'UN agency fighting hunger worldwide, delivering food assistance in emergencies' WHERE Code = 'WFP';
UPDATE [dbo].[Organizations] SET [Description] = 'UN specialized agency for international public health' WHERE Code = 'WHO';
UPDATE [dbo].[Organizations] SET [Description] = 'UN specialized agency for meteorology, climate and water resources' WHERE Code = 'WMO';

-- Update existing organizations that were already in database
UPDATE [dbo].[Organizations] SET [Description] = 'Office providing independent internal audit, investigation, inspection and evaluation services' WHERE Code = 'OIOS';
UPDATE [dbo].[Organizations] SET [Description] = 'UN programme coordinating environmental activities and assisting developing countries' WHERE Code = 'UNEP';
UPDATE [dbo].[Organizations] SET [Description] = 'UN programme working towards better urban future (UN-Habitat)' WHERE Code = 'UN-HAB';
UPDATE [dbo].[Organizations] SET [Description] = 'UN information centre for public engagement and outreach' WHERE Code = 'UNIC';
UPDATE [dbo].[Organizations] SET [Description] = 'Major UN office complex and headquarters in Nairobi, Kenya' WHERE Code = 'UNON';

PRINT 'Organization descriptions updated successfully.';

-- Display all organizations with descriptions
SELECT Code, Name, Description
FROM [dbo].[Organizations]
ORDER BY Code;
GO