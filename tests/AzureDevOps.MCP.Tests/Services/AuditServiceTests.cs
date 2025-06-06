using AzureDevOps.MCP.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureDevOps.MCP.Tests.Services;

[TestClass]
public class AuditServiceTests
{
	AuditService _auditService = null!;
	string _testAuditDirectory = null!;

	[TestInitialize]
	public void Setup ()
	{
		var services = new ServiceCollection ();
		services.AddLogging ();

		var provider = services.BuildServiceProvider ();
		var logger = provider.GetRequiredService<ILogger<AuditService>> ();

		_auditService = new AuditService (logger);
		_testAuditDirectory = Path.Combine (Directory.GetCurrentDirectory (), "audit");
	}

	[TestCleanup]
	public void Cleanup ()
	{
		if (Directory.Exists (_testAuditDirectory)) {
			Directory.Delete (_testAuditDirectory, recursive: true);
		}
	}

	[TestMethod]
	public async Task LogWriteOperationAsync_ValidEntry_CreatesAuditFile ()
	{
		// Arrange
		var entry = new WriteOperationAuditEntry {
			Operation = "TestOperation",
			TargetResource = "TestResource",
			ProjectName = "TestProject",
			Success = true
		};

		// Act
		await _auditService.LogWriteOperationAsync (entry);

		// Assert
		var auditFile = Path.Combine (_testAuditDirectory, "write-operations.json");
		File.Exists (auditFile).Should ().BeTrue ();
	}

	[TestMethod]
	public async Task GetAuditLogsAsync_NoLogs_ReturnsEmpty ()
	{
		// Act
		var logs = await _auditService.GetAuditLogsAsync ();

		// Assert
		logs.Should ().BeEmpty ();
	}

	[TestMethod]
	public async Task LogWriteOperationAsync_ThenGetLogs_ReturnsLoggedEntry ()
	{
		// Arrange
		var entry = new WriteOperationAuditEntry {
			Operation = "TestOperation",
			TargetResource = "TestResource",
			ProjectName = "TestProject",
			Success = true,
			AdditionalContext = "Test context"
		};

		// Act
		await _auditService.LogWriteOperationAsync (entry);
		var logs = await _auditService.GetAuditLogsAsync ();

		// Assert
		logs.Should ().HaveCount (1);
		var retrievedEntry = logs.First ();
		retrievedEntry.Operation.Should ().Be (entry.Operation);
		retrievedEntry.TargetResource.Should ().Be (entry.TargetResource);
		retrievedEntry.ProjectName.Should ().Be (entry.ProjectName);
		retrievedEntry.Success.Should ().Be (entry.Success);
	}

	[TestMethod]
	public async Task GetAuditLogsAsync_WithSinceFilter_ReturnsFilteredLogs ()
	{
		// Arrange
		var oldEntry = new WriteOperationAuditEntry {
			Operation = "OldOperation",
			TargetResource = "OldResource",
			ProjectName = "TestProject",
			Success = true,
			Timestamp = DateTime.UtcNow.AddHours (-2)
		};

		var newEntry = new WriteOperationAuditEntry {
			Operation = "NewOperation",
			TargetResource = "NewResource",
			ProjectName = "TestProject",
			Success = true,
			Timestamp = DateTime.UtcNow
		};

		await _auditService.LogWriteOperationAsync (oldEntry);
		await _auditService.LogWriteOperationAsync (newEntry);

		// Act
		var recentLogs = await _auditService.GetAuditLogsAsync (DateTime.UtcNow.AddHours (-1));

		// Assert
		recentLogs.Should ().HaveCount (1);
		recentLogs.First ().Operation.Should ().Be ("NewOperation");
	}

	[TestMethod]
	public void HashToken_SameInput_ReturnsSameHash ()
	{
		// Arrange
		const string token = "test-token-123";

		// Act
		var hash1 = AuditService.HashToken (token);
		var hash2 = AuditService.HashToken (token);

		// Assert
		hash1.Should ().Be (hash2);
		hash1.Should ().HaveLength (8);
	}

	[TestMethod]
	public void HashToken_DifferentInputs_ReturnsDifferentHashes ()
	{
		// Arrange
		const string token1 = "test-token-123";
		const string token2 = "test-token-456";

		// Act
		var hash1 = AuditService.HashToken (token1);
		var hash2 = AuditService.HashToken (token2);

		// Assert
		hash1.Should ().NotBe (hash2);
	}
}