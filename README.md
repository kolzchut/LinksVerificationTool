Kol-Zchut LinksVerificationTool
===============================

Link checker for Kol-Zchut (www.kolzchut.org.il). It was built for checking outbound links in MediaWiki, by dumping the content of the externallinks table.

The process is as follows:
 1. Query mysql database: <code>SELECT page.page_namespace, page.page_title, externallinks.el_to FROM `externallinks` INNER JOIN page ON page.page_id = externallinks.el_from</code>
 2. Export to CSV
 3. Convert the namespace integer into the proper name and append it to the page title
 4. Remove the leftover column
 5. You should now have a CSV file with two columns:
   1. Origin page (where the link resides)
   2. Target (the link url itself)
 6. Pass this file to the link checker.
 7. Wait a long, long time.
 8. The output file will contain only the failed links, and will be in the same format,
with a third column specifying the type of error.
