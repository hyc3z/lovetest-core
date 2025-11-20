# 健康检查文档

本文档说明API的健康检查端点和Kubernetes探针配置。

## 健康检查端点

### 1. Liveness Probe - 存活检查

**端点:** `GET /api/health/live`

**用途:** 检查应用是否存活，如果失败Kubernetes会重启Pod

**响应示例:**
```json
{
  "status": "alive",
  "timestamp": "2025-11-18T10:30:00Z"
}
```

**HTTP状态码:**
- `200 OK` - 应用存活
- `503 Service Unavailable` - 应用不可用

**测试命令:**
```bash
curl http://localhost:8080/api/health/live
```

---

### 2. Readiness Probe - 就绪检查

**端点:** `GET /api/health/ready`

**用途:** 检查应用是否准备好接收流量，如果失败Kubernetes会将Pod从Service中移除

**检查项:**
- ✅ 数据库连接
- ✅ 数据库初始化状态（Admin用户是否存在）

**响应示例:**

**成功 (200 OK):**
```json
{
  "status": "ready",
  "database": "connected",
  "timestamp": "2025-11-18T10:30:00Z"
}
```

**失败 (503 Service Unavailable):**
```json
{
  "status": "not_ready",
  "reason": "database_unavailable",
  "timestamp": "2025-11-18T10:30:00Z"
}
```

**测试命令:**
```bash
curl http://localhost:8080/api/health/ready
```

---

### 3. Startup Probe - 启动检查

**端点:** `GET /api/health/startup`

**用途:** 检查应用是否已完成启动，用于慢启动的应用

**检查项:**
- ✅ 数据库连接
- ✅ 数据库初始化完成

**响应示例:**

**成功 (200 OK):**
```json
{
  "status": "started",
  "database": "initialized",
  "timestamp": "2025-11-18T10:30:00Z"
}
```

**失败 (503 Service Unavailable):**
```json
{
  "status": "starting",
  "reason": "database_unavailable",
  "timestamp": "2025-11-18T10:30:00Z"
}
```

**测试命令:**
```bash
curl http://localhost:8080/api/health/startup
```

---

### 4. 综合健康检查

**端点:** `GET /api/health`

**用途:** 返回详细的健康状态信息，用于监控和调试

**响应示例:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-18T10:30:00Z",
  "checks": {
    "database": {
      "status": "healthy",
      "message": "Connected"
    },
    "database_data": {
      "status": "healthy",
      "adminUsers": 1,
      "activationCodes": 5000
    },
    "version": {
      "status": "healthy",
      "version": "1.0.0"
    }
  }
}
```

**测试命令:**
```bash
curl http://localhost:8080/api/health
```

---

## Kubernetes探针配置

### Deployment配置

```yaml
containers:
- name: api
  image: omaticaya/lovetest-core:latest
  
  # Startup probe - 启动检查
  startupProbe:
    httpGet:
      path: /api/health/startup
      port: 8080
    initialDelaySeconds: 0
    periodSeconds: 5
    timeoutSeconds: 3
    successThreshold: 1
    failureThreshold: 30  # 最多等待150秒启动
  
  # Liveness probe - 存活检查
  livenessProbe:
    httpGet:
      path: /api/health/live
      port: 8080
    initialDelaySeconds: 30
    periodSeconds: 10
    timeoutSeconds: 5
    successThreshold: 1
    failureThreshold: 3
  
  # Readiness probe - 就绪检查
  readinessProbe:
    httpGet:
      path: /api/health/ready
      port: 8080
    initialDelaySeconds: 10
    periodSeconds: 5
    timeoutSeconds: 3
    successThreshold: 1
    failureThreshold: 3
```

### 探针参数说明

| 参数 | 说明 | Startup | Liveness | Readiness |
|------|------|---------|----------|-----------|
| `initialDelaySeconds` | 首次检查延迟 | 0秒 | 30秒 | 10秒 |
| `periodSeconds` | 检查间隔 | 5秒 | 10秒 | 5秒 |
| `timeoutSeconds` | 超时时间 | 3秒 | 5秒 | 3秒 |
| `successThreshold` | 成功阈值 | 1次 | 1次 | 1次 |
| `failureThreshold` | 失败阈值 | 30次 | 3次 | 3次 |

### 探针行为

#### Startup Probe
- **目的:** 保护慢启动的应用
- **行为:** 在startup probe成功之前，liveness和readiness probe不会执行
- **失败后果:** 如果30次检查（150秒）都失败，Pod会被重启
- **适用场景:** 数据库初始化、数据迁移等耗时操作

#### Liveness Probe
- **目的:** 检测应用死锁或无响应
- **行为:** 定期检查应用是否存活
- **失败后果:** 连续3次失败后，Kubernetes会重启Pod
- **适用场景:** 应用崩溃、死锁、内存泄漏

#### Readiness Probe
- **目的:** 控制流量路由
- **行为:** 定期检查应用是否准备好处理请求
- **失败后果:** 连续3次失败后，Pod会从Service的endpoints中移除
- **适用场景:** 数据库连接断开、依赖服务不可用

---

## 探针时间线

```
时间轴: 0s -----> 10s -----> 30s -----> 40s -----> ...

Startup:   [检查] [检查] [检查] [成功✓]
                                    |
Readiness:                          [等待] [检查] [成功✓] [检查] ...
                                                      |
Liveness:                                             [等待] [检查] ...
```

1. **0-30秒:** Startup probe每5秒检查一次
2. **30秒:** Startup probe成功后，Readiness probe开始
3. **40秒:** Readiness probe成功后，Pod开始接收流量
4. **40秒后:** Liveness probe开始定期检查

---

## 监控和调试

### 查看探针状态

```bash
# 查看Pod状态
kubectl get pods -n lovetest

# 查看Pod详情（包含探针状态）
kubectl describe pod <pod-name> -n lovetest

# 查看Pod事件
kubectl get events -n lovetest --field-selector involvedObject.name=<pod-name>
```

### 常见探针事件

**Startup probe失败:**
```
Warning  Unhealthy  Pod  Startup probe failed: HTTP probe failed with statuscode: 503
```

**Liveness probe失败:**
```
Warning  Unhealthy  Pod  Liveness probe failed: HTTP probe failed with statuscode: 503
Warning  Killing    Pod  Container api failed liveness probe, will be restarted
```

**Readiness probe失败:**
```
Warning  Unhealthy  Pod  Readiness probe failed: HTTP probe failed with statuscode: 503
```

### 手动测试探针

```bash
# 进入Pod
kubectl exec -it <pod-name> -n lovetest -- sh

# 测试探针端点
curl http://localhost:8080/api/health/live
curl http://localhost:8080/api/health/ready
curl http://localhost:8080/api/health/startup
curl http://localhost:8080/api/health
```

### 从集群外测试

```bash
# 端口转发
kubectl port-forward -n lovetest deployment/lovetest-api 8080:8080

# 在另一个终端测试
curl http://localhost:8080/api/health/live
curl http://localhost:8080/api/health/ready
```

---

## 故障排查

### Pod一直重启

**症状:**
```
NAME                            READY   STATUS             RESTARTS   AGE
lovetest-api-xxx                0/1     CrashLoopBackOff   5          5m
```

**可能原因:**
1. Liveness probe失败次数过多
2. Startup probe超时
3. 应用启动失败

**排查步骤:**
```bash
# 1. 查看Pod日志
kubectl logs <pod-name> -n lovetest

# 2. 查看上一次容器日志
kubectl logs <pod-name> -n lovetest --previous

# 3. 查看Pod事件
kubectl describe pod <pod-name> -n lovetest

# 4. 检查探针配置
kubectl get pod <pod-name> -n lovetest -o yaml | grep -A 20 "livenessProbe"
```

### Pod不接收流量

**症状:**
Service无法访问，但Pod在运行

**可能原因:**
Readiness probe失败

**排查步骤:**
```bash
# 1. 检查Pod readiness状态
kubectl get pods -n lovetest
# READY列显示 0/1 表示未就绪

# 2. 查看endpoints
kubectl get endpoints lovetest-api -n lovetest
# 如果addresses为空，说明没有就绪的Pod

# 3. 手动测试readiness端点
kubectl exec -it <pod-name> -n lovetest -- curl http://localhost:8080/api/health/ready

# 4. 查看应用日志
kubectl logs <pod-name> -n lovetest
```

### 启动时间过长

**症状:**
Pod长时间处于NotReady状态

**解决方案:**
1. 增加startup probe的failureThreshold：
```yaml
startupProbe:
  failureThreshold: 60  # 增加到300秒
```

2. 优化应用启动时间：
   - 延迟非关键初始化
   - 使用异步初始化
   - 优化数据库连接

### 频繁触发探针

**症状:**
日志中大量探针请求

**解决方案:**
1. 增加periodSeconds：
```yaml
readinessProbe:
  periodSeconds: 10  # 从5秒增加到10秒
```

2. 优化探针端点性能
3. 添加探针请求日志过滤

---

## 最佳实践

### 1. 探针端点设计

✅ **推荐:**
- 轻量级检查
- 快速响应（<1秒）
- 明确的成功/失败状态
- 包含必要的依赖检查

❌ **避免:**
- 复杂的业务逻辑
- 长时间运行的操作
- 过多的外部依赖检查
- 写入操作

### 2. 探针配置

✅ **推荐:**
- 根据应用特性调整参数
- Startup probe保护慢启动
- Readiness probe检查关键依赖
- Liveness probe检查应用存活

❌ **避免:**
- 过短的超时时间
- 过低的失败阈值
- 所有探针使用相同配置
- 忽略startup probe

### 3. 监控和告警

✅ **推荐:**
- 监控探针失败率
- 设置探针失败告警
- 记录探针失败原因
- 定期审查探针配置

### 4. 测试

✅ **推荐:**
- 本地测试探针端点
- 模拟故障场景
- 验证探针行为
- 压力测试探针性能

---

## 探针配置模板

### 快速启动应用
```yaml
startupProbe:
  httpGet:
    path: /api/health/startup
    port: 8080
  initialDelaySeconds: 0
  periodSeconds: 2
  failureThreshold: 15  # 30秒超时

livenessProbe:
  httpGet:
    path: /api/health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 2
```

### 慢启动应用
```yaml
startupProbe:
  httpGet:
    path: /api/health/startup
    port: 8080
  initialDelaySeconds: 0
  periodSeconds: 10
  failureThreshold: 60  # 600秒超时

livenessProbe:
  httpGet:
    path: /api/health/live
    port: 8080
  initialDelaySeconds: 60
  periodSeconds: 30
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10
  failureThreshold: 3
```

### 高可用应用
```yaml
startupProbe:
  httpGet:
    path: /api/health/startup
    port: 8080
  initialDelaySeconds: 0
  periodSeconds: 5
  failureThreshold: 30

livenessProbe:
  httpGet:
    path: /api/health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 5
  timeoutSeconds: 3
  failureThreshold: 2  # 快速检测故障

readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 3
  timeoutSeconds: 2
  failureThreshold: 2  # 快速移除故障Pod
```

---

## 相关文档

- [API文档](API_DOCUMENTATION.md)
- [部署文档](DEPLOYMENT.md)
- [K8s部署](k8s/README.md)
- [故障排查](TROUBLESHOOTING.md)
- [Kubernetes官方文档 - Configure Liveness, Readiness and Startup Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
