namespace Minio.Rest.Extensions
{
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Minio.Rest.Impl;
    using RestSharp.Portable;

    internal static class RestClientExtensions
    {
        internal static IHttpContent GetContent(this IRestClient client, IRestRequest request, RequestParameters parameters)
        {
            HttpContent content;
            var collectionMode = request?.ContentCollectionMode ?? ContentCollectionMode.MultiPartForFileParameters;
            if (collectionMode != ContentCollectionMode.BasicContent)
            {
                var fileParameters = parameters.OtherParameters.GetFileParameters().ToList();
                if (collectionMode == ContentCollectionMode.MultiPart || fileParameters.Count != 0)
                {
                    content = client.GetMultiPartContent(request, parameters);
                }
                else
                {
                    content = client.GetBasicContent(request, parameters);
                }
            }
            else
            {
                content = client.GetBasicContent(request, parameters);
            }

            if (content == null)
            {
                return null;
            }

            foreach (var param in parameters.ContentHeaderParameters)
            {
                if (content.Headers.Contains(param.Name))
                {
                    content.Headers.Remove(param.Name);
                }

                if (param.ValidateOnAdd)
                {
                    content.Headers.Add(param.Name, param.ToRequestString());
                }
                else
                {
                    content.Headers.TryAddWithoutValidation(param.Name, param.ToRequestString());
                }
            }

            return new MinioHttpContent(content);
        }

        private static HttpContent GetBasicContent(this IRestClient client, IRestRequest request, RequestParameters parameters)
        {
            HttpContent content;
            var body = parameters.OtherParameters.FirstOrDefault(x => x.Type == ParameterType.RequestBody);
            if (body != null)
            {
                content = request.GetBodyContent(body);
            }
            else
            {
                var effectiveMethod = client.GetEffectiveHttpMethod(request);
                if (effectiveMethod != Method.GET)
                {
                    var getOrPostParameters = parameters.OtherParameters.GetGetOrPostParameters().ToList();
                    content = getOrPostParameters.Count != 0 ? new PostParametersContent(getOrPostParameters).AsHttpContent() : null;
                }
                else
                {
                    content = null;
                }
            }

            return content;
        }

        private static HttpContent GetMultiPartContent(this IRestClient client, IRestRequest request, RequestParameters parameters)
        {
            var isPostMethod = client.GetEffectiveHttpMethod(request) == Method.POST;
            var multipartContent = new MultipartFormDataContent();
            foreach (var parameter in parameters.OtherParameters)
            {
                var fileParameter = parameter as FileParameter;
                if (fileParameter != null)
                {
                    var file = fileParameter;
                    var data = new ByteArrayContent((byte[])file.Value);
                    data.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                    data.Headers.ContentLength = file.ContentLength;
                    multipartContent.Add(data, file.Name, file.FileName);
                }
                else if (isPostMethod && parameter.Type == ParameterType.GetOrPost)
                {
                    HttpContent data;
                    var bytes = parameter.Value as byte[];
                    if (bytes != null)
                    {
                        var rawData = bytes;
                        data = new ByteArrayContent(rawData);
                        data.Headers.ContentType = string.IsNullOrEmpty(parameter.ContentType)
                                                       ? new MediaTypeHeaderValue("application/octet-stream")
                                                       : MediaTypeHeaderValue.Parse(parameter.ContentType);
                        data.Headers.ContentLength = rawData.Length;
                        multipartContent.Add(data, parameter.Name);
                    }
                    else
                    {
                        var value = parameter.ToRequestString();
                        data = new StringContent(value, parameter.Encoding ?? ParameterExtensions.DefaultEncoding);
                        if (!string.IsNullOrEmpty(parameter.ContentType))
                        {
                            data.Headers.ContentType = MediaTypeHeaderValue.Parse(parameter.ContentType);
                        }

                        multipartContent.Add(data, parameter.Name);
                    }
                }
                else if (parameter.Type == ParameterType.RequestBody)
                {
                    var data = request.GetBodyContent(parameter);
                    multipartContent.Add(data, parameter.Name);
                }
            }

            return multipartContent;
        }

        private static HttpContent GetBodyContent(this IRestRequest request, Parameter body)
        {
            if (body == null)
            {
                return null;
            }

            MediaTypeHeaderValue contentType;
            var buffer = body.Value as byte[];
            if (buffer != null)
            {
                contentType = MediaTypeHeaderValue.Parse(body.ContentType ?? "application/octet-stream");
            }
            else
            {
                buffer = request.Serializer.Serialize(body.Value);
                contentType = MediaTypeHeaderValue.Parse(request.Serializer.ContentType);
            }

            var content = new ByteArrayContent(buffer);
            content.Headers.ContentType = contentType;
            content.Headers.ContentLength = buffer.Length;
            return content;
        }
    }
}