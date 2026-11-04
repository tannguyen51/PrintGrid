$ErrorActionPreference = 'Stop'
$path = 'E:\DOC DO An\2_SEP490\Report5_Test Report.xlsx'
$Excel = New-Object -ComObject Excel.Application
$Excel.Visible = $false
$Excel.DisplayAlerts = $false
$wb = $Excel.Workbooks.Open($path)
$st = $wb.Worksheets.Item('Test Statistics')
$st.Range('G3').Value2 = 'TanNXSE181599'
$st.Range('G4').Value2 = 'Nguyen Tan Phuc'
$st.Range('E20').NumberFormat = '0.00'
$st.Range('E21').NumberFormat = '0.00'
$st.Range('E20').Value2 = $st.Range('E20').Value2
$wb.Save()
$wb.Close($true)
$Excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($Excel) | Out-Null
"PATCHED"
