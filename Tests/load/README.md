# Load Testing Guide

## Requirements

1. Install K6: https://k6.io/docs/get-started/installation/
2. Windows: `choco install k6`
3. Linux: `sudo apt-get install k6`
4. Mac: `brew install k6`

## Running Tests

### 1. Smoke Test (Quick validation)
```bash
k6 run --duration 30s --vus 10 load-test.js
```

### 2. Load Test (500 users)
```bash
k6 run --duration 5m --vus 500 load-test.js
```

### 3. Stress Test (5000 users)
```bash
k6 run load-test.js  # Uses options defined in script
```

### 4. With InfluxDB + Grafana (Real-time monitoring)
```bash
k6 run --out influxdb=http://localhost:8086/k6 load-test.js
```

## Interpreting Results

### Success Criteria for 5000 Users

| Metric | Target | Acceptable |
|--------|--------|------------|
| p95 Response Time | < 500ms | < 1000ms |
| Error Rate | < 1% | < 5% |
| Throughput | > 1000 req/s | > 500 req/s |
| CPU Usage | < 70% | < 85% |
| Memory Usage | < 80% | < 90% |

### Example Output
```
✓ http_req_duration..............: avg=234ms  min=10ms   med=180ms  max=2s     p(90)=450ms  p(95)=680ms
✓ http_req_failed................: 0.23%  ✓ 1777 / ✗ 4
✓ iterations.....................: 5000   83.33/s
```

## Performance Tuning

### If p95 > 1000ms:
1. Enable SQL Server Read Replicas
2. Increase Cache TTL
3. Add database indexes
4. Scale horizontally (more API instances)

### If Error Rate > 5%:
1. Check database connection pool
2. Increase timeout values
3. Enable Circuit Breaker
4. Check for memory leaks

### If CPU > 85%:
1. Scale up VM (more CPU cores)
2. Optimize hot paths in code
3. Enable response compression
4. Use CDN for static files

## Monitoring During Test

### Windows Performance Monitor (PerfMon)
- Processor\% Processor Time
- Memory\Available MBytes
- SQL Server\Batch Requests/sec
- .NET CLR Memory\% Time in GC

### Application Insights
- Live Metrics Stream
- Failed Requests
- Server Response Time
- Dependency Duration

## Continuous Load Testing

### Azure DevOps Pipeline
```yaml
- task: k6-load-test@0
  inputs:
    script: 'tests/load/load-test.js'
    args: '--out influxdb=http://monitoring:8086/k6'
    failOnErrors: true
```

### GitHub Actions
```yaml
- name: Run k6 Load Test
  uses: grafana/k6-action@v0.3.0
  with:
    filename: tests/load/load-test.js
    flags: --duration 5m --vus 5000
```
