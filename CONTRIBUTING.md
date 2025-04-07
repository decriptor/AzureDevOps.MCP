# Contributing to Azure DevOps MCP Server

Thank you for your interest in contributing to the Azure DevOps MCP Server! We welcome contributions from the community and are grateful for your help in making this project better.

## Code of Conduct

This project adheres to a [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/decriptor/AzureDevOps.MCP.git
   cd AzureDevOps.MCP
   ```
3. **Create a topic branch** for your contribution:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## 🚀 Quick Start

### Option 1: Development Containers (Recommended)
The easiest way to get started is using development containers:

**VS Code + Dev Containers:**
1. Install [VS Code](https://code.visualstudio.com/) and the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
2. Open the repository in VS Code
3. Click "Reopen in Container" when prompted
4. Everything is automatically configured!

**GitHub Codespaces:**
1. Go to the repository on GitHub
2. Click "Code" → "Codespaces" → "Create codespace on main"
3. Start coding immediately in your browser

### Option 2: Local Development
1. **Setup Development Environment**
   ```bash
   # Manual setup (any platform)
   dotnet restore
   dotnet build
   dotnet test
   ```

2. **Configure for Development**
   - Copy `.env.example` to `.env.development`
   - Add your Azure DevOps organization URL and PAT
   - Enable any write operations you want to test

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# Test coverage is handled by CI/CD pipeline
```

### Test Categories
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test interactions between components
- **Performance Tests**: Validate performance characteristics

### Writing Tests
- Use MSTest framework with FluentAssertions
- Mock external dependencies using Moq
- Test both success and failure scenarios
- Include performance-critical path testing

## 🏗️ Architecture

### Project Structure
```
src/AzureDevOps.MCP/
├── Configuration/     # Configuration models
├── Services/         # Business logic and Azure DevOps integration
├── Tools/           # MCP tool implementations
└── Program.cs       # Application entry point

tests/AzureDevOps.MCP.Tests/
├── Services/        # Service layer tests
└── Tools/          # Tool layer tests
```

### Key Components

1. **Services Layer**
   - `IAzureDevOpsService`: Core Azure DevOps operations
   - `CachedAzureDevOpsService`: Performance-optimized wrapper
   - `ICacheService`: Caching abstraction
   - `IPerformanceService`: Performance tracking
   - `IAuditService`: Write operation auditing

2. **Tools Layer**
   - `AzureDevOpsTools`: Read-only MCP tools
   - `SafeWriteTools`: Opt-in write operations
   - `BatchTools`: Batch/parallel operations
   - `PerformanceTools`: Performance monitoring

3. **Configuration**
   - `AzureDevOpsConfiguration`: Main configuration model
   - `SafeWriteOperations`: Available write operations

## 📝 Coding Guidelines

### Code Style
- Use tabs for indentation (configured in .editorconfig)
- Follow C# naming conventions
- Use file-scoped namespaces
- Prefer explicit types over `var` for clarity
- Add XML documentation for public APIs

### Safe Write Operations
When adding new write operations:

1. **Add to Configuration**
   ```csharp
   public const string NewOperation = "NewOperation";
   ```

2. **Implement in Service**
   ```csharp
   Task<T> NewOperationAsync(parameters...);
   ```

3. **Add MCP Tool**
   ```csharp
   [McpServerTool(Name = "new_operation", ReadOnly = false)]
   public async Task<object> NewOperationAsync(parameters..., bool confirm = false)
   ```

4. **Include Safety Features**
   - Operation enablement check
   - Confirmation requirement
   - Audit logging
   - Cache invalidation
   - Error handling

5. **Write Tests**
   - Test confirmation workflow
   - Test operation disabled scenario
   - Test success and failure cases
   - Test audit logging

### Performance Considerations
- Use caching for frequently accessed data
- Implement batch operations for multiple items
- Track performance metrics
- Invalidate cache appropriately after writes

## 🔒 Security Guidelines

### Write Operations
- **Opt-in by default**: All write operations must be explicitly enabled
- **Confirmation required**: Show preview and require confirmation
- **Audit everything**: Log all write attempts with details
- **Minimize scope**: Only implement "safe" write operations

### Data Protection
- Never log sensitive data (PATs, passwords)
- Hash tokens for audit trails
- Use secure defaults in configuration
- Validate all inputs

## 🚀 Performance Best Practices

### Caching Strategy
- Cache frequently accessed, slowly changing data
- Use appropriate expiration times
- Invalidate cache after write operations
- Monitor cache hit rates

### Batch Operations
- Implement parallel processing for multiple items
- Handle partial failures gracefully
- Provide detailed results with error handling
- Limit batch sizes to prevent timeouts

### Monitoring
- Track operation durations
- Monitor API call success rates
- Identify slow operations
- Provide performance metrics via MCP tools

## 🔄 Pull Request Process

### Before Submitting
1. **Ensure tests pass**:
   ```bash
   dotnet test AzureDevOps.MCP.slnx --configuration Release
   ```
2. **Update documentation** if needed
3. **Add/update tests** for your changes

### Submitting a Pull Request
1. **Push your branch** to your fork
2. **Create a pull request** against the `main` branch
3. **Fill out the PR template** completely
4. **Ensure CI passes** (all checks must be green)
5. **Address review feedback** promptly

### PR Requirements
- ✅ All tests pass
- ✅ Code follows project standards
- ✅ Documentation is updated
- ✅ No merge conflicts
- ✅ Security scan passes

## 🐛 Issue Guidelines

### Bug Reports
Include:
- Clear reproduction steps
- Expected vs actual behavior
- Environment details (.NET version, OS)
- Configuration (anonymized)
- Relevant logs or error messages

### Feature Requests
Include:
- Use case description
- Proposed API design
- Security considerations
- Performance impact
- Breaking change assessment

## 📦 Release Process

1. **Update Version**: Update version in `Directory.Build.props`
2. **Update Changelog**: Document changes and breaking changes
3. **Test**: Run full test suite including performance tests
4. **Documentation**: Update README.md and API documentation
5. **Docker**: Test Docker build and deployment
6. **Tag**: Create git tag with version number

## 🤝 Code Review Checklist

- [ ] Tests added for new functionality
- [ ] Performance impact considered
- [ ] Security implications reviewed
- [ ] Documentation updated
- [ ] Breaking changes documented
- [ ] Error handling implemented
- [ ] Audit logging added (for write operations)
- [ ] Cache invalidation handled
- [ ] Code follows style guidelines

## 💡 Tips for Contributors

1. **Start Small**: Begin with documentation improvements or small bug fixes
2. **Ask Questions**: Use GitHub issues for design discussions
3. **Test Thoroughly**: Write tests that cover edge cases
4. **Think Performance**: Consider the impact of changes on responsiveness
5. **Document**: Update documentation for any API changes

## 🔒 Security

For security vulnerabilities, please see our [Security Policy](SECURITY.md). Do not report security issues through public GitHub issues.

## 📄 License

By contributing to this project, you agree that your contributions will be licensed under the [MIT License](LICENSE).

## 🌟 Recognition

Contributors are recognized in:
- Release notes
- GitHub contributor graphs
- Special thanks in documentation

Thank you for contributing to Azure DevOps MCP Server! 🚀