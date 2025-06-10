# Security Policy

## Supported Versions

We actively support the following versions of Azure DevOps MCP with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security vulnerability in Azure DevOps MCP, please report it to us as follows:

### How to Report

1. **DO NOT** create a public GitHub issue for security vulnerabilities
2. Send an email to: [security@your-domain.com] or use GitHub's private vulnerability reporting
3. Use GitHub's security advisory feature: Go to the Security tab → Report a vulnerability

### What to Include

Please include the following information in your report:

- Description of the vulnerability
- Steps to reproduce the vulnerability
- Potential impact of the vulnerability
- Any suggested fixes or mitigations
- Your contact information for follow-up questions

### Response Timeline

- **Initial Response**: We will acknowledge receipt of your vulnerability report within 48 hours
- **Assessment**: We will assess the vulnerability within 5 business days
- **Resolution**: We aim to resolve critical vulnerabilities within 30 days
- **Disclosure**: We will coordinate with you on the disclosure timeline

### Security Measures

This project implements several security measures:

#### Authentication & Authorization
- Azure DevOps Personal Access Token (PAT) authentication
- Configurable authorization levels per operation
- IP whitelisting support for production deployments
- API key authentication for additional security layers

#### Data Protection
- Sensitive data filtering in logs
- Azure Key Vault integration for secrets management
- No storage of credentials in application memory beyond necessary caching
- Automatic secret rotation support

#### Network Security
- HTTPS enforcement in production
- CORS configuration for web access
- Request rate limiting to prevent abuse
- Circuit breaker pattern for resilience

#### Monitoring & Auditing
- Comprehensive audit logging for all operations
- Security event monitoring with Sentry integration
- Performance monitoring to detect anomalies
- Health checks for security components

#### Code Security
- Regular dependency updates via Dependabot
- Static code analysis with CodeQL
- Container vulnerability scanning with Trivy
- Secret scanning to prevent credential exposure

### Security Configuration

#### Production Deployment
```json
{
  "Security": {
    "EnableKeyVault": true,
    "KeyVaultUrl": "https://your-keyvault.vault.azure.net/",
    "EnableApiKeyAuth": true,
    "EnableIpWhitelist": true,
    "AllowedIpRanges": ["10.0.0.0/8", "192.168.0.0/16"],
    "EnableRequestSigning": true
  },
  "Logging": {
    "EnableSensitiveDataFiltering": true,
    "SensitiveDataPatterns": [
      "pat_[a-zA-Z0-9]{52}",
      "Authorization:\\s*Bearer\\s+[a-zA-Z0-9\\-._~+/]+=*"
    ]
  },
  "RateLimiting": {
    "EnableRateLimiting": true,
    "RequestsPerMinute": 60,
    "RequestsPerHour": 1000
  }
}
```

#### Environment Variables
Ensure these environment variables are securely configured:

- `AZDO_ORGANIZATIONURL`: Azure DevOps organization URL
- `AZDO_PERSONALACCESSTOKEN`: Azure DevOps PAT (store in secrets)
- `AZURE_KEY_VAULT_URL`: Azure Key Vault URL for secrets
- `API_KEY_HASH`: Hashed API keys for authentication

### Security Best Practices

#### For Developers
1. Never commit secrets or credentials to the repository
2. Use the provided secret management system
3. Follow secure coding practices for authentication flows
4. Validate all inputs and sanitize outputs
5. Use HTTPS for all external communications

#### For Operators
1. Regularly rotate Azure DevOps PATs
2. Monitor access logs for suspicious activity
3. Keep the application and dependencies updated
4. Use Azure Key Vault for production secrets
5. Configure IP whitelisting for production deployments
6. Enable comprehensive logging and monitoring

#### For Users
1. Use least-privilege Azure DevOps PATs
2. Regularly review and rotate access tokens
3. Monitor Azure DevOps audit logs for unexpected activity
4. Report any suspicious behavior immediately

### Compliance

This project adheres to:
- OWASP Top 10 security guidelines
- Microsoft Security Development Lifecycle (SDL)
- Azure security best practices
- Industry standard authentication protocols

### Security Updates

Security updates will be:
- Released as soon as possible for critical vulnerabilities
- Clearly marked in release notes
- Backward compatible when possible
- Documented with migration guides when breaking changes are necessary

### Contact

For security-related questions or concerns:
- Email: [security@your-domain.com]
- GitHub Security Advisories: Use the Security tab
- General questions: Create a GitHub issue (non-security related only)

Thank you for helping keep Azure DevOps MCP secure!