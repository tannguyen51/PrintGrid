$ErrorActionPreference = 'Stop'
$html = 'E:\3D PRINT\.tmp_read\report5_clean.html'
$out  = 'E:\DOC DO An\2_SEP490\Report5_Test Documentation.docx'

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0

$doc = $word.Documents.Open($html, $false, $false, $false)

# Page setup: A4, 2.2cm top, 2cm elsewhere
$doc.PageSetup.PaperSize = 7
$doc.PageSetup.TopMargin = 62
$doc.PageSetup.BottomMargin = 57
$doc.PageSetup.LeftMargin = 57
$doc.PageSetup.RightMargin = 57

# Fonts
$doc.Styles.Item('Normal').Font.Name = 'Times New Roman'
$doc.Styles.Item('Normal').Font.Size = 12
$doc.Styles.Item('Heading 1').Font.Name = 'Times New Roman'
$doc.Styles.Item('Heading 1').Font.Size = 16
$doc.Styles.Item('Heading 1').Font.Bold = $true
$doc.Styles.Item('Heading 1').Font.Color = 0
$doc.Styles.Item('Heading 2').Font.Name = 'Times New Roman'
$doc.Styles.Item('Heading 2').Font.Size = 13.5
$doc.Styles.Item('Heading 2').Font.Bold = $true
$doc.Styles.Item('Heading 2').Font.Color = 0
$doc.Styles.Item('Heading 3').Font.Name = 'Times New Roman'
$doc.Styles.Item('Heading 3').Font.Size = 12
$doc.Styles.Item('Heading 3').Font.Bold = $true
$doc.Styles.Item('Heading 3').Font.Color = 0

# Replace the [[TOC]] placeholder with a real Word table of contents
$rng = $doc.Content
$rng.Find.ClearFormatting()
$rng.Find.Text = '[[TOC]]'
$rng.Find.Forward = $true
$rng.Find.Wrap = 1
if ($rng.Find.Execute()) {
    $rng.Text = ''
    $toc = $doc.TablesOfContents.Add($rng, $true, 1, 3)
    $toc.Update()
}

# Page number in the footer
$footer = $doc.Sections.Item(1).Footers.Item(1).Range
$footer.ParagraphFormat.Alignment = 1
$footer.Fields.Add($footer, 33) | Out-Null

$doc.SaveAs2($out, 16)
$doc.Close($false)
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
"DOCX REBUILT"
