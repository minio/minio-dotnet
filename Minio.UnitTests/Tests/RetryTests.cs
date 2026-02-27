using System.Net;
using Xunit;

namespace Minio.UnitTests.Tests;

public class RetryTests : MinioUnitTests
{
    public static readonly IEnumerable<object[]> Data =
    [
        [HttpStatusCode.BadRequest /* 400 */, false],
        [HttpStatusCode.Unauthorized /* 401 */, false ],
        [HttpStatusCode.PaymentRequired /* 402 */, false ],
        [HttpStatusCode.Forbidden /* 403 */, false ],
        //[HttpStatusCode.NotFound /* 404 */, false ],  /* skipped, because it's handled internally */
        [HttpStatusCode.MethodNotAllowed /* 405 */, false ],
        [HttpStatusCode.NotAcceptable /* 406 */, false ],
        [HttpStatusCode.ProxyAuthenticationRequired /* 407 */, false ],
        [HttpStatusCode.RequestTimeout /* 408 */, true ],
        [HttpStatusCode.Conflict /* 409 */, false ],
        [HttpStatusCode.Gone /* 410 */, false ],
        [HttpStatusCode.LengthRequired /* 411 */, false ],
        [HttpStatusCode.PreconditionFailed /* 412 */, false ],
        [HttpStatusCode.RequestEntityTooLarge /* 413 */, false ],
        [HttpStatusCode.RequestUriTooLong /* 414 */, false ],
        [HttpStatusCode.UnsupportedMediaType /* 415 */, false ],
        [HttpStatusCode.RequestedRangeNotSatisfiable /* 416 */, false ],
        [HttpStatusCode.ExpectationFailed /* 417 */, false ],
        [HttpStatusCode.MisdirectedRequest /* 421 */, false ],
        [HttpStatusCode.UnprocessableEntity /* 422 */, false ],
        [HttpStatusCode.Locked /* 423 */, true ],
        [HttpStatusCode.FailedDependency /* 424 */, false ],
        [HttpStatusCode.UpgradeRequired /* 426 */, false ],
        [HttpStatusCode.PreconditionRequired /* 428 */, false ],
        [HttpStatusCode.TooManyRequests /* 429 */, true ],
        [HttpStatusCode.RequestHeaderFieldsTooLarge /* 431 */, false ],
        [HttpStatusCode.UnavailableForLegalReasons /* 451 */, false ],
        [HttpStatusCode.InternalServerError /* 500 */, true ],
        [HttpStatusCode.NotImplemented /* 501 */, false ],
        [HttpStatusCode.BadGateway /* 502 */, true ],
        [HttpStatusCode.ServiceUnavailable /* 503 */, true ],
        [HttpStatusCode.GatewayTimeout /* 504 */, true ],
        [HttpStatusCode.HttpVersionNotSupported /* 505 */, false ],
        [HttpStatusCode.VariantAlsoNegotiates /* 506 */, false ],
        [HttpStatusCode.InsufficientStorage /* 507 */, false ],
        [HttpStatusCode.LoopDetected /* 508 */, false ],
        [HttpStatusCode.NotExtended /* 510 */, false ],
        [HttpStatusCode.NetworkAuthenticationRequired /* 511 */, false],
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public async Task CheckRetries(HttpStatusCode statusCode, bool expectRetry)
    {
        var attempt = 0; 
        await RunWithMinioClientAsync((req, resp) =>
        {
            // Check request
            Assert.Equal("HEAD", req.Method.Method);
            Assert.Equal("http://localhost:9000/testbucket", req.RequestUri?.ToString());

            if (attempt > 0)
            {
                if (expectRetry)
                    resp.StatusCode = HttpStatusCode.OK;
                else 
                    Assert.Fail($"HTTP status code {statusCode} ({(int)statusCode}) shouldn't be retried");
            }
            else
            {
                resp.StatusCode = statusCode;
            }
                
            attempt++;
        }, 
        async minioClient =>
        {
            if (expectRetry)
            {
                var exists = await minioClient.BucketExistsAsync("testbucket").ConfigureAwait(true);
                Assert.True(exists);
            }
            else
            {
                try
                {
                    await minioClient.BucketExistsAsync("testbucket").ConfigureAwait(true);
                    Assert.Fail("Should throw exception");
                }
                catch (MinioHttpException exc)
                {
                    Assert.Equal(statusCode, exc.Response.StatusCode);
                }
            }
        }, true);

    }

}