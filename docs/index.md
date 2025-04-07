---
layout: home
title: Azure DevOps MCP Server
---

## 🚀 Key Features

<div class="my-12">
  <div class="text-center mb-12">
    <p class="text-xl text-gray-600 max-w-3xl mx-auto">A comprehensive Model Context Protocol server that bridges the gap between AI tools and Azure DevOps, providing seamless integration with production-grade reliability.</p>
  </div>
</div>

<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 mb-20">
  <div class="bg-white rounded-xl p-8 shadow-lg hover:shadow-xl transition-all duration-300 border border-gray-100 hover:border-azure-blue/20">
    <div class="text-4xl mb-4">📊</div>
    <h3 class="text-xl font-bold text-gray-900 mb-3">Project Management</h3>
    <p class="text-gray-600 leading-relaxed">Browse projects, teams, and organizational structure with comprehensive access control.</p>
  </div>
  
  <div class="bg-white rounded-xl p-8 shadow-lg hover:shadow-xl transition-all duration-300 border border-gray-100 hover:border-azure-blue/20">
    <div class="text-4xl mb-4">🔄</div>
    <h3 class="text-xl font-bold text-gray-900 mb-3">Repository Operations</h3>
    <p class="text-gray-600 leading-relaxed">View repositories, files, commits, branches, and pull requests with full Git integration.</p>
  </div>
  
  <div class="bg-white rounded-xl p-8 shadow-lg hover:shadow-xl transition-all duration-300 border border-gray-100 hover:border-azure-blue/20">
    <div class="text-4xl mb-4">📋</div>
    <h3 class="text-xl font-bold text-gray-900 mb-3">Work Item Management</h3>
    <p class="text-gray-600 leading-relaxed">Query and manage work items with optional safe write operations and audit logging.</p>
  </div>
  
  <div class="bg-white rounded-xl p-8 shadow-lg hover:shadow-xl transition-all duration-300 border border-gray-100 hover:border-azure-blue/20">
    <div class="text-4xl mb-4">🧪</div>
    <h3 class="text-xl font-bold text-gray-900 mb-3">Test Management</h3>
    <p class="text-gray-600 leading-relaxed">Access test plans, test suites, test runs, and results for comprehensive test oversight.</p>
  </div>
  
  <div class="bg-white rounded-xl p-8 shadow-lg hover:shadow-xl transition-all duration-300 border border-gray-100 hover:border-azure-blue/20">
    <div class="text-4xl mb-4">⚡</div>
    <h3 class="text-xl font-bold text-gray-900 mb-3">Performance Optimized</h3>
    <p class="text-gray-600 leading-relaxed">Built-in caching, performance monitoring, and batch operations for enterprise-scale usage.</p>
  </div>
  
  <div class="bg-white rounded-xl p-8 shadow-lg hover:shadow-xl transition-all duration-300 border border-gray-100 hover:border-azure-blue/20">
    <div class="text-4xl mb-4">🔒</div>
    <h3 class="text-xl font-bold text-gray-900 mb-3">Production Ready</h3>
    <p class="text-gray-600 leading-relaxed">Comprehensive security, error handling, rate limiting, and Azure Key Vault integration.</p>
  </div>
</div>

## 🛠 Available Tools

<div class="text-center mb-12">
  <p class="text-xl text-gray-600 max-w-3xl mx-auto">The server exposes Azure DevOps functionality through specialized tool categories, each designed for specific workflows and use cases.</p>
</div>

<div class="bg-gradient-to-br from-gray-50 to-white rounded-2xl p-10 shadow-lg border border-gray-100 mb-20">
  
  <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
    <div class="text-center">
      <div class="bg-green-50 rounded-full w-16 h-16 flex items-center justify-center mx-auto mb-4">
        <div class="text-2xl">📖</div>
      </div>
      <h3 class="text-lg font-bold text-gray-900 mb-3">Core Operations (Read-Only)</h3>
      <ul class="text-sm text-gray-600 space-y-2 text-left">
        <li><strong>Projects:</strong> List and browse Azure DevOps projects</li>
        <li><strong>Repositories:</strong> Access Git repositories, files, and commit history</li>
        <li><strong>Work Items:</strong> Query work items with advanced filtering</li>
        <li><strong>Build & Test:</strong> Monitor build pipelines and test results</li>
      </ul>
    </div>
    
    <div class="text-center">
      <div class="bg-blue-50 rounded-full w-16 h-16 flex items-center justify-center mx-auto mb-4">
        <div class="text-2xl">✏️</div>
      </div>
      <h3 class="text-lg font-bold text-gray-900 mb-3">Safe Write Operations (Opt-In)</h3>
      <ul class="text-sm text-gray-600 space-y-2 text-left">
        <li><strong>Pull Request Comments:</strong> Add comments to pull requests</li>
        <li><strong>Draft Pull Requests:</strong> Create draft PRs for review</li>
        <li><strong>Work Item Tags:</strong> Manage work item tags and metadata</li>
      </ul>
    </div>
    
    <div class="text-center">
      <div class="bg-purple-50 rounded-full w-16 h-16 flex items-center justify-center mx-auto mb-4">
        <div class="text-2xl">⚡</div>
      </div>
      <h3 class="text-lg font-bold text-gray-900 mb-3">Batch & Performance</h3>
      <ul class="text-sm text-gray-600 space-y-2 text-left">
        <li><strong>Bulk Operations:</strong> Efficient parallel processing of multiple items</li>
        <li><strong>Performance Monitoring:</strong> Real-time metrics and system optimization</li>
        <li><strong>Cache Management:</strong> Intelligent caching with warming strategies</li>
      </ul>
    </div>
  </div>
</div>

## 🎯 Quick Start

<div class="bg-gradient-to-r from-blue-50 to-indigo-50 rounded-xl p-8 mb-16">
  <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
    <div class="text-center">
      <div class="bg-azure-blue text-white rounded-full w-12 h-12 flex items-center justify-center mx-auto mb-4 text-xl font-bold">1</div>
      <h3 class="text-lg font-bold text-gray-900 mb-3">Prerequisites</h3>
      <ul class="text-sm text-gray-600 space-y-2">
        <li><a href="https://dotnet.microsoft.com/download/dotnet/9.0" class="text-azure-blue hover:underline">.NET 9 SDK</a> or <a href="https://www.docker.com/products/docker-desktop/" class="text-azure-blue hover:underline">Docker</a></li>
        <li>Azure DevOps account with <a href="{{ '/getting-started/#generating-pat' | relative_url }}" class="text-azure-blue hover:underline">Personal Access Token</a></li>
      </ul>
    </div>
    
    <div class="text-center">
      <div class="bg-azure-blue text-white rounded-full w-12 h-12 flex items-center justify-center mx-auto mb-4 text-xl font-bold">2</div>
      <h3 class="text-lg font-bold text-gray-900 mb-3">Run with Docker</h3>
      <div class="bg-gray-900 text-green-400 p-4 rounded-lg text-xs text-left font-mono">
        <div class="mb-2"># Pull latest image</div>
        <div class="mb-2">docker pull ghcr.io/decriptor/azuredevops-mcp</div>
        <div class="mb-2"># Run with config</div>
        <div>docker run -it \\</div>
        <div>&nbsp;&nbsp;-e AzureDevOps__OrganizationUrl="..." \\</div>
        <div>&nbsp;&nbsp;ghcr.io/decriptor/azuredevops-mcp</div>
      </div>
    </div>
    
    <div class="text-center">
      <div class="bg-azure-blue text-white rounded-full w-12 h-12 flex items-center justify-center mx-auto mb-4 text-xl font-bold">3</div>
      <h3 class="text-lg font-bold text-gray-900 mb-3">Claude Integration</h3>
      <div class="bg-gray-900 text-blue-300 p-4 rounded-lg text-xs text-left font-mono">
        <div>{</div>
        <div>&nbsp;&nbsp;"mcpServers": {</div>
        <div>&nbsp;&nbsp;&nbsp;&nbsp;"azure-devops": {</div>
        <div>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"command": "docker",</div>
        <div>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"args": [...]</div>
        <div>&nbsp;&nbsp;&nbsp;&nbsp;}</div>
        <div>&nbsp;&nbsp;}</div>
        <div>}</div>
      </div>
    </div>
  </div>
</div>

## 🏗 Architecture Highlights

<div class="grid grid-cols-1 md:grid-cols-2 gap-8 mb-16">
  <div class="bg-white rounded-xl p-6 shadow-lg border border-gray-100">
    <h3 class="text-lg font-bold text-gray-900 mb-4 flex items-center">
      <div class="bg-green-100 text-green-600 rounded-lg p-2 mr-3">🏛️</div>
      Clean Architecture
    </h3>
    <p class="text-gray-600 mb-3">Modular services with clear separation of concerns</p>
    <ul class="text-sm text-gray-500 space-y-1">
      <li>• Performance-first design</li>
      <li>• Memory optimization</li>
      <li>• Intelligent caching</li>
    </ul>
  </div>
  
  <div class="bg-white rounded-xl p-6 shadow-lg border border-gray-100">
    <h3 class="text-lg font-bold text-gray-900 mb-4 flex items-center">
      <div class="bg-blue-100 text-blue-600 rounded-lg p-2 mr-3">🔒</div>
      Security Focused
    </h3>
    <p class="text-gray-600 mb-3">Comprehensive authorization and audit logging</p>
    <ul class="text-sm text-gray-500 space-y-1">
      <li>• Role-based access control</li>
      <li>• Azure Key Vault integration</li>
      <li>• Tamper-proof audit logs</li>
    </ul>
  </div>
  
  <div class="bg-white rounded-xl p-6 shadow-lg border border-gray-100">
    <h3 class="text-lg font-bold text-gray-900 mb-4 flex items-center">
      <div class="bg-purple-100 text-purple-600 rounded-lg p-2 mr-3">📊</div>
      Monitoring Ready
    </h3>
    <p class="text-gray-600 mb-3">Sentry integration and structured logging</p>
    <ul class="text-sm text-gray-500 space-y-1">
      <li>• Real-time performance metrics</li>
      <li>• Customizable thresholds</li>
      <li>• Circuit breaker patterns</li>
    </ul>
  </div>
  
  <div class="bg-white rounded-xl p-6 shadow-lg border border-gray-100">
    <h3 class="text-lg font-bold text-gray-900 mb-4 flex items-center">
      <div class="bg-orange-100 text-orange-600 rounded-lg p-2 mr-3">🐳</div>
      Container Native
    </h3>
    <p class="text-gray-600 mb-3">Optimized Docker images with security best practices</p>
    <ul class="text-sm text-gray-500 space-y-1">
      <li>• Multi-platform builds</li>
      <li>• Non-root containers</li>
      <li>• Minimal attack surface</li>
    </ul>
  </div>
</div>

## 🤝 Contributing

<div class="bg-gradient-to-r from-green-50 to-blue-50 rounded-xl p-8 mb-16">
  <div class="text-center mb-8">
    <h3 class="text-2xl font-bold text-gray-900 mb-4">Join Our Community</h3>
    <p class="text-lg text-gray-600 max-w-2xl mx-auto">We welcome contributions! This is an open-source project built for the community.</p>
  </div>
  
  <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
    <a href="https://github.com/decriptor/AzureDevOps.MCP/issues" class="bg-white rounded-lg p-4 text-center hover:shadow-lg transition-all duration-300 border border-gray-100">
      <div class="text-2xl mb-2">🐛</div>
      <div class="text-sm font-semibold text-gray-900">Report Issues</div>
    </a>
    <a href="https://github.com/decriptor/AzureDevOps.MCP/issues/new?template=feature_request.md" class="bg-white rounded-lg p-4 text-center hover:shadow-lg transition-all duration-300 border border-gray-100">
      <div class="text-2xl mb-2">💡</div>
      <div class="text-sm font-semibold text-gray-900">Request Features</div>
    </a>
    <a href="https://github.com/decriptor/AzureDevOps.MCP/issues" class="bg-white rounded-lg p-4 text-center hover:shadow-lg transition-all duration-300 border border-gray-100">
      <div class="text-2xl mb-2">📖</div>
      <div class="text-sm font-semibold text-gray-900">Improve Docs</div>
    </a>
    <a href="https://github.com/decriptor/AzureDevOps.MCP/pulls" class="bg-white rounded-lg p-4 text-center hover:shadow-lg transition-all duration-300 border border-gray-100">
      <div class="text-2xl mb-2">🔀</div>
      <div class="text-sm font-semibold text-gray-900">Submit PRs</div>
    </a>
  </div>
</div>

## 📚 Learn More

<div class="text-center mb-16">
  <div class="flex flex-col sm:flex-row gap-4 justify-center items-center">
    <a href="{{ '/getting-started/' | relative_url }}" class="bg-azure-blue text-white px-8 py-4 rounded-lg font-semibold text-lg hover:bg-azure-dark transition-all duration-300 transform hover:scale-105 shadow-lg">
      Get Started
    </a>
    <a href="{{ '/api-reference/' | relative_url }}" class="border-2 border-azure-blue text-azure-blue px-8 py-4 rounded-lg font-semibold text-lg hover:bg-azure-blue hover:text-white transition-all duration-300 transform hover:scale-105">
      API Reference
    </a>
    <a href="{{ '/examples/' | relative_url }}" class="border-2 border-gray-300 text-gray-700 px-8 py-4 rounded-lg font-semibold text-lg hover:border-gray-400 hover:bg-gray-50 transition-all duration-300 transform hover:scale-105">
      Examples
    </a>
    <a href="https://github.com/decriptor/AzureDevOps.MCP" class="bg-gray-900 text-white px-8 py-4 rounded-lg font-semibold text-lg hover:bg-gray-800 transition-all duration-300 transform hover:scale-105 shadow-lg flex items-center">
      <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 0C4.477 0 0 4.484 0 10.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0110 4.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.203 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.942.359.31.678.921.678 1.856 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0020 10.017C20 4.484 15.522 0 10 0z" clip-rule="evenodd"></path></svg>
      GitHub
    </a>
  </div>
</div>

---

<div class="bg-white rounded-xl p-8 shadow-lg border border-gray-100">
  <div class="grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
    <div>
      <div class="text-3xl font-bold text-azure-blue mb-2">153</div>
      <div class="text-sm text-gray-600">Passing Tests</div>
    </div>
    <div>
      <div class="text-3xl font-bold text-azure-blue mb-2">5</div>
      <div class="text-sm text-gray-600">Tool Categories</div>
    </div>
    <div>
      <div class="text-3xl font-bold text-azure-blue mb-2">20+</div>
      <div class="text-sm text-gray-600">Available Operations</div>
    </div>
    <div>
      <div class="text-3xl font-bold text-azure-blue mb-2">.NET 9</div>
      <div class="text-sm text-gray-600">Powered</div>
    </div>
  </div>
</div>