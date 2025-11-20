# Activation Code API 文档

完整的API接口文档，包含所有端点、请求/响应格式和使用示例。

## 目录

- [基本信息](#基本信息)
- [认证](#认证)
- [公共端点](#公共端点)
- [管理员端点](#管理员端点)
- [错误代码](#错误代码)
- [使用示例](#使用示例)

---

## 基本信息

**Base URL:** `https://api.lovetest.com.cn`

**Content-Type:** `application/json`

**速率限制:**
- 每分钟 3 次请求
- 每小时 10 次请求

超过限制返回 `429 Too Many Requests`

---

## 认证

管理员端点需要JWT令牌认证。

### 获取令牌

首先通过登录端点获取JWT令牌，然后在后续请求中使用：

```http
Authorization: Bearer <your-jwt-token>
```

令牌有效期：**30分钟**

---

## 公共端点

### 验证激活码

验证激活码是否有效。首次使用时激活码，设置7天有效期。

**端点:** `POST /api/activation/validate`

**认证:** 不需要

**请求体:**
```json
{
  "code": "TEST-CODE-001"
}
```

**请求参数:**

| 字段 | 类型 | 必填 | 描述 |
|------|------|------|------|
| code | string | 是 | 激活码 |

**响应示例:**

**成功 - 首次激活 (200 OK):**
```json
{
  "isValid": true,
  "message": "Activation code successfully activated",
  "expiresAt": "2025-11-27T12:00:00Z"
}
```

**成功 - 已激活但未过期 (200 OK):**
```json
{
  "isValid": true,
  "message": "Activation code is valid",
  "expiresAt": "2025-11-27T12:00:00Z"
}
```

**错误 - 激活码不存在 (404 Not Found):**
```json
{
  "isValid": false,
  "message": "Activation code not found",
  "expiresAt": null
}
```

**错误 - 激活码已过期 (400 Bad Request):**
```json
{
  "isValid": false,
  "message": "Activation code has expired",
  "expiresAt": null
}
```

**错误 - 激活码为空 (400 Bad Request):**
```json
{
  "isValid": false,
  "message": "Activation code is required",
  "expiresAt": null
}
```

**cURL 示例:**
```bash
curl -X POST http://api.lovetest.com.cn/api/activation/validate \
  -H "Content-Type: application/json" \
  -d '{"code":"TEST-CODE-001"}'
```

---

## 管理员端点

所有管理员端点（除了登录）都需要JWT令牌认证。

### 1. 管理员登录

获取JWT访问令牌。

**端点:** `POST /api/admin/login`

**认证:** 不需要

**默认凭据:**
- 用户名: `admin`
- 密码: `admin`

**请求体:**
```json
{
  "username": "admin",
  "password": "admin"
}
```

**请求参数:**

| 字段 | 类型 | 必填 | 描述 |
|------|------|------|------|
| username | string | 是 | 管理员用户名 |
| password | string | 是 | 管理员密码 |

**响应示例:**

**成功 (200 OK):**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImFkbWluIiwicm9sZSI6IkFkbWluIiwibmJmIjoxNzAwMjQwMDAwLCJleHAiOjE3MDAyNDE4MDAsImlhdCI6MTcwMDI0MDAwMCwiaXNzIjoiQWN0aXZhdGlvbkNvZGVBcGkiLCJhdWQiOiJBY3RpdmF0aW9uQ29kZUFwaSJ9.xxx"
}
```

**失败 (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Invalid username or password",
  "token": null
}
```

**cURL 示例:**
```bash
curl -X POST http://api.lovetest.com.cn/api/admin/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```

---

### 2. 修改密码

修改管理员密码。

**端点:** `POST /api/admin/change-password`

**认证:** 需要 (Bearer Token)

**请求体:**
```json
{
  "oldPassword": "admin",
  "newPassword": "NewSecurePassword123"
}
```

**请求参数:**

| 字段 | 类型 | 必填 | 描述 |
|------|------|------|------|
| oldPassword | string | 是 | 当前密码 |
| newPassword | string | 是 | 新密码（最少6个字符） |

**响应示例:**

**成功 (200 OK):**
```json
{
  "message": "Password changed successfully"
}
```

**失败 - 旧密码错误 (400 Bad Request):**
```json
{
  "message": "Invalid old password"
}
```

**失败 - 未认证 (401 Unauthorized):**
```json
{
  "message": "User not authenticated"
}
```

**cURL 示例:**
```bash
TOKEN="your-jwt-token"
curl -X POST http://api.lovetest.com.cn/api/admin/change-password \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"oldPassword":"admin","newPassword":"NewSecurePassword123"}'
```

---

### 3. 生成激活码

批量生成新的激活码。

**端点:** `POST /api/admin/generate-codes`

**认证:** 需要 (Bearer Token)

**请求体:**
```json
{
  "count": 1000,
  "prefix": "PROD"
}
```

**请求参数:**

| 字段 | 类型 | 必填 | 描述 |
|------|------|------|------|
| count | integer | 是 | 生成数量（1-20000） |
| prefix | string | 否 | 激活码前缀（默认: "CODE"） |

**响应示例:**

**小批量 ≤100 (200 OK):**
```json
{
  "message": "Successfully generated 10 activation codes",
  "count": 10,
  "prefix": "PROD",
  "codes": [
    "PROD-A1B2C3D4E5F6",
    "PROD-G7H8I9J0K1L2",
    "PROD-M3N4O5P6Q7R8",
    "..."
  ]
}
```

**大批量 >100 (200 OK):**
```json
{
  "message": "Successfully generated 5000 activation codes",
  "count": 5000,
  "prefix": "PROD",
  "note": "Use GET /api/admin/codes to retrieve the generated codes"
}
```

**注意:**
- 最大支持一次生成 20,000 个激活码
- 大批量生成会分批处理（每批1000个）
- 超过100个激活码时，不会在响应中返回完整列表，需要通过查询端点获取

**cURL 示例:**
```bash
TOKEN="your-jwt-token"

# 小批量
curl -X POST http://api.lovetest.com.cn/api/admin/generate-codes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"count":50,"prefix":"TEST"}'

# 大批量
curl -X POST http://api.lovetest.com.cn/api/admin/generate-codes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"count":10000,"prefix":"BULK"}'
```

---

### 4. 查询激活码列表

分页查询所有激活码，支持过滤和跳跃令牌分页。

**端点:** `GET /api/admin/codes`

**认证:** 需要 (Bearer Token)

**查询参数:**

| 参数 | 类型 | 必填 | 默认值 | 描述 |
|------|------|------|--------|------|
| isUsed | boolean | 否 | - | 过滤条件：true=已使用，false=未使用 |
| skipToken | integer | 否 | - | 上一页最后一条记录的ID |
| pageSize | integer | 否 | 100 | 每页数量（1-1000） |

**响应示例 (200 OK):**
```json
{
  "codes": [
    {
      "id": 1,
      "code": "TEST-CODE-001",
      "isUsed": false,
      "activatedAt": null,
      "expiresAt": null
    },
    {
      "id": 2,
      "code": "TEST-CODE-002",
      "isUsed": true,
      "activatedAt": "2025-11-18T10:30:00Z",
      "expiresAt": "2025-11-25T10:30:00Z"
    }
  ],
  "totalCount": 5000,
  "pageSize": 100,
  "nextSkipToken": 100,
  "hasMore": true
}
```

**响应字段:**

| 字段 | 类型 | 描述 |
|------|------|------|
| codes | array | 激活码列表 |
| totalCount | integer | 符合条件的总数 |
| pageSize | integer | 当前页大小 |
| nextSkipToken | integer | 下一页的跳跃令牌（null表示没有更多） |
| hasMore | boolean | 是否还有更多数据 |

**分页示例:**

```bash
TOKEN="your-jwt-token"

# 第一页
curl -X GET "http://api.lovetest.com.cn/api/admin/codes?pageSize=100" \
  -H "Authorization: Bearer $TOKEN"

# 第二页（使用上一页返回的 nextSkipToken）
curl -X GET "http://api.lovetest.com.cn/api/admin/codes?pageSize=100&skipToken=100" \
  -H "Authorization: Bearer $TOKEN"

# 只查询未使用的激活码
curl -X GET "http://api.lovetest.com.cn/api/admin/codes?isUsed=false&pageSize=50" \
  -H "Authorization: Bearer $TOKEN"

# 只查询已使用的激活码
curl -X GET "http://api.lovetest.com.cn/api/admin/codes?isUsed=true&pageSize=50" \
  -H "Authorization: Bearer $TOKEN"
```

---

### 5. 获取统计信息

获取激活码的统计数据。

**端点:** `GET /api/admin/stats`

**认证:** 需要 (Bearer Token)

**响应示例 (200 OK):**
```json
{
  "totalCodes": 10000,
  "unusedCodes": 7500,
  "usedCodes": 2500,
  "activeCodes": 2000
}
```

**响应字段:**

| 字段 | 类型 | 描述 |
|------|------|------|
| totalCodes | integer | 总激活码数 |
| unusedCodes | integer | 未使用的激活码数 |
| usedCodes | integer | 已使用的激活码数 |
| activeCodes | integer | 已使用且未过期的激活码数 |

**cURL 示例:**
```bash
TOKEN="your-jwt-token"
curl -X GET http://api.lovetest.com.cn/api/admin/stats \
  -H "Authorization: Bearer $TOKEN"
```

---

### 6. 删除指定激活码

删除单个激活码。

**端点:** `DELETE /api/admin/codes/{code}`

**认证:** 需要 (Bearer Token)

**路径参数:**

| 参数 | 类型 | 必填 | 描述 |
|------|------|------|------|
| code | string | 是 | 要删除的激活码 |

**响应示例:**

**成功 (200 OK):**
```json
{
  "message": "Code deleted successfully"
}
```

**失败 - 激活码不存在 (404 Not Found):**
```json
{
  "message": "Code not found"
}
```

**cURL 示例:**
```bash
TOKEN="your-jwt-token"
curl -X DELETE http://api.lovetest.com.cn/api/admin/codes/TEST-CODE-001 \
  -H "Authorization: Bearer $TOKEN"
```

---

### 7. 删除所有过期激活码

批量删除所有已过期的激活码。

**端点:** `DELETE /api/admin/codes/expired`

**认证:** 需要 (Bearer Token)

**响应示例 (200 OK):**
```json
{
  "message": "Deleted 150 expired codes"
}
```

**cURL 示例:**
```bash
TOKEN="your-jwt-token"
curl -X DELETE http://api.lovetest.com.cn/api/admin/codes/expired \
  -H "Authorization: Bearer $TOKEN"
```

---

### 8. 初始化数据库

手动初始化数据库（通常不需要，启动时自动执行）。

**端点:** `POST /api/admin/init-database`

**认证:** 需要 (Bearer Token)

**响应示例:**

**成功 (200 OK):**
```json
{
  "message": "Database initialized successfully"
}
```

**失败 (500 Internal Server Error):**
```json
{
  "message": "Failed to initialize database",
  "error": "详细错误信息"
}
```

**cURL 示例:**
```bash
TOKEN="your-jwt-token"
curl -X POST http://api.lovetest.com.cn/api/admin/init-database \
  -H "Authorization: Bearer $TOKEN"
```

---

## 错误代码

| HTTP状态码 | 描述 |
|-----------|------|
| 200 | 成功 |
| 400 | 请求参数错误 |
| 401 | 未认证或令牌无效 |
| 404 | 资源不存在 |
| 429 | 超过速率限制 |
| 500 | 服务器内部错误 |

**通用错误响应格式:**
```json
{
  "message": "错误描述信息"
}
```

---

## 使用示例

### 完整工作流程示例

#### 1. 管理员登录
```bash
# 登录获取令牌
RESPONSE=$(curl -s -X POST http://api.lovetest.com.cn/api/admin/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}')

# 提取令牌
TOKEN=$(echo $RESPONSE | jq -r '.token')
echo "Token: $TOKEN"
```

#### 2. 修改默认密码（推荐）
```bash
curl -X POST http://api.lovetest.com.cn/api/admin/change-password \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"oldPassword":"admin","newPassword":"MySecurePassword123"}'
```

#### 3. 生成激活码
```bash
# 生成100个激活码
curl -X POST http://api.lovetest.com.cn/api/admin/generate-codes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"count":100,"prefix":"PROD"}'
```

#### 4. 查看统计信息
```bash
curl -X GET http://api.lovetest.com.cn/api/admin/stats \
  -H "Authorization: Bearer $TOKEN"
```

#### 5. 查询激活码列表
```bash
# 查询未使用的激活码
curl -X GET "http://api.lovetest.com.cn/api/admin/codes?isUsed=false&pageSize=50" \
  -H "Authorization: Bearer $TOKEN"
```

#### 6. 用户验证激活码
```bash
# 用户端验证激活码（不需要认证）
curl -X POST http://api.lovetest.com.cn/api/activation/validate \
  -H "Content-Type: application/json" \
  -d '{"code":"PROD-A1B2C3D4E5F6"}'
```

#### 7. 清理过期激活码
```bash
# 删除所有过期的激活码
curl -X DELETE http://api.lovetest.com.cn/api/admin/codes/expired \
  -H "Authorization: Bearer $TOKEN"
```

---

### Python 示例

```python
import requests
import json

BASE_URL = "http://api.lovetest.com.cn"

# 1. 登录
login_response = requests.post(
    f"{BASE_URL}/api/admin/login",
    json={"username": "admin", "password": "admin"}
)
token = login_response.json()["token"]
headers = {"Authorization": f"Bearer {token}"}

# 2. 生成激活码
generate_response = requests.post(
    f"{BASE_URL}/api/admin/generate-codes",
    headers=headers,
    json={"count": 10, "prefix": "PYTHON"}
)
print(generate_response.json())

# 3. 获取统计信息
stats_response = requests.get(
    f"{BASE_URL}/api/admin/stats",
    headers=headers
)
print(stats_response.json())

# 4. 验证激活码（用户端）
validate_response = requests.post(
    f"{BASE_URL}/api/activation/validate",
    json={"code": "PYTHON-A1B2C3D4E5F6"}
)
print(validate_response.json())
```

---

### JavaScript/Node.js 示例

```javascript
const axios = require('axios');

const BASE_URL = 'http://api.lovetest.com.cn';

async function main() {
  // 1. 登录
  const loginResponse = await axios.post(`${BASE_URL}/api/admin/login`, {
    username: 'admin',
    password: 'admin'
  });
  const token = loginResponse.data.token;
  const headers = { Authorization: `Bearer ${token}` };

  // 2. 生成激活码
  const generateResponse = await axios.post(
    `${BASE_URL}/api/admin/generate-codes`,
    { count: 10, prefix: 'JS' },
    { headers }
  );
  console.log(generateResponse.data);

  // 3. 获取统计信息
  const statsResponse = await axios.get(
    `${BASE_URL}/api/admin/stats`,
    { headers }
  );
  console.log(statsResponse.data);

  // 4. 验证激活码（用户端）
  const validateResponse = await axios.post(
    `${BASE_URL}/api/activation/validate`,
    { code: 'JS-A1B2C3D4E5F6' }
  );
  console.log(validateResponse.data);
}

main().catch(console.error);
```

---

## 数据模型

### ActivationCode

```json
{
  "id": 1,
  "code": "TEST-CODE-001",
  "isUsed": false,
  "activatedAt": "2025-11-18T10:30:00Z",
  "expiresAt": "2025-11-25T10:30:00Z"
}
```

| 字段 | 类型 | 描述 |
|------|------|------|
| id | integer | 唯一标识符 |
| code | string | 激活码 |
| isUsed | boolean | 是否已使用 |
| activatedAt | datetime | 激活时间（未激活为null） |
| expiresAt | datetime | 过期时间（未激活为null） |

---

## 注意事项

1. **首次运行**: 默认管理员账号为 `admin/admin`，请立即修改密码
2. **令牌过期**: JWT令牌30分钟后过期，需要重新登录
3. **速率限制**: 每分钟3次请求，每小时10次请求
4. **激活码有效期**: 激活后7天内有效
5. **自动清理**: 后台服务每小时自动删除过期激活码
6. **批量生成**: 最大支持一次生成20,000个激活码
7. **分页查询**: 使用skipToken进行高效分页，避免页码漂移

---

## 更新日志

### v1.0.0 (2025-11-18)
- 初始版本发布
- 支持激活码验证
- 支持批量生成激活码（最多20,000个）
- JWT认证
- 速率限制
- 自动过期清理
- 跳跃令牌分页

---

## 技术支持

如有问题，请查看：
- [部署文档](DEPLOYMENT.md)
- [README](README.md)
- [GitHub Actions设置](GITHUB_ACTIONS_SETUP.md)
