<?php
header("Content-disposition: attachment; filename={SetupFileName}");
header("Content-type: application/x-msi");
readfile("{SetupFileName}");
?>