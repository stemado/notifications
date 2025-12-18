# Health Endpoints Documentation

## Overview

The NotificationService.Api provides multiple health check endpoints designed for monitoring, container orchestration (Kubernetes), and operational diagnostics. These endpoints require no authentication and can be used by monitoring systems, load balancers, and orchestration platforms.

## Endpoints Summary

| Endpoint | Purpose | Use Case |
|----------|---------|----------|
| `/health` | Built-in health check with detailed component status | Infrastructure monitoring, detailed diagnostics |
| `/health/live` | Liveness probe | Kubernetes liveness probe, basic uptime check |
| `/health/ready` | Readiness probe | Kubernetes readiness probe, load balancer health |
| `/api/health` | Overall service health with channel metrics | Application monitoring, dashboard integration |

---

## Endpoint Details

### 1. GET /health

Built-in ASP.NET Core health check endpoint with detailed component-level diagnostics.

#### Purpose
- Provides detailed health status for all registered health checks
- Includes database connectivity, memory usage, and component-specific checks
- Returns granular diagnostics for troubleshooting

#### Request
```http
GET /health HTTP/1.1
Host: localhost:5201
```

#### Response Format
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "MySQL database connection is healthy",
      "duration": 45.2
    },
    {
      "name": "self",
      "status": "Healthy",
      "description": null,
      "duration": 0.1
    }
  ],
  "totalDuration": 45.3
}
```

#### Response Fields
- `status` (string): Overall health status - `Healthy`, `Degraded`, or `Unhealthy`
- `checks` (array): Individual health check results
  - `name` (string): Health check identifier
  - `status` (string): Check-specific status
  - `description` (string|null): Optional status description or error message
  - `duration` (number): Check execution time in milliseconds
- `totalDuration` (number): Total health check execution time in milliseconds

#### HTTP Status Codes
- `200 OK`: Service is healthy or degraded
- `503 Service Unavailable`: Service is unhealthy

#### Use Cases
- Infrastructure monitoring dashboards
- Detailed health diagnostics
- Component-level troubleshooting
- Monitoring system integration (Prometheus, Datadog, etc.)

---

### 2. GET /health/live

Simple liveness probe indicating the service process is running.

#### Purpose
- Confirms the service is alive and responding
- Does not check dependencies (database, external services)
- Returns immediately without expensive checks
- Used by Kubernetes to restart failed pods

#### Request
```http
GET /health/live HTTP/1.1
Host: localhost:5201
```

#### Response Format
```json
{
  "status": "Healthy",
  "timestamp": "2025-12-18T14:30:00.000Z"
}
```

#### Response Fields
- `status` (string): Always `Healthy` if service responds
- `timestamp` (string): Current UTC timestamp (ISO 8601 format)

#### HTTP Status Codes
- `200 OK`: Service process is alive

#### Use Cases
- **Kubernetes liveness probe**: Detects deadlocked or crashed pods
- **Docker health checks**: Basic container health monitoring
- **Simple uptime monitoring**: Quick availability check

#### Kubernetes Configuration Example
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 5201
  initialDelaySeconds: 10
  periodSeconds: 30
  timeoutSeconds: 5
  failureThreshold: 3
```

---

### 3. GET /health/ready

Readiness probe indicating the service is ready to accept traffic.

#### Purpose
- Checks if service dependencies are healthy
- Verifies database connectivity
- Ensures service can handle requests
- Used by Kubernetes to control traffic routing

#### Request
```http
GET /health/ready HTTP/1.1
Host: localhost:5201
```

#### Response Format (Healthy)
```json
{
  "status": "Healthy",
  "timestamp": "2025-12-18T14:30:00.000Z"
}
```

#### Response Format (Unhealthy)
```json
{
  "status": "Unhealthy",
  "timestamp": "2025-12-18T14:30:00.000Z"
}
```

#### Response Format (Error)
```json
{
  "status": "Unhealthy",
  "error": "Unable to connect to MySQL database"
}
```

#### Response Fields
- `status` (string): `Healthy`, `Degraded`, or `Unhealthy`
- `timestamp` (string): Current UTC timestamp (ISO 8601 format)
- `error` (string, optional): Error message if health check fails

#### HTTP Status Codes
- `200 OK`: Service is healthy or degraded (ready to accept traffic)
- `503 Service Unavailable`: Service is unhealthy (not ready for traffic)

#### Use Cases
- **Kubernetes readiness probe**: Controls pod traffic routing
- **Load balancer health checks**: Route traffic only to ready instances
- **Deployment health gates**: Verify new deployments before promoting
- **Zero-downtime deployments**: Ensure new instances are ready

#### Kubernetes Configuration Example
```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 5201
  initialDelaySeconds: 5
  periodSeconds: 10
  timeoutSeconds: 5
  successThreshold: 1
  failureThreshold: 3
```

---

### 4. GET /api/health

Comprehensive application health status with channel-specific metrics.

#### Purpose
- Provides detailed service health including channel status
- Returns service uptime and version information
- Includes notification channel health (Email, Slack, Teams, SignalR)
- Designed for application monitoring and dashboards

#### Request
```http
GET /api/health HTTP/1.1
Host: localhost:5201
```

#### Response Format (Healthy)
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "uptime": "12h 34m",
  "lastCheck": "2025-12-18T14:30:00.000Z",
  "channels": [
    {
      "channel": "Email",
      "status": "healthy",
      "lastDeliveryAt": "2025-12-18T14:25:00.000Z",
      "errorCount24h": 0
    },
    {
      "channel": "Slack",
      "status": "healthy",
      "lastDeliveryAt": "2025-12-18T14:28:00.000Z",
      "errorCount24h": 0
    },
    {
      "channel": "Teams",
      "status": "degraded",
      "lastDeliveryAt": "2025-12-18T12:15:00.000Z",
      "errorCount24h": 3
    },
    {
      "channel": "SignalR",
      "status": "healthy",
      "lastDeliveryAt": "2025-12-18T14:29:45.000Z",
      "errorCount24h": 0
    }
  ]
}
```

#### Response Format (Unhealthy)
```json
{
  "status": "unhealthy",
  "version": "1.0.0",
  "uptime": "0h 5m",
  "lastCheck": "2025-12-18T14:30:00.000Z",
  "channels": []
}
```

#### Response Fields
- `status` (string): Overall service health - `healthy`, `degraded`, or `unhealthy`
- `version` (string): Service assembly version
- `uptime` (string): Time since service started (format: `Xh Ym`)
- `lastCheck` (string): UTC timestamp of health check (ISO 8601 format)
- `channels` (array): Health status for each notification channel
  - `channel` (string): Channel name - `Email`, `Slack`, `Teams`, or `SignalR`
  - `status` (string): Channel-specific status - `healthy`, `degraded`, or `unhealthy`
  - `lastDeliveryAt` (string|null): UTC timestamp of last successful delivery (ISO 8601)
  - `errorCount24h` (number): Number of errors in the last 24 hours

#### HTTP Status Codes
- `200 OK`: Service is healthy or degraded
- `503 Service Unavailable`: Service is unhealthy

#### Channel Status Interpretation

**Healthy**: Channel is operating normally
- Error count is low
- Recent successful deliveries
- All dependencies available

**Degraded**: Channel has issues but may still function
- Elevated error count (3+ in 24h)
- Some delivery failures
- Partial functionality available

**Unhealthy**: Channel is not operational
- High error count or complete failure
- No recent successful deliveries
- Critical dependencies unavailable

#### Use Cases
- **Application dashboards**: Display service health and metrics
- **Monitoring alerts**: Trigger alerts based on channel health
- **SLA tracking**: Monitor uptime and availability
- **Operational insights**: Understand which channels are experiencing issues
- **Trend analysis**: Track error counts and delivery patterns

---

## Health Status Hierarchy

The service uses a three-tier health status model:

### Healthy
- All checks passing
- All channels operational
- Database connectivity confirmed
- Ready to accept traffic

### Degraded
- Service is operational but with reduced functionality
- One or more channels experiencing issues
- Non-critical failures detected
- Service still accepts traffic

### Unhealthy
- Critical failures detected
- Database connectivity lost
- Service cannot function properly
- Should not receive traffic

---

## Monitoring Integration

### Prometheus

Use the `/health` endpoint with a custom exporter or configure Prometheus to scrape the endpoint:

```yaml
scrape_configs:
  - job_name: 'notification-service'
    metrics_path: '/health'
    static_configs:
      - targets: ['localhost:5201']
```

### Kubernetes Probes

Complete pod configuration with both probes:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: notification-service
spec:
  containers:
  - name: notification-service
    image: notification-service:latest
    ports:
    - containerPort: 5201
    livenessProbe:
      httpGet:
        path: /health/live
        port: 5201
      initialDelaySeconds: 10
      periodSeconds: 30
      timeoutSeconds: 5
      failureThreshold: 3
    readinessProbe:
      httpGet:
        path: /health/ready
        port: 5201
      initialDelaySeconds: 5
      periodSeconds: 10
      timeoutSeconds: 5
      successThreshold: 1
      failureThreshold: 3
```

### Load Balancer Health Checks

Configure your load balancer to use `/health/ready`:

**AWS Application Load Balancer**:
- Health check path: `/health/ready`
- Success codes: `200`
- Interval: 10 seconds
- Timeout: 5 seconds
- Healthy threshold: 2
- Unhealthy threshold: 3

**Azure Load Balancer**:
- Protocol: HTTP
- Port: 5201
- Path: `/health/ready`
- Interval: 10 seconds

**NGINX**:
```nginx
upstream notification_service {
    server localhost:5201 max_fails=3 fail_timeout=30s;
    check interval=10000 rise=2 fall=3 timeout=5000 type=http;
    check_http_send "GET /health/ready HTTP/1.0\r\n\r\n";
    check_http_expect_alive http_2xx;
}
```

---

## Troubleshooting

### Endpoint Returns 503

**Possible Causes**:
1. Database connection failure
2. Critical health check failing
3. Service initialization incomplete

**Resolution**:
1. Check `/health` endpoint for detailed component status
2. Verify database connectivity
3. Review application logs for errors
4. Ensure all required environment variables are set

### Channel Shows Degraded Status

**Possible Causes**:
1. Elevated error count in last 24 hours
2. Intermittent delivery failures
3. Third-party service issues (Slack API, Teams webhook)

**Resolution**:
1. Check `errorCount24h` in `/api/health` response
2. Review notification logs for specific errors
3. Verify third-party service credentials
4. Test channel connectivity

### Liveness Probe Failing

**Possible Causes**:
1. Service process crashed or deadlocked
2. Network connectivity issues
3. Resource exhaustion (CPU, memory)

**Resolution**:
1. Check if service is running: `systemctl status notification-service`
2. Review system resource usage
3. Check for deadlocks in application logs
4. Verify network configuration

### Readiness Probe Failing but Service Running

**Possible Causes**:
1. Database temporarily unavailable
2. Service warming up after restart
3. Health check timeout too short

**Resolution**:
1. Increase `initialDelaySeconds` in readiness probe
2. Check database connection string
3. Verify database server is accessible
4. Review health check logs

---

## Best Practices

### Monitoring Strategy

1. **Use /health/live for container orchestration**: Detects crashed processes
2. **Use /health/ready for traffic routing**: Ensures service can handle requests
3. **Use /api/health for application monitoring**: Track channel health and metrics
4. **Use /health for detailed diagnostics**: Component-level troubleshooting

### Alert Configuration

**Critical Alerts** (Immediate action required):
- Liveness probe failures → Service restart needed
- Unhealthy status on `/api/health` → Service outage

**Warning Alerts** (Investigation required):
- Degraded status on `/api/health` → Channel issues
- Elevated error count on channels → Potential delivery problems
- Readiness probe intermittent failures → Performance issues

### Performance Considerations

- **Liveness probe**: No performance impact (instant response)
- **Readiness probe**: Minimal impact (quick health check query)
- **Detailed health checks**: May include database queries (< 100ms typically)
- **Recommended probe intervals**:
  - Liveness: 30 seconds
  - Readiness: 10 seconds

---

## Authentication

**All health endpoints are publicly accessible and require no authentication.**

This design allows:
- Container orchestration platforms to access probes
- Load balancers to perform health checks
- Monitoring systems to scrape metrics
- Operations teams to quickly check service status

---

## Additional Resources

- **Service Documentation**: See main README for service overview
- **Deployment Guide**: Infrastructure setup and configuration
- **API Reference**: Complete API documentation
- **Operations Runbook**: Incident response procedures

---

## Support

For issues or questions:
- Check application logs: `D:\Projects\PlanSourceAutomation-V2\NotificationServices\logs`
- Review health check responses for diagnostic information
- Contact the platform team for infrastructure-related issues
