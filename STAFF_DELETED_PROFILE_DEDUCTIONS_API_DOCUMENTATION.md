# Staff Panel API Documentation: Deleted Profile Verification Deductions (Clawbacks)

This documentation explains the REST API endpoints used in the Staff Panel to fetch the **Deleted Profile Deductions** (verification incentive clawbacks) for a staff member across any date period, month, or year.

---

## 📌 Business Logic Overview

- **Verification Incentives**: When a staff member verifies a profile (Male or Female Grade A, B, C, D) and it receives Admin Approval, a verification incentive is credited.
- **Incentive Clawback / Deduction**: If any of those verified profiles are subsequently **deleted**, **recycled**, **deactivated**, or **disabled due to reported violations**, the earned verification incentive is deducted from the staff member's payroll.
- **This API provides**:
  1. A comprehensive **financial summary** (Total clawback amount, Male clawback amount/count, Female clawback amount/count, and Grade A/B/C/D breakdown).
  2. A detailed **itemized profile list** with profile ID, name, gender, verification grade, deletion reason/status, and deduction amount.

---

## 🔐 Base URL & Authentication

- **Base URL:** `/api/staff`
- **Authentication Scheme:** `Bearer JWT Token`
- **Required Header:**
  ```http
  Authorization: Bearer <STAFF_JWT_TOKEN>
  ```
- **Allowed Roles:** `Staff`

---

## 📋 API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| `/api/staff/deleted-profile-deductions/{staffId}` | `GET` | **(Recommended)** Fetch deductions for a specific staff ID using a path parameter. |
| `/api/staff/deleted-profile-deductions` | `GET` | Fetch deductions using query parameters (defaults to authenticated staff if `staffId` is omitted). |

---

## 1. Get Deleted Profile Deductions by Staff ID

### 🌐 HTTP Request
- **Method:** `GET`
- **URL:** `/api/staff/deleted-profile-deductions/{staffId}`
- **Route Parameters:**
  | Parameter | Type | Required | Description |
  |---|---|---|---|
  | `staffId` | `long` | Yes | The ID of the staff member (e.g. `10`). |

- **Query Parameters:**
  | Parameter | Type | Required | Default | Description |
  |---|---|---|---|---|
  | `period` | `string` | No | `"Month"` | Options: `"Day"`, `"Week"`, `"Month"`, `"Year"`. |
  | `year` | `integer` | No | Current Year (e.g. `2026`) | Target year filter. |
  | `month` | `integer` | No | Current Month (e.g. `9`) | Target month filter (1–12). |
  | `dateFrom` | `date / ISO` | No | `null` | Custom range start date (e.g. `2026-09-01`). |
  | `dateTo` | `date / ISO` | No | `null` | Custom range end date (e.g. `2026-09-30`). |

---

### 📥 Request Examples

#### A. Fetch for Current Month (Default)
```http
GET /api/staff/deleted-profile-deductions/10 HTTP/1.1
Host: your-domain.com
Authorization: Bearer <STAFF_JWT_TOKEN>
Accept: application/json
```

#### B. Fetch for a Specific Month and Year
```http
GET /api/staff/deleted-profile-deductions/10?year=2026&month=9&period=Month HTTP/1.1
Host: your-domain.com
Authorization: Bearer <STAFF_JWT_TOKEN>
```

#### C. Fetch for Custom Date Range
```http
GET /api/staff/deleted-profile-deductions/10?dateFrom=2026-09-01&dateTo=2026-09-15 HTTP/1.1
Host: your-domain.com
Authorization: Bearer <STAFF_JWT_TOKEN>
```

---

### 📤 Responses

#### ✅ 200 OK — Success Response
```json
{
  "success": true,
  "message": "Deleted profile verification deductions fetched successfully.",
  "data": {
    "staff": {
      "staffId": 10,
      "staffName": "Aisha Khan",
      "email": "aisha@m4nikah.com",
      "department": "Telecaller"
    },
    "filter": {
      "period": "Month",
      "year": 2026,
      "month": 9,
      "startDate": "2026-09-01",
      "endDate": "2026-09-30"
    },
    "summary": {
      "totalDeductionAmount": 600.0,
      "totalDeletedProfiles": 3,
      "maleDeductionAmount": 100.0,
      "maleDeletedCount": 1,
      "femaleDeductionAmount": 500.0,
      "femaleDeletedCount": 2,
      "femaleBreakdown": {
        "gradeA": {
          "count": 1,
          "amount": 300.0
        },
        "gradeB": {
          "count": 1,
          "amount": 200.0
        },
        "gradeC": {
          "count": 0,
          "amount": 0.0
        },
        "gradeD": {
          "count": 0,
          "amount": 0.0
        }
      }
    },
    "deletedProfiles": [
      {
        "profileId": 10425,
        "registerNumber": "M4N10425",
        "profileName": "Farida Yasmin",
        "gender": "Female",
        "photoUrl": "/uploads/profiles/10425.jpg",
        "phone": "+919876543210",
        "email": "farida@example.com",
        "verificationType": "Female Grade A Verification",
        "verificationGrade": "A",
        "verificationStatus": "DetailedVerify",
        "adminApprovalStatus": "Approved",
        "verifiedDate": "2026-09-05",
        "verifiedDateTime": "2026-09-05 11:30:00",
        "deletionStatus": "Account Deleted / Recycled",
        "deletionReason": "Account Deleted / Recycled",
        "deleteReasonText": "User got married elsewhere",
        "deleteReasonId": 2,
        "isRecycled": true,
        "isReportedViolation": false,
        "isActive": false,
        "isDeleted": true,
        "followUpId": 512,
        "deductionAmount": 300.0
      },
      {
        "profileId": 10380,
        "registerNumber": "M4N10380",
        "profileName": "Zainab Begum",
        "gender": "Female",
        "photoUrl": "/uploads/profiles/10380.jpg",
        "phone": "+919876543211",
        "email": "zainab@example.com",
        "verificationType": "Female Grade B Verification",
        "verificationGrade": "B",
        "verificationStatus": "Verify",
        "adminApprovalStatus": "Approved",
        "verifiedDate": "2026-09-08",
        "verifiedDateTime": "2026-09-08 14:20:00",
        "deletionStatus": "Reported Violation",
        "deletionReason": "Reported Violation",
        "deleteReasonText": "Duplicate profile",
        "deleteReasonId": null,
        "isRecycled": false,
        "isReportedViolation": true,
        "isActive": false,
        "isDeleted": false,
        "followUpId": 490,
        "deductionAmount": 200.0
      },
      {
        "profileId": 10112,
        "registerNumber": "M4N10112",
        "profileName": "Tariq Ahmed",
        "gender": "Male",
        "photoUrl": null,
        "phone": "+919876543212",
        "email": "tariq@example.com",
        "verificationType": "Male Verification",
        "verificationGrade": "Male",
        "verificationStatus": "Verify",
        "adminApprovalStatus": "Approved",
        "verifiedDate": "2026-09-10",
        "verifiedDateTime": "2026-09-10 16:45:00",
        "deletionStatus": "Profile Deleted",
        "deletionReason": "Profile Deleted",
        "deleteReasonText": "Deleted by user request",
        "deleteReasonId": 1,
        "isRecycled": false,
        "isReportedViolation": false,
        "isActive": false,
        "isDeleted": true,
        "followUpId": 520,
        "deductionAmount": 100.0
      }
    ]
  }
}
```

#### ❌ 400 Bad Request
```json
{
  "success": false,
  "message": "Invalid Staff ID."
}
```

#### ❌ 404 Not Found
```json
{
  "success": false,
  "message": "Staff member not found."
}
```

---

### 🏷️ Response Fields Reference

#### Top-level & Filter
| Field | Type | Description |
|---|---|---|
| `success` | `boolean` | `true` if request was successful. |
| `message` | `string` | Status message. |
| `data.staff.staffId` | `long` | Staff ID. |
| `data.staff.staffName` | `string` | Staff member's name. |
| `data.filter.period` | `string` | Filter period (`"Day"`, `"Week"`, `"Month"`, `"Year"`). |
| `data.filter.startDate` | `string` | Resolved start date (`YYYY-MM-DD`). |
| `data.filter.endDate` | `string` | Resolved end date (`YYYY-MM-DD`). |

#### Summary Object (`data.summary`)
| Field | Type | Description |
|---|---|---|
| `totalDeductionAmount` | `decimal` | Total amount deducted for all deleted verified profiles in period. |
| `totalDeletedProfiles` | `integer` | Total number of deleted verified profiles. |
| `maleDeductionAmount` | `decimal` | Total deduction amount for deleted male profiles. |
| `maleDeletedCount` | `integer` | Count of deleted male profiles. |
| `femaleDeductionAmount` | `decimal` | Total deduction amount for deleted female profiles. |
| `femaleDeletedCount` | `integer` | Count of deleted female profiles. |
| `femaleBreakdown.gradeA` | `{count, amount}` | Count and amount deducted for Female Grade A profiles. |
| `femaleBreakdown.gradeB` | `{count, amount}` | Count and amount deducted for Female Grade B profiles. |
| `femaleBreakdown.gradeC` | `{count, amount}` | Count and amount deducted for Female Grade C profiles. |
| `femaleBreakdown.gradeD` | `{count, amount}` | Count and amount deducted for Female Grade D profiles. |

#### Deleted Profile Item (`data.deletedProfiles[]`)
| Field | Type | Description |
|---|---|---|
| `profileId` | `long` | Profile ID. |
| `registerNumber` | `string` | Profile registration number (e.g. `"M4N10425"`). |
| `profileName` | `string` | Full name of the candidate. |
| `gender` | `string` | `"Male"` or `"Female"`. |
| `photoUrl` | `string?` | Profile image path. |
| `verificationType` | `string` | Verification type label (e.g. `"Female Grade A Verification"`). |
| `verificationGrade` | `string` | `"A"`, `"B"`, `"C"`, `"D"`, or `"Male"`. |
| `verifiedDate` | `string` | Date the profile was verified (`YYYY-MM-DD`). |
| `deletionStatus` | `string` | Readable reason or status (e.g. `"Account Deleted / Recycled"`). |
| `deleteReasonText` | `string?` | Specific remarks provided when the profile was deleted. |
| `isRecycled` | `boolean` | `true` if profile was recycled into deleted pool. |
| `isReportedViolation` | `boolean` | `true` if profile was disabled due to reported violation. |
| `deductionAmount` | `decimal` | Amount clawed back / deducted for this single profile. |

---

## 💻 Frontend Code Integration Examples

### Example 1: Axios Service (`src/services/deductionsService.js`)

```javascript
import axios from 'axios';

const API_BASE_URL = '/api/staff/deleted-profile-deductions';

const apiClient = axios.create();

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('staff_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/**
 * Fetch deleted profile deductions for a staff member
 * @param {number} staffId
 * @param {object} filters - { year, month, period, dateFrom, dateTo }
 */
export const getDeletedProfileDeductions = async (staffId, filters = {}) => {
  const response = await apiClient.get(`${API_BASE_URL}/${staffId}`, {
    params: filters
  });
  return response.data;
};
```

---

### Example 2: React Component (Deductions Breakdown & Table)

```jsx
import React, { useEffect, useState } from 'react';
import { getDeletedProfileDeductions } from '../services/deductionsService';

export const DeletedProfileDeductionsView = ({ staffId }) => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [month, setMonth] = useState(new Date().getMonth() + 1);
  const [year, setYear] = useState(new Date().getFullYear());

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const res = await getDeletedProfileDeductions(staffId, { year, month });
        if (res.success) {
          setData(res.data);
        }
      } catch (err) {
        console.error('Error fetching deductions:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [staffId, year, month]);

  if (loading) return <div>Loading deductions...</div>;
  if (!data) return <div>No data available.</div>;

  const { summary, deletedProfiles } = data;

  return (
    <div className="deductions-container">
      <h3>Deleted Profile Incentive Deductions (Clawbacks)</h3>

      {/* Summary Cards */}
      <div className="summary-grid">
        <div className="card alert-danger">
          <h4>Total Deduction</h4>
          <p className="amount">₹ {summary.totalDeductionAmount.toLocaleString()}</p>
          <small>{summary.totalDeletedProfiles} Deleted Profiles</small>
        </div>

        <div className="card">
          <h4>Male Clawbacks</h4>
          <p className="amount">₹ {summary.maleDeductionAmount.toLocaleString()}</p>
          <small>{summary.maleDeletedCount} Profiles</small>
        </div>

        <div className="card">
          <h4>Female Clawbacks</h4>
          <p className="amount">₹ {summary.femaleDeductionAmount.toLocaleString()}</p>
          <small>
            Grade A: {summary.femaleBreakdown.gradeA.count} | 
            Grade B: {summary.femaleBreakdown.gradeB.count} | 
            Grade C: {summary.femaleBreakdown.gradeC.count} | 
            Grade D: {summary.femaleBreakdown.gradeD.count}
          </small>
        </div>
      </div>

      {/* Itemized Table */}
      <table className="table table-bordered mt-4">
        <thead>
          <tr>
            <th>Profile ID</th>
            <th>Name</th>
            <th>Gender</th>
            <th>Verified Grade</th>
            <th>Verified Date</th>
            <th>Deletion Status / Reason</th>
            <th>Deduction (₹)</th>
          </tr>
        </thead>
        <tbody>
          {deletedProfiles.length === 0 ? (
            <tr>
              <td colSpan="7" className="text-center">No deleted profile deductions for this period. 🎉</td>
            </tr>
          ) : (
            deletedProfiles.map((p) => (
              <tr key={p.profileId}>
                <td>{p.registerNumber}</td>
                <td>{p.profileName}</td>
                <td>{p.gender}</td>
                <td>
                  <span className="badge">{p.verificationGrade}</span>
                </td>
                <td>{p.verifiedDate}</td>
                <td>
                  <span className="badge badge-warning">{p.deletionStatus}</span>
                  {p.deleteReasonText && <small className="d-block text-muted">{p.deleteReasonText}</small>}
                </td>
                <td className="text-danger font-weight-bold">- ₹ {p.deductionAmount}</td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
};
```

---

### Example 3: Fetch / cURL

```bash
curl -X GET "https://your-domain.com/api/staff/deleted-profile-deductions/10?year=2026&month=9&period=Month" \
     -H "Authorization: Bearer <STAFF_JWT_TOKEN>" \
     -H "Accept: application/json"
```
