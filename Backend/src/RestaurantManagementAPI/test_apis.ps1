# Automated API Integration Tests for Restaurant Management System (QLNH)
# Written in PowerShell using Write-Output to guarantee stdout capturing.
# Vietnamese status strings are constructed dynamically via Unicode escape sequences.

$ProgressPreference = 'SilentlyContinue'

$baseUrl = "http://localhost:5276"
$apiStartedByUs = $false

# Dynamic Vietnamese Unicode string construction
$statusTrong = "Tr$([char]0x1ED1)ng"
$statusDangCho = "$([char]0x0110)ang ch$([char]0x1EDD)"
$statusDangCheBien = "$([char]0x0110)ang ch$([char]0x1EBF) bi$([char]0x1EBF)n"
$statusDaXong = "$([char]0x0110)$([char]0x00E3) xong"
$statusDaThanhToan = "$([char]0x0110)$([char]0x00E3) thanh to$([char]0x00E1)n"
$statusDaNghi = "$([char]0x0110)$([char]0x00E3) ngh$([char]0x1EC9)"
$statusChuaThanhToan = "Ch$([char]0x01B0)a thanh to$([char]0x00E1)n"

Write-Output "=========================================================="
Write-Output "   STARTING AUTOMATED API INTEGRATION TESTS FOR QLNH"
Write-Output "=========================================================="

# Stop any existing API process to ensure clean port and fresh run
$existingAPI = Get-Process -Name "RestaurantManagementAPI" -ErrorAction SilentlyContinue
if ($existingAPI) {
    Write-Output "[INFO] Stopping existing RestaurantManagementAPI process..."
    Stop-Process -Name "RestaurantManagementAPI" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# Check if the port is already listening
$portCheck = Get-NetTCPConnection -LocalPort 5276 -ErrorAction SilentlyContinue
if ($portCheck) {
    Write-Output "[INFO] API process is already running on port 5276. Attaching..."
} else {
    Write-Output "[INFO] Port 5276 is empty. Starting API in the background..."
    $apiProcess = Start-Process dotnet -ArgumentList "run --launch-profile http" -PassThru -NoNewWindow -RedirectStandardOutput "api_stdout.log" -RedirectStandardError "api_stderr.log"
    $apiStartedByUs = $true
    
    # Wait for the API to boot up (max 15 seconds)
    $retries = 15
    $booted = $false
    while ($retries -gt 0 -and -not $booted) {
        try {
            $resp = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($resp -eq "Healthy") {
                $booted = $true
            }
        } catch {}
        if (-not $booted) {
            Start-Sleep -Seconds 1
            $retries--
            Write-Output "."
        }
    }
    if (-not $booted) {
        Write-Output "[ERROR] API failed to start in time. Please check build/logs."
        if ($apiProcess) { Stop-Process -Id $apiProcess.Id -Force }
        exit 1
    }
    Write-Output "[SUCCESS] API is up and running!"
}

# Helper to print test headers
function Test-Header($title) {
    Write-Output "`n----------------------------------------------------------"
    Write-Output " [*] $title"
    Write-Output "----------------------------------------------------------"
}

# Helper to report status
function Report-Status($success, $message) {
    if ($success) {
        Write-Output " [PASS] $message"
    } else {
        Write-Output " [FAIL] $message"
        $global:hasFailure = $true
    }
}

$global:hasFailure = $false
$token = ""
$testMaMA = ""
$tableId = ""

try {
    # -------------------------------------------------------------------------
    # TEST CASE 1: Health Check
    # -------------------------------------------------------------------------
    Test-Header "Health Check Endpoint (/health)"
    try {
        $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
        Report-Status ($health -eq "Healthy") "Health Check returned Healthy. DB connection active."
    } catch {
        Report-Status $false "Health Check failed: $_"
    }

    # -------------------------------------------------------------------------
    # TEST CASE 2: Unauthorized Access Protection
    # -------------------------------------------------------------------------
    Test-Header "Security [Authorize] Protection"
    try {
        $res = Invoke-WebRequest -Uri "$baseUrl/api/tables" -Method Get -ErrorAction Stop
        Report-Status $false "Expected 401 Unauthorized but got: $($res.StatusCode)"
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp -and $resp.StatusCode -eq "Unauthorized") {
            Report-Status $true "Block unauthorized access successfully (HTTP 401)."
        } else {
            Report-Status $false "Error checking authorization: $_"
        }
    }

    # -------------------------------------------------------------------------
    # TEST CASE 3: Admin Login Flow
    # -------------------------------------------------------------------------
    Test-Header "Admin Authentication Flow"
    $loginBody = @{
        tenDangNhap = "admin"
        matKhau = "123456"
    } | ConvertTo-Json

    try {
        $loginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
        if ($loginRes.success -and $loginRes.data.accessToken) {
            $token = $loginRes.data.accessToken
            $adminMaNV = $loginRes.data.maNV
            Report-Status $true "Admin login successful! Token retrieved."
        } else {
            Report-Status $false "Login failed: $($loginRes.message)"
        }
    } catch {
        Report-Status $false "Login error: $_"
    }

    if ($token -eq "") {
        Write-Output "[CRITICAL] Cannot retrieve JWT token. Aborting other tests!"
        exit 1
    }

    # Set up headers with authorization token
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    # -------------------------------------------------------------------------
    # TEST CASE 4: Dishes CRUD
    # -------------------------------------------------------------------------
    Test-Header "Dishes CRUD Operations"
    $randomSuffix = (Get-Random -Minimum 1000 -Maximum 9999).ToString()
    $randomName = "Test Dish " + $randomSuffix
    $dishBody = @{
        tenMA = $randomName
        donGia = 75000
        loai = "Appetizer"
        hinhAnh = "test_image.png"
    } | ConvertTo-Json

    try {
        $dishRes = Invoke-RestMethod -Uri "$baseUrl/api/dishes" -Method Post -Headers $headers -Body $dishBody
        if ($dishRes.success -and $dishRes.data) {
            $testMaMA = $dishRes.data.maMA
            Report-Status $true "Created test dish: MaMA = $testMaMA"
            
            # Fetch detail
            $getDish = Invoke-RestMethod -Uri "$baseUrl/api/dishes/$testMaMA" -Method Get
            Report-Status ($getDish.success -and $getDish.data.tenMA -eq $randomName) "Retrieve dish details successfully!"
        } else {
            Report-Status $false "Failed to create dish: $($dishRes.message)"
        }
    } catch {
        Report-Status $false "Dish CRUD error: $_"
    }

    # -------------------------------------------------------------------------
    # TEST CASE 5: Tables CRUD & Pagination
    # -------------------------------------------------------------------------
    Test-Header "Tables CRUD & Pagination"
    $tableId = "B" + (Get-Random -Minimum 30 -Maximum 99)
    $tableBody = @{
        maBan = $tableId
        tenBan = "Test Table $tableId"
        sucChua = 6
        khuVuc = "West Zone"
    } | ConvertTo-Json

    try {
        $tableRes = Invoke-RestMethod -Uri "$baseUrl/api/tables" -Method Post -Headers $headers -Body $tableBody
        if ($tableRes.success) {
            Report-Status $true "Created table successfully ($tableId)"
            
            # List paginated tables
            $listTables = Invoke-RestMethod -Uri "$baseUrl/api/tables?pageNumber=1&pageSize=10" -Method Get -Headers $headers
            if ($listTables.data.items -ne $null) {
                Report-Status $true "Pagination works! Page 1 count: $($listTables.data.items.Count), Total count: $($listTables.data.totalCount)"
            } else {
                Report-Status $false "Pagination did not return Items."
            }
        } else {
            Report-Status $false "Failed to create table: $($tableRes.message)"
        }
    } catch {
        Report-Status $false "Table CRUD error: $_"
    }

    # -------------------------------------------------------------------------
    # TEST CASE 6: Order State Machine & DB Sequences
    # -------------------------------------------------------------------------
    Test-Header "Order State Machine & Sequences"
    if ($testMaMA -eq "") {
        Report-Status $false "Skipping Order tests because test dish creation failed."
    } else {
        # Set table status to Empty (Trong)
        $bodyTrong = $statusTrong | ConvertTo-Json
        Invoke-RestMethod -Uri "$baseUrl/api/tables/$tableId/status" -Method Put -Headers $headers -Body $bodyTrong -ContentType "application/json; charset=utf-8"
        
        $orderBody = @{
            maBan = $tableId
            maNV = $adminMaNV
            chiTietHoaDons = @(
                @{
                    maMA = $testMaMA
                    soLuong = 2
                }
            )
        } | ConvertTo-Json

        try {
            # 1. Create Order
            $orderRes = Invoke-RestMethod -Uri "$baseUrl/api/orders" -Method Post -Headers $headers -Body $orderBody -ContentType "application/json; charset=utf-8"
            if ($orderRes.success -and $orderRes.data) {
                $maHD = $orderRes.data.maHD
                Report-Status $true "Created order successfully. MaHD (from sequence): $maHD"
                Report-Status ($orderRes.data.trangThai -eq $statusChuaThanhToan) "Initial order status is 'Chua thanh toan' (Valid)."
                
                $ct = $orderRes.data.chiTietHoaDons[0]
                Report-Status ($ct.trangThai -eq $statusDangCho) "Initial item status is 'Dang cho' (Valid)."

                # 2. State Machine: Wait -> Processing
                $status1 = @{ newStatus = $statusDangCheBien } | ConvertTo-Json
                $up1 = Invoke-RestMethod -Uri "$baseUrl/api/orders/$maHD/items/$testMaMA/status" -Method Put -Headers $headers -Body $status1 -ContentType "application/json; charset=utf-8"
                Report-Status ($up1.success) "State change Wait -> Processing: Success!"

                # 3. State Machine: Processing -> Completed
                $status2 = @{ newStatus = $statusDaXong } | ConvertTo-Json
                $up2 = Invoke-RestMethod -Uri "$baseUrl/api/orders/$maHD/items/$testMaMA/status" -Method Put -Headers $headers -Body $status2 -ContentType "application/json; charset=utf-8"
                Report-Status ($up2.success) "State change Processing -> Completed: Success!"

                # 4. Checkout Order
                $checkoutBody = @{ paymentMethod = "Cash" } | ConvertTo-Json
                $checkoutRes = Invoke-RestMethod -Uri "$baseUrl/api/orders/$maHD/checkout" -Method Post -Headers $headers -Body $checkoutBody -ContentType "application/json; charset=utf-8"
                if ($checkoutRes.success) {
                    Report-Status $true "Checked out order $maHD successfully."
                    
                    # Verify final status
                    $getOrder = Invoke-RestMethod -Uri "$baseUrl/api/orders/$maHD" -Method Get -Headers $headers
                    Report-Status ($getOrder.data.trangThai -eq $statusDaThanhToan) "Order transition to terminal 'Da thanh toan': Success!"

                    # 5. State Machine: Duplicate checkout block
                    try {
                        $reCheckout = Invoke-RestMethod -Uri "$baseUrl/api/orders/$maHD/checkout" -Method Post -Headers $headers -Body $checkoutBody -ContentType "application/json; charset=utf-8"
                        Report-Status $false "Block duplicate checkout failed!"
                    } catch {
                        Report-Status $true "Block duplicate checkout successfully (State Machine protection)."
                    }
                } else {
                    Report-Status $false "Checkout failed: $($checkoutRes.message)"
                }
            } else {
                Report-Status $false "Order creation failed: $($orderRes.message)"
            }
        } catch [System.Net.WebException] {
            $respStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($respStream)
            $respBody = $reader.ReadToEnd()
            Report-Status $false "Order flow failed. API Response: $respBody"
        } catch {
            Report-Status $false "Order flow error: $_"
        }
    }

    # -------------------------------------------------------------------------
    # TEST CASE 7: Reservation Overlapping Conflict Check
    # -------------------------------------------------------------------------
    Test-Header "Reservation Conflict Checks"
    $bookingTime = (Get-Date).AddDays(1)
    $timeStr = $bookingTime.ToString("yyyy-MM-ddTHH:00:00")
    
    $resBody1 = @{
        maBan = "B02"
        tenKhachHang = "Customer A"
        soDienThoai = "0987654321"
        thoiGianDat = $timeStr
        soNguoi = 4
    } | ConvertTo-Json

    try {
        # 1. First booking succeeds
        $book1 = Invoke-RestMethod -Uri "$baseUrl/api/reservations" -Method Post -Headers $headers -Body $resBody1
        if ($book1.success -and $book1.data) {
            $maDatBan = $book1.data.maDatBan
            Report-Status $true "Reservation 1 created (MaDatBan from sequence: $maDatBan)"
            
            # 2. Second overlapping booking fails (same table B02, within 2 hours interval)
            $overlappingTime = $bookingTime.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:00")
            $resBody2 = @{
                maBan = "B02"
                tenKhachHang = "Customer B"
                soDienThoai = "0912345678"
                thoiGianDat = $overlappingTime
                soNguoi = 2
            } | ConvertTo-Json
            
            try {
                $book2 = Invoke-RestMethod -Uri "$baseUrl/api/reservations" -Method Post -Headers $headers -Body $resBody2
                Report-Status $false "Overlapping block failed! Overlapping reservation was allowed."
            } catch {
                Report-Status $true "Block overlapping reservation successfully (within 2-hour window)."
            }
            
            # 3. Cancel reservation succeeds
            $cancelRes = Invoke-RestMethod -Uri "$baseUrl/api/reservations/$maDatBan/cancel" -Method Post -Headers $headers
            Report-Status ($cancelRes.success) "Cancelled reservation successfully!"
        } else {
            Report-Status $false "Reservation 1 creation failed: $($book1.message)"
        }
    } catch {
        Report-Status $false "Reservation test error: $_"
    }

    # -------------------------------------------------------------------------
    # TEST CASE 8: User Soft Delete Flow (Email is empty to skip SMTP)
    # -------------------------------------------------------------------------
    Test-Header "User Soft Delete Flow"
    $randomUser = "user" + (Get-Random -Minimum 1000 -Maximum 9999)
    $registerBody = @{
        tenDangNhap = $randomUser
        matKhau = "Nhanvien123"
        hoTen = "Test Employee"
        email = $null
        sdt = "0900000000"
        chucVu = "NhanVien"
    } | ConvertTo-Json

    try {
        $regRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
        if ($regRes.success -and $regRes.data) {
            $newMaNV = $regRes.data.maNV
            Report-Status $true "Registered test user successfully without SMTP: MaNV = $newMaNV"
            
            # Admin calls Soft Delete on this user
            $delRes = Invoke-RestMethod -Uri "$baseUrl/api/users/$newMaNV" -Method Delete -Headers $headers
            if ($delRes.success) {
                Report-Status $true "Admin soft deleted user $newMaNV successfully."
                
                # Verify status in DB (via list endpoint)
                $users = Invoke-RestMethod -Uri "$baseUrl/api/users" -Method Get -Headers $headers
                # Select target user
                $targetNV = $users.data | Where-Object { $_.maNV -eq $newMaNV }
                $trangThaiNV = $targetNV.trangThai
                $isActive = $targetNV.isActive
                
                Report-Status ($trangThaiNV -eq $statusDaNghi -and $isActive -eq $false) "Verified user TrangThai is 'Da nghi' and IsActive is False!"
            } else {
                Report-Status $false "Soft Delete failed: $($delRes.message)"
            }
        } else {
            Report-Status $false "User registration failed: $($regRes.message)"
        }
        } catch [System.Net.WebException] {
            $respStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($respStream)
            $respBody = $reader.ReadToEnd()
            Report-Status $false "User flow failed. API Response: $respBody"
        } catch {
            Report-Status $false "User flow error: $_"
        }

    # -------------------------------------------------------------------------
    # Clean up test table
    # -------------------------------------------------------------------------
    try {
        if ($tableId -ne "") {
            $cleanup = Invoke-RestMethod -Uri "$baseUrl/api/tables/$tableId" -Method Delete -Headers $headers -ErrorAction SilentlyContinue
        }
    } catch {}

} finally {
    # Clean up background process if we started it
    if ($apiStartedByUs -and $apiProcess) {
        Write-Output "`n[INFO] Stopping background API process..."
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        Write-Output "[SUCCESS] Background API stopped."
    }
}

Write-Output "`n=========================================================="
if ($global:hasFailure) {
    Write-Output "   TEST RESULTS: FAILURE(S) DETECTED. PLEASE RESOLVE!"
} else {
    Write-Output "   TEST RESULTS: ALL INTEGRATION TESTS PASSED PERFECTLY! (100 PERCENT SUCCESS)"
}
Write-Output "=========================================================="
