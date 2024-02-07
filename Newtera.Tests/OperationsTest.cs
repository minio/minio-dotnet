using System.Text;
using CommunityToolkit.HighPerformance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtera.DataModel.Args;
using Newtera.Exceptions;

namespace Newtera.Tests;

[TestClass]
public class OperationsTest
{
    private async Task<bool> ObjectExistsAsync(INewteraClient client, string bucket, string objectName)
    {
        try
        {
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithCallbackStream(stream => { });
            _ = await client.GetObjectAsync(getObjectArgs).ConfigureAwait(false);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }
}
