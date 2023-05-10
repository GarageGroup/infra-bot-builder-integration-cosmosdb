using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Infra.Bot.Builder;

partial class CosmosStorage
{
    public Task DeleteAsync(string[] keys, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        ThrowIfDisposed();

        if (keys is null || keys.Length is default(int))
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(keys.Select(DeleteItemAsync));
        Task DeleteItemAsync(string key) => InnerDeleteItemAsync(key, cancellationToken);
    }

    private async Task InnerDeleteItemAsync(string key, CancellationToken cancellationToken)
    {
        var itemPath = ParseItemPath(key);
        var result = await lazyCosmosApi.Value.DeleteItemAsync(itemPath, cancellationToken).ConfigureAwait(false);

        _ = result.Fold(InnerPipeSelf, MapFailureOrThrow);
    }

    private static Unit MapFailureOrThrow(StorageItemDeleteFailure failure)
    {
        if (failure.FailureCode is StorageItemDeleteFailureCode.NotFound)
        {
            return default;
        }

        throw new InvalidOperationException(failure.FailureMessage);
    }
}