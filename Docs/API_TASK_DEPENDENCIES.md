# Task Dependencies API - Postman Collection Guide

> Base URL: `https://api.nexus.local/api`

---

## 🔐 Authentication

Bütün endpointlər JWT token tələb edir:

```http
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
```

---

## 📚 Endpoints

### 1. Asılılıqları Listlə

```http
GET /tasks/{taskId}/dependencies
```

**Response 200 OK:**
```json
{
  "taskId": 100,
  "dependencies": [
    {
      "dependencyId": 25,
      "taskId": 100,
      "taskTitle": "API Integration",
      "taskStatus": "InProgress",
      "dependsOnTaskId": 50,
      "dependsOnTaskTitle": "Database Setup",
      "dependsOnTaskStatus": "Done",
      "type": "FinishToStart",
      "lagDays": 0,
      "isBlocking": false
    },
    {
      "dependencyId": 26,
      "taskId": 100,
      "dependsOnTaskId": 60,
      "dependsOnTaskTitle": "UI Design",
      "dependsOnTaskStatus": "InProgress",
      "type": "FinishToStart",
      "isBlocking": true
    }
  ]
}
```

---

### 2. Asılı Olan Tapşırıqları Listlə

```http
GET /tasks/{taskId}/dependents
```

Bu tapşırıq hansı tapşırıqların başlanğıc nöqtəsidir.

**Response 200 OK:**
```json
{
  "taskId": 50,
  "dependents": [
    {
      "dependencyId": 25,
      "taskId": 100,
      "taskTitle": "API Integration",
      "taskStatus": "InProgress",
      "dependsOnTaskId": 50,
      "dependsOnTaskTitle": "Database Setup",
      "dependsOnTaskStatus": "Done",
      "type": "FinishToStart",
      "isBlocking": false
    }
  ]
}
```

---

### 3. Yeni Asılılıq Əlavə Et

```http
POST /tasks/{taskId}/dependencies
```

**Request Body:**
```json
{
  "dependsOnTaskId": 50,
  "type": "FinishToStart",
  "lagDays": 2,
  "description": "Database must be ready before API development"
}
```

**Dependency Types:**
- `FinishToStart` (FS): A bitdikdən sonra B başlaya bilər (Ən çox istifadə edilən)
- `StartToStart` (SS): A başladıqdan sonra B başlaya bilər
- `FinishToFinish` (FF): A bitdikdən sonra B bitə bilər
- `StartToFinish` (SF): A başladıqdan sonra B bitə bilər (Nadir)

**Response 201 Created:**
```json
{
  "dependencyId": 25,
  "taskId": 100,
  "dependsOnTaskId": 50,
  "type": "FinishToStart",
  "isValid": true,
  "warning": "Bu asılılıq tapşırığın başlamasını bloklayır"
}
```

**Error Responses:**

```json
// 400 Bad Request - Self dependency
{
  "error": "Tapşırıq özündən asılı ola bilməz"
}

// 400 Bad Request - Different projects
{
  "error": "Fərqli layihələrdəki tapşırıqlar arasında asılılıq yaradıla bilməz"
}

// 400 Bad Request - Circular dependency
{
  "error": "Dairəvi asılılıq yaradıla bilməz (Circular dependency)"
}

// 409 Conflict - Already exists
{
  "error": "Bu asılılıq artıq mövcuddur"
}
```

---

### 4. Asılılığı Sil

```http
DELETE /tasks/{taskId}/dependencies/{dependencyId}
```

**Response 204 No Content**

---

### 5. Bloklanma Statusunu Yoxla

```http
GET /tasks/{taskId}/dependencies/blocked
```

**Response 200 OK:**
```json
{
  "taskId": 100,
  "isBlocked": true
}
```

---

### 6. Başlaya Bilərmi Yoxla

```http
GET /tasks/{taskId}/dependencies/can-start
```

**Response 200 OK:**
```json
{
  "taskId": 100,
  "canStart": false
}
```

---

### 7. Asılılıq Qrafı

```http
GET /tasks/{taskId}/dependencies/graph?depth=3
```

**Response 200 OK:**
```json
{
  "rootTaskId": 100,
  "nodes": [
    {
      "taskId": 100,
      "title": "API Integration",
      "status": "InProgress",
      "depth": 0,
      "isRoot": true
    },
    {
      "taskId": 50,
      "title": "Database Setup",
      "status": "Done",
      "depth": 1,
      "isRoot": false
    },
    {
      "taskId": 40,
      "title": "Server Setup",
      "status": "Done",
      "depth": 2,
      "isRoot": false
    }
  ],
  "edges": [
    {
      "fromTaskId": 100,
      "toTaskId": 50,
      "type": "FinishToStart",
      "isBlocking": false
    },
    {
      "fromTaskId": 50,
      "toTaskId": 40,
      "type": "FinishToStart",
      "isBlocking": false
    }
  ]
}
```

---

## 🔄 Real-World Scenarios

### Scenario 1: Layihə başlanğıc planı

```bash
# 1. Layihə strukturu yarat
Project: "Website Development"
├── Task 10: "Requirements Analysis" (Başlanğıc)
├── Task 20: "UI Design" → depends on 10
├── Task 30: "Database Design" → depends on 10
├── Task 40: "Frontend Development" → depends on 20
├── Task 50: "Backend Development" → depends on 30
└── Task 60: "Integration Testing" → depends on 40, 50

# 2. Asılılıqlar yarat
POST /tasks/20/dependencies  {"dependsOnTaskId": 10, "type": "FinishToStart"}
POST /tasks/30/dependencies  {"dependsOnTaskId": 10, "type": "FinishToStart"}
POST /tasks/40/dependencies  {"dependsOnTaskId": 20, "type": "FinishToStart"}
POST /tasks/50/dependencies  {"dependsOnTaskId": 30, "type": "FinishToStart"}
POST /tasks/60/dependencies  {"dependsOnTaskId": 40, "type": "FinishToStart"}
POST /tasks/60/dependencies  {"dependsOnTaskId": 50, "type": "FinishToStart"}
```

### Scenario 2: Dairəvi asılılıq yoxlanışı

```bash
# Xətalı cəhd: A → B → C → A yaradmaq
POST /tasks/10/dependencies {"dependsOnTaskId": 20}  # A → B (OK)
POST /tasks/20/dependencies {"dependsOnTaskId": 30}  # B → C (OK)
POST /tasks/30/dependencies {"dependsOnTaskId": 10}  # C → A (XƏTA!)

# Response:
{
  "error": "Dairəvi asılılıq yaradıla bilməz (Circular dependency)"
}
```

### Scenario 3: Lag time (gözləmə vaxtı)

```bash
# Beton quruması üçün 7 gün gözləmə
POST /tasks/50/dependencies
{
  "dependsOnTaskId": 40,
  "type": "FinishToStart",
  "lagDays": 7,
  "description": "Concrete curing time"
}
```

---

## 📝 Postman Collection JSON

```json
{
  "info": {
    "name": "Nexus Task Dependencies API",
    "description": "Task dependency management endpoints",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Get Dependencies",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": {
          "raw": "{{base_url}}/tasks/{{task_id}}/dependencies",
          "host": ["{{base_url}}"],
          "path": ["tasks", "{{task_id}}", "dependencies"]
        }
      }
    },
    {
      "name": "Add Dependency",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"dependsOnTaskId\": 50,\n  \"type\": \"FinishToStart\",\n  \"lagDays\": 0,\n  \"description\": \"Depends on previous task\"\n}"
        },
        "url": {
          "raw": "{{base_url}}/tasks/{{task_id}}/dependencies",
          "host": ["{{base_url}}"],
          "path": ["tasks", "{{task_id}}", "dependencies"]
        }
      }
    },
    {
      "name": "Remove Dependency",
      "request": {
        "method": "DELETE",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": {
          "raw": "{{base_url}}/tasks/{{task_id}}/dependencies/{{dependency_id}}",
          "host": ["{{base_url}}"],
          "path": ["tasks", "{{task_id}}", "dependencies", "{{dependency_id}}"]
        }
      }
    },
    {
      "name": "Get Dependents",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": {
          "raw": "{{base_url}}/tasks/{{task_id}}/dependencies/dependents",
          "host": ["{{base_url}}"],
          "path": ["tasks", "{{task_id}}", "dependencies", "dependents"]
        }
      }
    },
    {
      "name": "Check Blocked",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": {
          "raw": "{{base_url}}/tasks/{{task_id}}/dependencies/blocked",
          "host": ["{{base_url}}"],
          "path": ["tasks", "{{task_id}}", "dependencies", "blocked"]
        }
      }
    },
    {
      "name": "Check Can Start",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": {
          "raw": "{{base_url}}/tasks/{{task_id}}/dependencies/can-start",
          "host": ["{{base_url}}"],
          "path": ["tasks", "{{task_id}}", "dependencies", "can-start"]
        }
      }
    },
    {
      "name": "Get Dependency Graph",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": {
          "raw": "{{base_url}}/tasks/{{task_id}}/dependencies/graph?depth=3",
          "host": ["{{base_url}}"],
          "path": ["tasks", "{{task_id}}", "dependencies", "graph"],
          "query": [
            {
              "key": "depth",
              "value": "3"
            }
          ]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "base_url",
      "value": "https://api.nexus.local/api"
    },
    {
      "key": "jwt_token",
      "value": "your-jwt-token-here"
    },
    {
      "key": "task_id",
      "value": "100"
    },
    {
      "key": "dependency_id",
      "value": "25"
    }
  ]
}
```

---

## 🧪 Test Scripts (Postman)

### Pre-request Script (Auth check)
```javascript
pm.test("JWT token exists", function () {
    pm.expect(pm.environment.get("jwt_token")).to.not.be.undefined;
});
```

### Tests (Response validation)
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response has correct structure", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property("taskId");
    pm.expect(jsonData).to.have.property("isBlocked");
});

pm.test("isBlocked is boolean", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.isBlocked).to.be.a('boolean');
});
```

---

## 🚀 Hazır Postman Collection Import

1. Postman açın
2. **Import** → **Raw text**
3. Yuxarıdakı JSON yapışdırın
4. **Import** düyməsinə basın
5. Environment variables təyin edin:
   - `base_url`: `https://api.nexus.local/api`
   - `jwt_token`: Login-dən aldığınız token
   - `task_id`: Test üçün tapşırıq ID

---

**Hazırsınız! 🎉 Artıq API test edə bilərsiniz.**
