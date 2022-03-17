// ReuqstDelegate
using OpenRiaServices.Hosting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Hosting.Wcf.Behaviors;
using OpenRiaServices.Server;
using System.Runtime.Serialization;


abstract class OperationInvoker
{
    private static readonly WebHttpQueryStringConverter s_queryStringConverter = new();
    protected readonly DomainOperationEntry operation;
    private readonly DomainOperationType operationType;
    protected readonly SerializationHelper serializationHelper;
    private readonly DataContractSerializer responseSerializer;

    private const string MessageRootElementName = "MessageRoot";
    private const string QueryOptionsListElementName = "QueryOptions";
    private const string QueryOptionElementName = "QueryOption";
    private const string QueryNameAttribute = "Name";
    private const string QueryValueAttribute = "Value";
    private const string QueryIncludeTotalCountOption = "includeTotalCount";


    public OperationInvoker(DomainOperationEntry operation, DomainOperationType operationType,
        SerializationHelper serializationHelper,
        DataContractSerializer responseSerializer)
    {
        this.operation = operation;
        this.operationType = operationType;
        this.serializationHelper = serializationHelper;
        this.responseSerializer = responseSerializer;
    }

    public abstract Task Invoke(HttpContext context);

    protected object[] GetParametersFromUri(HttpContext context)
    {
        var query = context.Request.Query;
        var parameters = operation.Parameters;
        var inputs = new object[parameters.Count];
        for (int i = 0; i < parameters.Count; ++i)
        {
            if (query.TryGetValue(parameters[i].Name, out var values))
            {
                var value = values.FirstOrDefault();
                inputs[i] = s_queryStringConverter.ConvertStringToValue(value, parameters[i].ParameterType);
            }
        }

        return inputs;
    }

    protected async Task<(ServiceQuery, object[])> ReadParametersFromBody(HttpContext context)
    {
        // TODO: Use arraypool directly instead
        using var ms = new PooledStream.PooledMemoryStream();
        var contentLength = context.Request.ContentLength;

        if (contentLength > 0 && contentLength < int.MaxValue)
            ms.Reserve((int)contentLength);
        await context.Request.BodyReader.CopyToAsync(ms);

        ServiceQuery serviceQuery = null;
        object[] values;
        ms.Seek(0, SeekOrigin.Begin);
        using (var reader = System.Xml.XmlDictionaryReader.CreateBinaryReader(ms, null, System.Xml.XmlDictionaryReaderQuotas.Max))
        {
            reader.MoveToContent();

            bool hasMessageRoot = reader.IsStartElement("MessageRoot");
            // Check for QueryOptions which is part of message root
            if (hasMessageRoot)
            {
                // Go to the <QueryOptions> node.
                reader.Read();                                                              // <MessageRoot>
                reader.ReadStartElement(QueryOptionsListElementName);        // <QueryOptions>
                serviceQuery = ReadServiceQuery(reader);                     // <QueryOption></QueryOption>
                                                                             // Go to the starting node of the original message.
                reader.ReadEndElement();                                                    // </QueryOptions>
                //reader = XmlDictionaryReader.CreateDictionaryReader(reader.ReadSubtree());  // Remainder of the message
            }

            values = ReadParameters(reader);

            if (hasMessageRoot)
                reader.ReadEndElement();

            // Verify at end 
            if (reader.ReadState != System.Xml.ReadState.EndOfFile)
                throw new InvalidDataException();
            return (serviceQuery, values);
        }
    }
    /// Reads the query options from the given reader and returns the resulting service query.
    /// It assumes that the reader is positioned on a stream containing the query options.
    /// </summary>
    /// <param name="reader">Reader to the stream containing the query options.</param>
    /// <returns>Extracted service query.</returns>
    internal static ServiceQuery ReadServiceQuery(System.Xml.XmlReader reader)
    {
        List<ServiceQueryPart> serviceQueryParts = new List<ServiceQueryPart>();
        bool includeTotalCount = false;
        while (reader.IsStartElement(QueryOptionElementName))
        {
            string name = reader.GetAttribute(QueryNameAttribute);
            string value = reader.GetAttribute(QueryValueAttribute);
            if (name.Equals(QueryIncludeTotalCountOption, StringComparison.OrdinalIgnoreCase))
            {
                bool queryOptionValue = false;
                if (Boolean.TryParse(value, out queryOptionValue))
                {
                    includeTotalCount = queryOptionValue;
                }
            }
            else
            {
                serviceQueryParts.Add(new ServiceQueryPart { QueryOperator = name, Expression = value });
            }

            ReadElement(reader);
        }

        ServiceQuery serviceQuery = new ServiceQuery()
        {
            QueryParts = serviceQueryParts,
            IncludeTotalCount = includeTotalCount
        };
        return serviceQuery;
    }

    private static void ReadElement(System.Xml.XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.Read();
            reader.ReadEndElement();
        }
    }

    private object[] ReadParameters(System.Xml.XmlDictionaryReader reader)
    {
        object[] values;
        if (reader.IsStartElement(operation.Name))
        {
            reader.Read();

            var parameters = operation.Parameters;
            values = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; ++i)
            {
                var parameter = parameters[i];
                if (!reader.IsStartElement(parameter.Name))
                    throw new InvalidDataException();

                if (reader.HasAttributes && reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true"
                    || ((reader.NodeType == System.Xml.XmlNodeType.EndElement || reader.IsEmptyElement)))
                {
                    values[i] = null;
                }
                else
                {
                    // TODO: consider knowtypes ?
                    var serializer = serializationHelper.GetSerializer(parameter.ParameterType, null);
                    values[i] = serializer.ReadObject(reader, verifyObjectName: false);
                }
            }
            // TODO: Verify we are at end element ?
            reader.ReadEndElement(); // operation.Name
        }
        else
        {
            if (operation.Parameters.Count == 0)
                values = Array.Empty<object>();
            else
                throw new InvalidDataException();
        }

        return values;
    }


    /// <summary>
    /// Verifies the reader is at node with LocalName equal to operationName + postfix.
    /// If the reader is at any other node, then a <see cref="DomainOperationException"/> is thrown
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="postfix">The postfix.</param>
    /// <exception cref="DomainOperationException">If reader is not at the expected xml element</exception>
    protected static void VerifyReaderIsAtNode(System.Xml.XmlDictionaryReader reader, string operationName, string postfix)
    {
        // localName should be operationName + postfix
        if (!(reader.LocalName.Length == operationName.Length + postfix.Length
            && reader.LocalName.StartsWith(operationName, StringComparison.Ordinal)
            && reader.LocalName.EndsWith(postfix, StringComparison.Ordinal)))
        {
            // TODO:
            throw new InvalidDataException();
        }
    }

    protected async Task WriteResponse(HttpContext context, object result)
    {
        var response = context.Response;
        response.Headers.ContentType = "application/msbin1";
        response.StatusCode = 200;

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

            this.responseSerializer.WriteObjectContent(writer, result);

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

    protected DomainService CreateDomainService(HttpContext context)
    {
        var domainService = (DomainService)context.RequestServices.GetRequiredService(operation.DomainServiceType);
        var serviceContext = new AspNetDomainServiceContext(context, this.operationType);
        domainService.Initialize(serviceContext);
        return domainService;
    }
}

class InvokeOperationInvoker : OperationInvoker
{
    public InvokeOperationInvoker(DomainOperationEntry operation, SerializationHelper serializationHelper)
            : base(operation, DomainOperationType.Invoke, serializationHelper, GetRespponseSerializer(operation, serializationHelper))
    {
    }

    private static DataContractSerializer GetRespponseSerializer(DomainOperationEntry operation, SerializationHelper serializationHelper)
    {
//        var knownTypes = DomainServiceDescription.GetDescription(operation.DomainServiceType).EntityKnownTypes;
        return serializationHelper.GetSerializer(operation.ReturnType, null);
    }

    public override async Task Invoke(HttpContext context)
    {
        DomainService domainService = CreateDomainService(context);

        // TODO: consider using ArrayPool<object>.Shared in future
        object[] inputs;
        if (context.Request.Method == "GET")
        {
            inputs = GetParametersFromUri(context);
        }
        else // POST
        {
            if (context.Request.ContentType != "application/msbin1")
            {
                context.Response.StatusCode = 400; // maybe 406 / System.Net.HttpStatusCode.NotAcceptable
                return;
            }
            (_, inputs) = await ReadParametersFromBody(context);
        }

        // TODO: Try/Catch + write fault
        ServiceInvokeResult invokeResult;
        //try
        //{
//            SetOutputCachingPolicy(httpContext, operation);
            InvokeDescription invokeDescription = new InvokeDescription(this.operation, inputs);
            invokeResult = await domainService.InvokeAsync(invokeDescription, domainService.ServiceContext.CancellationToken).ConfigureAwait(false);
        //}
        //catch (Exception ex)
        //{
        //    if (ex.IsFatal())
        //    {
        //        throw;
        //    }
        //    ClearOutputCachingPolicy(httpContext);
        //    throw ServiceUtility.CreateFaultException(ex, disableStackTraces);
        //}

        if (invokeResult.HasValidationErrors)
        {
            throw new NotImplementedException();
            //throw ServiceUtility.CreateFaultException(invokeResult.ValidationErrors, disableStackTraces);
        }
        else
        {
            await WriteResponse(context, invokeResult.Result);
        }
    }
}

class QueryOperationInvoker<TEntity> : OperationInvoker
{
    public QueryOperationInvoker(DomainOperationEntry operation, SerializationHelper serializationHelper)
            : base(operation, DomainOperationType.Query, serializationHelper, GetRespponseSerializer(operation, serializationHelper))
    {
    }

    private static DataContractSerializer GetRespponseSerializer(DomainOperationEntry operation, SerializationHelper serializationHelper)
    {
        var knownTypes = DomainServiceDescription.GetDescription(operation.DomainServiceType).EntityKnownTypes;
        return serializationHelper.GetSerializer(typeof(QueryResult<TEntity>), knownTypes.GetValueOrDefault(typeof(TEntity)));
    }

    public override async Task Invoke(HttpContext context)
    {
        DomainService domainService = CreateDomainService(context);

        // TODO: consider using ArrayPool<object>.Shared in future
        object[] inputs;
        ServiceQuery serviceQuery;
        if (context.Request.Method == "GET")
        {
            inputs = GetParametersFromUri(context);

            QueryAttribute queryAttribute = (QueryAttribute)this.operation.OperationAttribute;
            serviceQuery = queryAttribute.IsComposable ? GetServiceQuery(context.Request) : null;
        }
        else // POST
        {
            if (context.Request.ContentType != "application/msbin1")
            {
                context.Response.StatusCode = 400; // maybe 406 / System.Net.HttpStatusCode.NotAcceptable
                return;
            }

            (serviceQuery, inputs) = await ReadParametersFromBody(context);
        }

        // TODO: Try/Catch + write fault
        var result = await InvokeCoreAsync(context, domainService, inputs, serviceQuery);


        await WriteResponse(context, result);
    }



    protected /*override*/ async ValueTask<QueryResult<TEntity>> InvokeCoreAsync(HttpContext httpContext, DomainService instance, object[] inputs, ServiceQuery serviceQuery)
    {
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
