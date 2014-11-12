Kol-Zchut LinksVerificationTool
===============================

Link checker for Kol-Zchut (www.kolzchut.org.il). It was built for checking outbound links in MediaWiki,
by dumping the content of the externallinks table.

The process is as follows:
 1. Query MediaWiki mysql database for links. A sample query is included in <code>externallinks.sql</code>
 2. Export to CSV
 3. If you haven't used the sample query in step 1:
   1. You need to convert the namespace number into the proper name and prepend it to the page title
   2. Remove the leftover column
 4. You should now have a CSV file with two columns:
   1. Origin page (where the link resides)
   2. Target (the link url itself)
 5. Pass this file to the link checker.
 6. Wait a long, long time.
 7. The output file will contain only the failed links, and will be in the same format,
with a third column specifying the type of error.
