import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const apiResponseTime = new Trend('api_response_time');

// Test configuration
export const options = {
  stages: [
    // Ramp up
    { duration: '2m', target: 100 },   // 100 users
    { duration: '2m', target: 500 },   // 500 users
    { duration: '2m', target: 1000 },  // 1000 users
    { duration: '5m', target: 5000 },  // 5000 users - PEAK
    // Ramp down
    { duration: '2m', target: 1000 },
    { duration: '2m', target: 500 },
    { duration: '2m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests under 500ms
    http_req_failed: ['rate<0.01'],   // Less than 1% errors
    errors: ['rate<0.05'],            // Custom error rate under 5%
  },
};

const BASE_URL = 'https://localhost:5000';
const AUTH_TOKEN = 'your-jwt-token-here'; // Replace with actual token

export function setup() {
  console.log('Starting load test...');
  return { startTime: new Date().toISOString() };
}

export default function () {
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${AUTH_TOKEN}`,
    },
  };

  group('Document Operations', () => {
    // GET - Document list
    const listResponse = http.get(`${BASE_URL}/api/v2/documents?page=1`, params);
    check(listResponse, {
      'List status is 200': (r) => r.status === 200,
      'List response time < 500ms': (r) => r.timings.duration < 500,
    });
    errorRate.add(listResponse.status !== 200);
    apiResponseTime.add(listResponse.timings.duration);

    sleep(1);

    // GET - Single document
    const docResponse = http.get(`${BASE_URL}/api/v2/documents/1`, params);
    check(docResponse, {
      'Get document status is 200': (r) => r.status === 200,
    });
    errorRate.add(docResponse.status !== 200);

    sleep(1);

    // POST - Search documents
    const searchPayload = JSON.stringify({
      searchTerm: 'report',
      page: 1,
      pageSize: 20,
    });
    const searchResponse = http.post(
      `${BASE_URL}/api/v2/documents/search`,
      searchPayload,
      params
    );
    check(searchResponse, {
      'Search status is 200': (r) => r.status === 200,
      'Search response time < 1000ms': (r) => r.timings.duration < 1000,
    });
    errorRate.add(searchResponse.status !== 200);
  });

  sleep(2);

  group('Task Operations', () => {
    // GET - Tasks
    const tasksResponse = http.get(`${BASE_URL}/api/tasks/my-tasks`, params);
    check(tasksResponse, {
      'Tasks status is 200': (r) => r.status === 200,
    });
    errorRate.add(tasksResponse.status !== 200);

    sleep(1);
  });

  sleep(3);
}

export function teardown(data) {
  console.log('Load test completed. Start time:', data.startTime);
  console.log('End time:', new Date().toISOString());
}
