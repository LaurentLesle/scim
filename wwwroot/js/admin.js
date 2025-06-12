// Authentication helper
function getAuthHeaders() {
    const adminToken = sessionStorage.getItem('adminToken');
    return adminToken ? { 'X-Admin-Token': adminToken } : {};
}

// Check authentication on page load
function checkAuth() {
    const adminToken = sessionStorage.getItem('adminToken');
    if (!adminToken) {
        window.location.href = '/admin-login.html';
        return false;
    }
    return true;
}

// Logout function
async function logout() {
    try {
        await fetch('/admin/logout', { 
            method: 'POST',
            headers: getAuthHeaders()
        });
        sessionStorage.removeItem('adminToken');
        window.location.href = '/admin-login.html';
    } catch (error) {
        console.error('Logout error:', error);
        sessionStorage.removeItem('adminToken');
        window.location.href = '/admin-login.html';
    }
}

// Copy to clipboard function
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        // Could add a toast notification here
        console.log('Copied to clipboard');
    });
}

// Show customers page
async function showCustomers() {
    if (!checkAuth()) return;

    const contentDiv = document.getElementById('content');
    contentDiv.innerHTML = '<div class="loading">Loading customers...</div>';

    try {
        const response = await fetch('/admin/api/customers', {
            headers: getAuthHeaders()
        });
        
        if (response.status === 401) {
            window.location.href = '/admin-login.html';
            return;
        }
        
        const customers = await response.json();
        
        let html = '<h2>👥 Customers & Token Generation</h2>';
        
        if (customers.length === 0) {
            html += '<p>No customers found. Create a customer first using the customers API endpoint.</p>';
        } else {
            customers.forEach(customer => {
                const info = customer.tokenGenerationInfo;
                html += `
                    <div class="customer-card">
                        <div class="customer-header">${customer.name}</div>
                        <div class="customer-info">
                            <strong>Customer ID:</strong> ${customer.id}<br>
                            <strong>Tenant ID:</strong> ${customer.tenantId}<br>
                            <strong>Status:</strong> ${customer.isActive ? '✅ Active' : '❌ Inactive'}<br>
                            <strong>Created:</strong> ${new Date(customer.created).toLocaleString()}
                        </div>
                        
                        <div class="token-section">
                            <h4>🔑 Token Generation for ${customer.tenantId}</h4>
                            
                            <p><strong>1. Generate Bearer Token:</strong></p>
                            <div class="code-block">
                                ${info.exampleCurl}
                                <button class="copy-btn" onclick="copyToClipboard(\`${info.exampleCurl}\`)">📋 Copy</button>
                            </div>
                            
                            <p><strong>2. SCIM API Endpoints (use the token above):</strong></p>
                            <div class="code-block">
                                <strong>Users:</strong> ${info.scimEndpoints.users}<br>
                                <strong>Groups:</strong> ${info.scimEndpoints.groups}
                            </div>
                            
                            <p><strong>3. Example: Create a User</strong></p>
                            <div class="code-block">
curl -X POST "${info.scimEndpoints.users}" \\
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \\
  -H "Content-Type: application/scim+json" \\
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "john.doe@example.com",
    "displayName": "John Doe",
    "active": true,
    "emails": [{"value": "john.doe@example.com", "type": "work", "primary": true}],
    "name": {"givenName": "John", "familyName": "Doe"}
  }'
                                <button class="copy-btn" onclick="copyToClipboard(\`curl -X POST '${info.scimEndpoints.users}' -H 'Authorization: Bearer YOUR_TOKEN_HERE' -H 'Content-Type: application/scim+json' -d '{&quot;schemas&quot;: [&quot;urn:ietf:params:scim:schemas:core:2.0:User&quot;], &quot;userName&quot;: &quot;john.doe@example.com&quot;, &quot;displayName&quot;: &quot;John Doe&quot;, &quot;active&quot;: true, &quot;emails&quot;: [{&quot;value&quot;: &quot;john.doe@example.com&quot;, &quot;type&quot;: &quot;work&quot;, &quot;primary&quot;: true}], &quot;name&quot;: {&quot;givenName&quot;: &quot;John&quot;, &quot;familyName&quot;: &quot;Doe&quot;}}')\`)">📋 Copy</button>
                            </div>
                        </div>
                    </div>
                `;
            });
        }
        
        contentDiv.innerHTML = html;
    } catch (error) {
        contentDiv.innerHTML = `<div class="error">Error loading customers: ${error.message}</div>`;
    }
}

// Initialize page
document.addEventListener('DOMContentLoaded', () => {
    if (checkAuth()) {
        showCustomers();
    }
});
