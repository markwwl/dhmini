# Endpoint sweep: log every backend GET endpoint and its row count
$ErrorActionPreference = "Continue"
$log = "C:\Users\Administrator\sweep.log"
$base = "http://127.0.0.1:24661"
$out = @()

function Count-Of($v) {
  if ($null -eq $v) { return 0 }
  if ($v -is [System.Array]) { return $v.Count }
  if ($v.PSObject.Properties.Name -contains "items") { return @($v.items).Count }
  if ($v.PSObject.Properties.Name -contains "total") { return $v.total }
  return 1
}

# admin login
$token = ""
try {
  $b = @{ username = "admin"; password = "admin123" } | ConvertTo-Json
  $t = Invoke-RestMethod "$base/api/admin/auth/login" -Method Post -ContentType "application/json" -Body $b -TimeoutSec 15
  $token = $t.token
  $out += "LOGIN ok token_len=$($t.token.Length)"
} catch { $out += "LOGIN ERR $($_.Exception.Message)"; $out | Out-File $log -Encoding utf8; exit 1 }

$H = @{ Authorization = "Bearer $token" }

$adminGets = @(
  "/api/admin/plans",
  "/api/admin/stages?planId=1",
  "/api/admin/weeks?stageId=1",
  "/api/admin/days?weekId=1",
  "/api/admin/dishes?keyword=",
  "/api/admin/candidate-groups",
  "/api/admin/ingredients",
  "/api/admin/content-blocks?type=",
  "/api/admin/content-blocks?type=banner",
  "/api/admin/content-blocks?type=news",
  "/api/admin/content-blocks?type=video",
  "/api/admin/stats/overview",
  "/api/admin/stats/users",
  "/api/admin/stats/checkins?from=2026-08-01&to=2026-09-30"
)
$out += ""
$out += "=== ADMIN GET ==="
foreach ($p in $adminGets) {
  try {
    $x = Invoke-RestMethod "$base$p" -Headers $H -TimeoutSec 15
    $out += ("OK   {0,-58} rows={1}" -f $p, (Count-Of $x))
  } catch { $out += ("ERR  {0,-58} {1}" -f $p, $_.Exception.Message) }
}

$clientGets = @(
  "/api/plans",
  "/api/plans/1",
  "/api/content/blocks?type=banner",
  "/api/content/blocks?type=news",
  "/api/content/blocks?type=video"
)
$out += ""
$out += "=== CLIENT GET ==="
foreach ($p in $clientGets) {
  try {
    $x = Invoke-RestMethod "$base$p" -TimeoutSec 15
    $out += ("OK   {0,-58} rows={1}" -f $p, (Count-Of $x))
  } catch { $out += ("ERR  {0,-58} {1}" -f $p, $_.Exception.Message) }
}

# deep probes
$out += ""
$out += "=== DEEP ==="
try {
  $tree = Invoke-RestMethod "$base/api/plans/1" -TimeoutSec 15
  $out += "plan name=$($tree.name) stages=$(@($tree.stages).Count)"
  foreach ($s in $tree.stages) { $out += ("  stage id={0} name={1} weeks={2}" -f $s.id, $s.name, @($s.weeks).Count) }
  $weekId = $tree.stages[0].weeks[0].id
  $out += "  first weekId=$weekId"
  $day = Invoke-RestMethod "$base/api/weeks/$weekId/days/1/meals" -TimeoutSec 15
  $out += ("  day1 meals={0}" -f @($day.meals).Count)
  foreach ($m in $day.meals) { $out += ("    meal={0} slots={1}" -f $m.mealName, @($m.slots).Count) }
  $slotId = $day.meals[0].slots[0].slotId
  $cand = Invoke-RestMethod "$base/api/dish-slots/$slotId/candidates" -TimeoutSec 15
  $out += ("  candidates(for slot {0})={1}" -f $slotId, @($cand).Count)
} catch { $out += "DEEP ERR $($_.Exception.Message)" }

$out | Out-File $log -Encoding utf8
Write-Host "SWEEP_DONE"
