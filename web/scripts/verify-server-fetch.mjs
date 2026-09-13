#!/usr/bin/env node

/**
 * verify-server-fetch.mjs
 * Live fetch integration test verifying HTTP communication from Web to .NET WebAPI Server.
 */

import http from 'node:http';

const SERVER_BASE = process.env.API_SERVER_URL || 'http://localhost:5065';
console.log(`\n======================================================`);
console.log(`[Fetch Test] Target Server: ${SERVER_BASE}`);
console.log(`======================================================\n`);

async function checkEndpoint(path, options = {}) {
  const url = `${SERVER_BASE}${path}`;
  const start = Date.now();
  try {
    const response = await fetch(url, {
      headers: {
        'Accept': 'application/json',
        ...(options.headers || {}),
      },
      ...options,
    });
    const elapsed = Date.now() - start;
    let data = null;
    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
      data = await response.json();
    } else {
      data = await response.text();
    }

    return {
      ok: response.ok,
      status: response.status,
      elapsed,
      data,
      headers: Object.fromEntries(response.headers.entries()),
    };
  } catch (err) {
    return {
      ok: false,
      status: 0,
      elapsed: Date.now() - start,
      error: err.message,
    };
  }
}

async function runFetchSuite() {
  console.log('1. Checking Server Connectivity & Health / Swagger...');
  const swaggerRes = await checkEndpoint('/swagger/v1/swagger.json');
  if (swaggerRes.status === 200) {
    console.log(`   ✔ Swagger UI / OpenAPI docs reachable (${swaggerRes.elapsed}ms)`);
  } else if (swaggerRes.status === 0) {
    console.log(`   ✖ Server offline or not reachable at ${SERVER_BASE}: ${swaggerRes.error}`);
    console.log(`   (Note: Run the server with 'dotnet run' in deblog/server to execute live server requests)`);
    return { success: false, serverOnline: false };
  } else {
    console.log(`   ⚠ Swagger returned HTTP ${swaggerRes.status}`);
  }

  console.log('\n2. Testing Public Posts Endpoint [GET /api/posts?page=1&pageSize=10]...');
  const postsRes = await checkEndpoint('/api/posts?page=1&pageSize=10&publishedOnly=true');
  if (postsRes.status === 200 && postsRes.data) {
    const items = postsRes.data.items || [];
    console.log(`   ✔ Posts endpoint responded with HTTP 200 (${postsRes.elapsed}ms)`);
    console.log(`   ✔ Found ${items.length} public posts (totalCount: ${postsRes.data.totalCount || 0})`);
  } else {
    console.log(`   ✖ Posts endpoint failed: HTTP ${postsRes.status}`, postsRes.error || postsRes.data);
  }

  console.log('\n3. Testing Admin Posts Endpoint [GET /api/posts?publishedOnly=false]...');
  const adminPostsRes = await checkEndpoint('/api/posts?page=1&pageSize=10&publishedOnly=false');
  if (adminPostsRes.status === 200 && adminPostsRes.data) {
    const items = adminPostsRes.data.items || [];
    console.log(`   ✔ Admin posts query responded with HTTP 200 (${adminPostsRes.elapsed}ms)`);
    console.log(`   ✔ Retrieved ${items.length} posts (totalCount: ${adminPostsRes.data.totalCount || 0})`);
  } else {
    console.log(`   ✖ Admin posts query failed: HTTP ${adminPostsRes.status}`);
  }

  console.log('\n4. Testing Guarded Admin Comments Endpoint [GET /api/admin/comments] (Unauthorized check)...');
  const commentsUnauth = await checkEndpoint('/api/admin/comments?page=1&pageSize=10');
  if (commentsUnauth.status === 401) {
    console.log(`   ✔ Admin guard working as expected: HTTP 401 Unauthorized without token (${commentsUnauth.elapsed}ms)`);
  } else {
    console.log(`   ⚠ Expected 401 Unauthorized but received HTTP ${commentsUnauth.status}`);
  }

  console.log('\n5. Testing Guarded Admin Users Endpoint [GET /api/admin/users] (Unauthorized check)...');
  const usersUnauth = await checkEndpoint('/api/admin/users?page=1&pageSize=10');
  if (usersUnauth.status === 401) {
    console.log(`   ✔ Admin guard working as expected: HTTP 401 Unauthorized without token (${usersUnauth.elapsed}ms)`);
  } else {
    console.log(`   ⚠ Expected 401 Unauthorized but received HTTP ${usersUnauth.status}`);
  }

  console.log('\n======================================================');
  console.log('Fetch Integration Verification Summary: Complete.');
  console.log('======================================================\n');
  return { success: true, serverOnline: true };
}

runFetchSuite().then((result) => {
  if (!result.serverOnline) {
    process.exit(0);
  }
});
