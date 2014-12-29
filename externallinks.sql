SELECT page.page_namespace, page.page_title, externallinks.el_to FROM `externallinks` INNER JOIN page ON page.page_id = externallinks.el_from


-- Version that automatically adds the namespace to the title:
SELECT 
	CONCAT(
		CASE page_namespace
			-- MediaWiki standard (canonical) namespaces
			WHEN 0 THEN ''
			WHEN 1 THEN 'Talk:'
			WHEN 2 THEN 'User:'
			WHEN 4 THEN 'Project:'
			WHEN 6 THEN 'File:'
			WHEN 8 THEN 'Mediawiki:'
			WHEN 10 THEN 'Template:'
			WHEN 12 THEN 'Help:'
			WHEN 14 THEN 'Category:'

			-- MediaWiki standard extension
			WHEN 274 THEN 'Widget:'

			-- Kol-Zchut cusom namespaces:
			WHEN 110 THEN 'אודות:'
			WHEN 112 THEN 'קהילת_ידע:'
			WHEN 116 THEN 'חדש:'
			WHEN 118 THEN 'הקפאה:'
			WHEN 120 THEN 'תרגול:'
			WHEN 122 THEN 'נתון:'
			
			-- Default so we can see if the above isn't comprehensive enough:
			ELSE CONCAT(page_namespace,':')
		END,
	page_title) as title,
	externallinks.el_to
FROM `externallinks`
INNER JOIN page ON page.page_id = externallinks.el_from

