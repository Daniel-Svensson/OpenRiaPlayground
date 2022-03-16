// ReuqstDelegate
using OpenRiaServices.Hosting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Hosting.Wcf.Behaviors;
using OpenRiaServices.Server;
using System.Runtime.Serialization;

class QueryOperationInvoker<TEntity> : IDomainOperationInvoker
{
    private static readonly WebHttpQueryStringConverter s_queryStringConverter = new();
    private readonly DomainOperationEntry operation;
    private readonly DataContractSerializer serializer;

    public QueryOperationInvoker(DomainOperationEntry operation, SerializationHelper serializationHelper)
    //        : base(DomainOperationType.Query)
    {
        this.operation = operation;
        var knownTypes = DomainServiceDescription.GetDescription(operation.DomainServiceType).EntityKnownTypes;
        this.serializer = serializationHelper.GetSerializer(typeof(QueryResult<TEntity>), knownTypes.GetValueOrDefault(typeof(TEntity)));
    }

    public async Task Invoke(HttpContext context)
    {
        var domainService = (DomainService)context.RequestServices.GetRequiredService(operation.DomainServiceType);
        var serviceContext = new AspNetDomainServiceContext(context, DomainOperationType.Query);
        domainService.Initialize(serviceContext);

        // TODO: consider using ArrayPool<object>.Shared in future
        var inputs = new object[this.operation.Parameters.Count];
        SetParametersFromUri(context, inputs);

        // TODO: Try/Catch + write fault
        var result = await InvokeCoreAsync(context, domainService, inputs);

        var response = context.Response;
        response.Headers.ContentType = "application/msbin1";
        response.StatusCode = 200;

        await WriteResponse(context, result);
    }

    private async Task WriteResponse(HttpContext context, QueryResult<TEntity> result)
    {
        // TODO: Port BufferManagerStream and related code
        using var ms = new PooledStream.PooledMemoryStream();
        using (var writer = System.Xml.XmlDictionaryWriter.CreateBinaryWriter(ms, null, null, ownsStream: false))
        {
            string operationName = operation.Name;
            // <GetQueryableRangeTaskResponse xmlns="http://tempuri.org/">
            writer.WriteStartElement(operationName + "Response", "http://tempuri.org/");
            // <GetQueryableRangeTaskResult xmlns:a="DomainServices" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
            writer.WriteStartElement(operationName + "Result");
            writer.WriteXmlnsAttribute("a", "DomainServices");
            writer.WriteXmlnsAttribute("i", "http://www.w3.org/2001/XMLSchema-instance");

            // TODO: XmlElemtnt  support
            //// XmlElemtnt returns the "ResultNode" unless we step into the contents
            //if (returnType == typeof(System.Xml.Linq.XElement))
            //    reader.ReadStartElement();

            this.serializer.WriteObjectContent(writer, result);

            writer.WriteEndElement(); // ***Result

            writer.WriteEndElement(); // ***Response

            //      writer.WriteEndDocument();
            writer.Flush();
            ms.Flush();
        }


        context.Response.ContentLength = ms.Length;
        var ct = context.RequestAborted;
        //await context.Response.StartAsync(ct);
        await context.Response.Body.WriteAsync(ms.ToMemoryUnsafe(), ct);
        //await context.Response.CompleteAsync();
    }

    private void SetParametersFromUri(HttpContext context, object[] inputs)
    {
        var query = context.Request.Query;
        var parameters = operation.Parameters;

        for (int i = 0; i < parameters.Count; ++i)
        {
            if (query.TryGetValue(parameters[i].Name, out var values))
            {
                var value = values.FirstOrDefault();
                inputs[i] = s_queryStringConverter.ConvertStringToValue(value, parameters[i].ParameterType);
            }
        }
    }

    protected /*override*/ async ValueTask<QueryResult<TEntity>> InvokeCoreAsync(HttpContext httpContext, DomainService instance, object[] inputs)
    {
        ServiceQuery serviceQuery = null;
        QueryAttribute queryAttribute = (QueryAttribute)this.operation.OperationAttribute;

        if (queryAttribute.IsComposable)
        {
            serviceQuery = GetServiceQuery(httpContext.Request);
        }

        QueryResult<TEntity> result;
        try
        {
            //SetOutputCachingPolicy(httpContext, this.operation);
            result = await QueryProcessor.ProcessAsync<TEntity>(instance, this.operation, inputs, serviceQuery);
        }
        catch (Exception ex)
        {
            //if (ex.IsFatal())
            //{
            //    throw;
            //}
            throw;
            //ClearOutputCachingPolicy(httpContext);
            //throw ServiceUtility.CreateFaultException(ex, disableStackTraces);
        }


        if (result.ValidationErrors != null && result.ValidationErrors.Any())
        {
            //         throw ServiceUtility.CreateFaultException(result.ValidationErrors, disableStackTraces);
        }

        return result;
    }

    protected /*override*/ void ConvertInputs(object[] inputs)
    {
        // Handles System.Data.Linq.Binary type, (LINQ to SQL)
        //for (int i = 0; i < this.operation.Parameters.Count; i++)
        //{
        //    DomainOperationParameter parameter = this.operation.Parameters[i];
        //    inputs[i] = SerializationUtility.GetServerValue(parameter.ParameterType, inputs[i]);
        //}
    }

    // FROM DomainServiceWebHttpBehavior
    /// <summary>
    /// This method returns a ServiceQuery for the specified URL and query string.
    /// <remarks>
    /// This method must ensure that the original ordering of the query parts is maintained
    /// in the results. We want to do this without doing any custom URL parsing. The approach
    /// taken is to use HttpUtility to parse the query string, and from those results we search
    /// in the full URL for the relative positioning of those elements.
    /// </remarks>
    /// </summary>
    /// <param name="queryString">The query string portion of the URL</param>
    /// <param name="fullRequestUrl">The full request URL</param>
    /// <returns>The corresponding ServiceQuery</returns>
    internal static ServiceQuery GetServiceQuery(HttpRequest httpRequest)
    {
        var queryPartCollection = httpRequest.Query;
        string fullRequestUrl = httpRequest.QueryString.Value;
        bool includeTotalCount = false;

        // Reconstruct a list of all key/value pairs
        List<string> queryParts = new List<string>();
        foreach (string queryPart in queryPartCollection.Keys)
        {
            if (queryPart == null || !queryPart.StartsWith("$", StringComparison.Ordinal))
            {
                // not a special query string
                continue;
            }

            if (queryPart.Equals("$includeTotalCount", StringComparison.OrdinalIgnoreCase))
            {
                string value = queryPartCollection[queryPart].First();
                Boolean.TryParse(value, out includeTotalCount);
                continue;
            }

            foreach (string value in queryPartCollection[queryPart])
            {
                queryParts.Add(queryPart + "=" + value);
            }
        }

        string decodedQueryString = /*HttpUtility.UrlDecode*/ Uri.UnescapeDataString(fullRequestUrl);

        // For each query part, find all occurrences of it in the Url (could be duplicates)
        List<KeyValuePair<string, int>> keyPairIndicies = new List<KeyValuePair<string, int>>();
        foreach (string queryPart in queryParts.Distinct())
        {
            int idx;
            int endIdx = 0;
            while (((idx = decodedQueryString.IndexOf(queryPart, endIdx, StringComparison.Ordinal)) != -1) &&
                    (endIdx < decodedQueryString.Length - 1))
            {
                // We found a match, however, we must ensure that the match is exact. For example,
                // The string "$take=1" will be found twice in query string "?$take=10&$orderby=Name&$take=1",
                // but the first match should be discarded. Therefore, before adding the match, we ensure
                // the next character is EOS or the param separator '&'.
                endIdx = idx + queryPart.Length - 1;
                if ((endIdx == decodedQueryString.Length - 1) ||
                    (endIdx < decodedQueryString.Length - 1 && (decodedQueryString[endIdx + 1] == '&')))
                {
                    keyPairIndicies.Add(new KeyValuePair<string, int>(queryPart, idx));
                }
            }
        }

        // create the list of ServiceQueryParts in order, ordered by
        // their location in the query string
        IEnumerable<string> orderedParts = keyPairIndicies.OrderBy(p => p.Value).Select(p => p.Key);
        IEnumerable<ServiceQueryPart> serviceQueryParts =
            from p in orderedParts
            let idx = p.IndexOf('=')
            select new ServiceQueryPart(p.Substring(1, idx - 1), p.Substring(idx + 1));

        ServiceQuery serviceQuery = new ServiceQuery()
        {
            QueryParts = serviceQueryParts.ToList(),
            IncludeTotalCount = includeTotalCount
        };

        return serviceQuery;
    }
}
