$ErrorActionPreference = 'Stop'
$path = 'E:\DOC DO An\2_SEP490\Report5_Unit Test.xls'
$Excel = New-Object -ComObject Excel.Application
$Excel.Visible = $false
$Excel.DisplayAlerts = $false
$wb = $Excel.Workbooks.Open($path)
$names = @('Auth','Model','Order','Commerce','SchedFilter','SchedScore','SchedEngine','JobLifecycle','Slicing','Shipment','LabRegistry')
foreach ($n in $names) {
    $ws = $wb.Worksheets.Item($n)
    $ws.Range('B14:B30').ClearContents()
    $ws.Range('B14').Value2 = 'Test method'
    $ws.Range('A14').Value2 = 'Input'
}
$wb.Save()
$wb.Close($true)
$Excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($Excel) | Out-Null
"CLEANED"
