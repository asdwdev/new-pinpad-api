# New PINPAD API

API untuk manajemen data PINPAD dengan struktur tabel yang sesuai dengan kebutuhan bisnis.

## Struktur Tabel PINPAD List

Tabel "New PINPAD BTN - PinpadList" memiliki struktur kolom sebagai berikut:

### Kolom-kolom Utama:

1. **Regional** - Nama regional (contoh: "Kantor Pusat", "Kanwil Jakarta I", "Kanwil Jakarta II")
2. **Cabang Induk** - Kode cabang induk (contoh: "00999", "00014", "00043")
3. **Kode Outlet** - Kode outlet/branch (contoh: "00999", "99998", "00198")
4. **Location** - Nama lokasi/branch (contoh: "KANTOR PUSAT", "Cabang Testing ITPD", "KCP ROXY MAS")
5. **Register** - Tanggal dan waktu registrasi (format: "dd-MM-yyyy HH:mm:ss")
6. **Update Date** - Tanggal dan waktu terakhir update (format: "dd-MM-yyyy HH:mm:ss")
7. **Serial Number** - Nomor seri pinpad (contoh: "999999", "298716", "298718")
8. **TID** - Terminal ID (contoh: "00999008", "99998005", "00198001")
9. **Status Pinpad** - Status pinpad (contoh: "Ready To Use", "Not Ready To Use", "Maintenance - Tampering")
10. **Create By** - User yang membuat (contoh: "ITPD-ICP.Julio")
11. **IP Low** - IP address rendah (contoh: "0.0.0.0", "10.198.1.1")
12. **IP High** - IP address tinggi (contoh: "255.255.255.255", "10.198.1.254")
13. **Last Activity** - Waktu aktivitas terakhir (format: "dd-MM-yyyy HH:mm:ss")

## Endpoint API

### 1. GetPinpadList
```
GET /api/Pinpads/GetPinpadList
```

**Query Parameters:**
- `status` - Filter berdasarkan status pinpad
- `regional` - Filter berdasarkan nama regional
- `branch` - Filter berdasarkan nama atau kode branch
- `search` - Pencarian di serial number, TID, atau location
- `page` - Halaman (default: 1)
- `size` - Ukuran per halaman (default: 50)

**Response:**
```json
{
  "success": true,
  "message": "Pinpad list retrieved successfully",
  "data": [
    {
      "regional": "Kantor Pusat",
      "cabangInduk": "00999",
      "kodeOutlet": "00999",
      "location": "KANTOR PUSAT",
      "register": "25-04-2025 09:09:36",
      "updateDate": "14-05-2025 14:03:50",
      "serialNumber": "999999",
      "tID": "00999008",
      "statusPinpad": "Ready To Use",
      "createBy": "ITPD-ICP.Julio",
      "ipLow": "0.0.0.0",
      "ipHigh": "255.255.255.255",
      "lastActivity": ""
    }
  ],
  "total": 100,
  "page": 1,
  "size": 50,
  "totalPages": 2
}
```

### 2. ExportPinpadList
```
GET /api/Pinpads/ExportPinpadList
```

**Query Parameters:**
- `format` - Format export: "csv", "xlsx", atau "pdf" (default: "csv")
- `status` - Filter berdasarkan status pinpad
- `regional` - Filter berdasarkan nama regional
- `branch` - Filter berdasarkan nama atau kode branch
- `search` - Pencarian di serial number, TID, atau location

**Response:** File download sesuai format yang dipilih

## Fitur Utama

1. **Filtering** - Filter berdasarkan status, regional, branch, dan pencarian
2. **Pagination** - Dukungan pagination untuk data yang besar
3. **Export** - Export ke format CSV, Excel, dan PDF
4. **Sorting** - Data diurutkan berdasarkan Regional → Cabang Induk → Kode Outlet
5. **Real-time Data** - Data diambil langsung dari database dengan join yang optimal

## Struktur Database

### Tabel Pinpad
- `PpadId` - Primary key
- `PpadSn` - Serial number
- `PpadBranch` - Foreign key ke Branch
- `PpadStatus` - Status pinpad
- `PpadTid` - Terminal ID
- `PpadCreateBy` - User yang membuat
- `PpadCreateDate` - Tanggal dibuat
- `PpadUpdateDate` - Tanggal update
- `PpadLastActivity` - Aktivitas terakhir

### Tabel Branch
- `Id` - Primary key
- `RegionalId` - Foreign key ke Regional
- `Code` - Kode branch
- `Name` - Nama branch
- `IpLow` - IP address rendah
- `IpHigh` - IP address tinggi
- `ParentBranchId` - ID branch induk (null jika cabang induk)

### Tabel Regional
- `Id` - Primary key
- `Code` - Kode regional
- `Name` - Nama regional

## Cara Penggunaan

1. **Ambil Data List:**
   ```
   GET /api/Pinpads/GetPinpadList?page=1&size=20
   ```

2. **Filter Data:**
   ```
   GET /api/Pinpads/GetPinpadList?status=Ready&regional=Kantor Pusat
   ```

3. **Export Data:**
   ```
   GET /api/Pinpads/ExportPinpadList?format=xlsx&status=Ready
   ```

## Catatan Penting

- Status pinpad dengan maintenance akan ditampilkan sebagai "Maintenance - [Status]"
- Data diurutkan berdasarkan hierarki: Regional → Cabang Induk → Kode Outlet
- Pagination default adalah 50 item per halaman
- Export mendukung 3 format: CSV, Excel (XLSX), dan PDF
- Semua filter bersifat opsional dan dapat dikombinasikan
