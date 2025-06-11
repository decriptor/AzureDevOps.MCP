using Microsoft.Extensions.ObjectPool;
using System.Text;
using System.Buffers;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Centralized object pooling service to reduce memory allocations.
/// </summary>
public class MemoryPoolService : IDisposable
{
    readonly ObjectPool<StringBuilder> _stringBuilderPool;
    readonly ArrayPool<byte> _bytePool;
    readonly ArrayPool<char> _charPool;
    readonly ObjectPool<List<string>> _stringListPool;
    
    bool _disposed;

    public MemoryPoolService(ObjectPoolProvider poolProvider)
    {
        _stringBuilderPool = poolProvider.CreateStringBuilderPool(
            initialCapacity: 256, 
            maximumRetainedCapacity: 4096);
            
        _bytePool = ArrayPool<byte>.Shared;
        _charPool = ArrayPool<char>.Shared;
        
        _stringListPool = poolProvider.Create(new StringListPooledObjectPolicy());
    }

    /// <summary>
    /// Gets a pooled StringBuilder with specified initial capacity.
    /// </summary>
    public StringBuilder GetStringBuilder() => _stringBuilderPool.Get();

    /// <summary>
    /// Returns a StringBuilder to the pool after clearing it.
    /// </summary>
    public void ReturnStringBuilder(StringBuilder sb)
    {
        _stringBuilderPool.Return(sb);
    }

    /// <summary>
    /// Gets a pooled byte array of at least the specified size.
    /// </summary>
    public byte[] GetByteArray(int minimumSize) => _bytePool.Rent(minimumSize);

    /// <summary>
    /// Returns a byte array to the pool.
    /// </summary>
    public void ReturnByteArray(byte[] array) => _bytePool.Return(array);

    /// <summary>
    /// Gets a pooled char array of at least the specified size.
    /// </summary>
    public char[] GetCharArray(int minimumSize) => _charPool.Rent(minimumSize);

    /// <summary>
    /// Returns a char array to the pool.
    /// </summary>
    public void ReturnCharArray(char[] array) => _charPool.Return(array);

    /// <summary>
    /// Gets a pooled List of strings for temporary collections.
    /// </summary>
    public List<string> GetStringList() => _stringListPool.Get();

    /// <summary>
    /// Returns a List of strings to the pool after clearing it.
    /// </summary>
    public void ReturnStringList(List<string> list) => _stringListPool.Return(list);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Object pools are managed by DI container, no explicit disposal needed
    }
}