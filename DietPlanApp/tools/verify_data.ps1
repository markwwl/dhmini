$log = "C:\Users\Administrator\verify_data2.log"
$base = "http://127.0.0.1:24661"
$r = @()

$b = @{ username = "admin"; password = "admin123" } | ConvertTo-Json
$t = Invoke-RestMethod "$base/api/admin/auth/login" -Method Post -ContentType "application/json" -Body $b -TimeoutSec 15
$H = @{ Authorization = "Bearer " + $t.token }

"=== OVERVIEW ===" | Out-File $log -Encoding utf8
try { $o = Invoke-RestMethod "$base/api/admin/stats/overview" -Headers $H -TimeoutSec 15; ($o | ConvertTo-Json -Compress) | Out-File $log -Encoding utf8 -Append } catch { "ERR " + $_.Exception.Message | Out-File $log -Encoding utf8 -Append }

"=== DISH STATUS FILTER ===" | Out-File $log -Encoding utf8 -Append
foreach ($s in @(0,1,2)) {
  try { $x = Invoke-RestMethod "$base/api/admin/dishes?status=$s" -Headers $H -TimeoutSec 15; "status=$s rows=" + @($x).Count | Out-File $log -Encoding utf8 -Append } catch { "status=$s ERR " + $_.Exception.Message | Out-File $log -Encoding utf8 -Append }
}
"=== DISH CATEGORY FILTER ===" | Out-File $log -Encoding utf8 -Append
foreach ($c in @("早餐","主食","荤菜","素菜","汤品","加餐","饮品")) {
  $u = [System.Uri]::EscapeDataString($c)
  try { $x = Invoke-RestMethod "$base/api/admin/dishes?category=$u" -Headers $H -TimeoutSec 15; "cat=$c rows=" + @($x).Count | Out-File $log -Encoding utf8 -Append } catch { "cat=$c ERR" | Out-File $log -Encoding utf8 -Append }
}

"=== PLAN LIST (admin) ===" | Out-File $log -Encoding utf8 -Append
try { $p = Invoke-RestMethod "$base/api/admin/plans" -Headers $H -TimeoutSec 15; foreach ($x in $p) { "  id=" + $x.id + " name=" + $x.name + " status=" + $x.status + " sort=" + $x.sort | Out-File $log -Encoding utf8 -Append } } catch { "ERR" | Out-File $log -Encoding utf8 -Append }

"=== PLAN 2 TREE ===" | Out-File $log -Encoding utf8 -Append
try {
  $tr = Invoke-RestMethod "$base/api/plans/2" -TimeoutSec 15
  "  name=" + $tr.name + " stages=" + @($tr.stages).Count | Out-File $log -Encoding utf8 -Append
  foreach ($s in $tr.stages) { "    id=" + $s.id + " " + $s.name + "/" + $s.theme + " weeks=" + @($s.weeks).Count | Out-File $log -Encoding utf8 -Append }
} catch { "ERR " + $_.Exception.Message | Out-File $log -Encoding utf8 -Append }

"=== OFFLINE PLAN 3 via client (should 404) ===" | Out-File $log -Encoding utf8 -Append
try { $x = Invoke-WebRequest "$base/api/plans/3" -TimeoutSec 15 -UseBasicParsing; "  status=" + $x.StatusCode | Out-File $log -Encoding utf8 -Append } catch { "  status=" + [int]$_.Exception.Response.StatusCode | Out-File $log -Encoding utf8 -Append }

"=== DEMO LOGIN + SUMMARY ===" | Out-File $log -Encoding utf8 -Append
try {
  $lb = @{ code = "any" } | ConvertTo-Json
  $l = Invoke-RestMethod "$base/api/auth/wx-login" -Method Post -ContentType "application/json" -Body $lb -TimeoutSec 15
  "  login userId=" + $l.userId + " nick=" + $l.nickname | Out-File $log -Encoding utf8 -Append
  $s = Invoke-RestMethod "$base/api/checkins/summary" -Headers @{ Authorization = "Bearer " + $l.token } -TimeoutSec 15
  ($s | ConvertTo-Json -Compress) | Out-File $log -Encoding utf8 -Append
} catch { "  ERR " + $_.Exception.Message | Out-File $log -Encoding utf8 -Append }

"=== DISH DETAIL 1 ===" | Out-File $log -Encoding utf8 -Append
try { $d = Invoke-RestMethod "$base/api/dishes/1" -TimeoutSec 15; "  name=" + $d.name + " images=" + @($d.images).Count + " ings=" + @($d.ingredients).Count + " steps=" + @($d.steps).Count | Out-File $log -Encoding utf8 -Append } catch { "  ERR" | Out-File $log -Encoding utf8 -Append }

"=== INGREDIENTS ===" | Out-File $log -Encoding utf8 -Append
try { $i = Invoke-RestMethod "$base/api/admin/ingredients" -Headers $H -TimeoutSec 15; "  rows=" + @($i).Count | Out-File $log -Encoding utf8 -Append } catch { "  ERR" | Out-File $log -Encoding utf8 -Append }

"=== CANDIDATE GROUP MEMBERS ===" | Out-File $log -Encoding utf8 -Append
try { $g = Invoke-RestMethod "$base/api/admin/candidate-groups" -Headers $H -TimeoutSec 15; foreach ($x in $g) { "  g" + $x.id + " " + $x.name + " members=" + @($x.items).Count | Out-File $log -Encoding utf8 -Append } } catch { "  ERR" | Out-File $log -Encoding utf8 -Append }

Write-Host DONE
