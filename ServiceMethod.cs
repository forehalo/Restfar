using System;
using System.Collections.Generic;
using Windows.Web.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Restfar.Attributes;
using System.Text.RegularExpressions;
using Windows.Storage;
using System.Linq;

namespace Restfar
{
    /// <summary>
    /// Delegate to reference http response handlers.
    /// </summary>
    /// <param name="response" <see cref="HttpResponseMessage"/>>The raw response result.</param>
    public delegate void ResponseHandler(HttpResponseMessage response);

    /// <summary>
    /// Adapts an invocation of an interface method into an HTTP call.
    /// </summary>
    public class ServiceMethod
    {
        /// <summary>
        /// regex of url path placeholder
        /// </summary>
        const string PARAM_URL_REGEX = "{([a-zA-Z][a-zA-Z0-9_-]*)}";

        #region Properties
        /// <summary>
        /// Origin method to call.
        /// </summary>
        private MethodInfo Method { get; set; }
        /// <summary>
        /// Base uri of the request.
        /// </summary>
        private string BaseUri { get; set; }
        /// <summary>
        /// Relative uri of the request.
        /// </summary>
        private string RelativeUrl { get; set; }
        /// <summary>
        /// Method attributes array.
        /// </summary>
        private Attribute[] MethodAttributes { get; set; }
        /// <summary>
        /// Interface attributes array.
        /// </summary>
        private Attribute[] ServiceAttributes { get; set; }
        /// <summary>
        /// All parameters of called method.
        /// </summary>
        private ParameterInfo[] Parameters { get; set; }
        /// <summary>
        /// Origin headers needed to parse.
        /// As the form like <code>{"header1: content", "header2: content"}</code>
        /// </summary>
        private string[] HeadersToParse { get; set; } = { };
        /// <summary>
        /// The http method of the sended request.
        /// </summary>
        private string HttpMethod { get; set; }
        /// <summary>
        /// Tell whether the requst form is encoded.
        /// </summary>
        private bool IsFormEncoded { get; set; }
        /// <summary>
        /// Tell whether the request if multipart.
        /// </summary>
        private bool IsMultipart { get; set; }
        private bool HasBody { get; set; }
        /// <summary>
        /// parameters' name parsed from uri <code>Path</code> placeholder.
        /// </summary>
        private HashSet<string> RelativeUrlParamNames { get; set; } = new HashSet<string>();
        #endregion

        #region Events
        public event ResponseHandler OnSuccess;
        public event ResponseHandler OnFailure;
        #endregion

        /// <summary>
        /// Constructor service method
        /// </summary>
        /// <param name="method">the target method</param>
        /// <param name="baseUri">base uri of all http request.</param>
        public ServiceMethod(MethodInfo method, string baseUri)
        {
            Method = method;
            BaseUri = baseUri;
            MethodAttributes = method.GetCustomAttributes() as Attribute[];
            ServiceAttributes = method.DeclaringType.GetTypeInfo().GetCustomAttributes() as Attribute[];
            Parameters = method.GetParameters();
            ProcessRequestMethod();
            ProcessService();
        }

        /// <summary>
        /// Process all about interface layer attributes.
        /// </summary>
        private void ProcessService()
        {
            foreach(var attr in ServiceAttributes)
            {
                if(attr is HeadersAttribute)
                {
                    HeadersToParse = HeadersToParse.Concat((attr as HeadersAttribute).Value).ToArray();
                }
            }
        }

        /// <summary>
        /// Process all method layer attributes.
        /// </summary>
        private void ProcessRequestMethod()
        {
            ParseMethodAttributes();

            if (string.IsNullOrEmpty(HttpMethod))
            {
                throw new ArgumentException("HTTP method annotation is required (e.g., GET, POST, etc.).");
            }

            if (!HasBody)
            {
                if (IsMultipart)
                {
                    throw new ArgumentException(
                        "Multipart can only be specified on HTTP methods with request body (e.g., POST).");
                }
                if (IsFormEncoded)
                {
                    throw new ArgumentException("FormUrlEncoded can only be specified on HTTP methods with "
                        + "request body (e.g., POST).");
                }
            }
        }

        /// <summary>
        /// Process all parameters' attributes.
        /// </summary>
        /// <param name="builder" <see cref="RequestBuilder"/>>the request builder instance</param>
        /// <param name="args">arguments passed when calling the target method.</param>
        private void ProcessParameters(RequestBuilder builder, object[] args)
        {
            int parameterCount = Parameters.Length;
            for(int i = 0; i < parameterCount; i++)
            {
                ParseParameter(builder, Parameters[i], args[i]);
            }
        }

        /// <summary>
        /// Caller proxy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="returnType">Retrun type of target method</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<T> Call<T>(T returnType, object[] args)
        {
            var httpClient = new HttpClient();

            var requestBuilder = new RequestBuilder(HttpMethod, HeadersToParse, BaseUri, RelativeUrl, HasBody, IsFormEncoded, IsMultipart);
            ProcessParameters(requestBuilder, args);
            try
            {
                var result = await httpClient.SendRequestAsync(requestBuilder.GetRequest());

                if (result.IsSuccessStatusCode)
                {
                    OnSuccess?.Invoke(result);

                    if (returnType != null)
                    {
                        var content = await result?.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<T>(content);
                    }
                    return default(T);
                }
                else
                {
                    OnFailure?.Invoke(result);
                    return default(T);
                }
            }
            catch
            {
                throw new AggregateException("Http Request returned with a exception, please check your network.");
            }
        }

        /// <summary>
        /// Paese http request method and headers.
        /// </summary>
        private void ParseMethodAttributes()
        {
            foreach(var attr in MethodAttributes)
            {
                if (attr is GetAttribute)
                    ParseMethodNameAndPath("GET", ((GetAttribute)attr).Value);
                else if (attr is DeleteAttribute)
                    ParseMethodNameAndPath("DELETE", ((DeleteAttribute)attr).Value);
                else if (attr is PostAttribute)
                    ParseMethodNameAndPath("POST", ((PostAttribute)attr).Value, true);
                else if (attr is PatchAttribute)
                    ParseMethodNameAndPath("PATCH", ((PatchAttribute)attr).Value, true);
                else if (attr is PutAttribute)
                    ParseMethodNameAndPath("PUT", ((PutAttribute)attr).Value, true);
                else if (attr is OptionsAttribute)
                    ParseMethodNameAndPath("OPTIONS", ((OptionsAttribute)attr).Value);
                else if (attr is HeadAttribute)
                    ParseMethodNameAndPath("HEAD", ((OptionsAttribute)attr).Value);
                else if (attr is HeadersAttribute)
                {
                    HeadersToParse = HeadersToParse.Concat((attr as HeadersAttribute).Value).ToArray();

                    if (HeadersToParse.Length == 0)
                    {
                        throw new ArgumentException("Headers is empty");
                    }
                }
                else if (attr is MultipartAttribute) {
                    if (IsFormEncoded)
                    {
                        throw new ArgumentException("Only one encoding annotation is allowed.");
                    }
                    IsMultipart = true;
                } else if (attr is FormUrlEncodedAttribute) {
                    if (IsMultipart)
                    {
                        throw new ArgumentException("Only one encoding annotation is allowed.");
                    }
                    IsFormEncoded = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="value"></param>
        /// <param name="hasBody"></param>
        private void ParseMethodNameAndPath(string httpMethod, string value, bool hasBody = false)
        {
            if (HttpMethod != null)
            {
                throw new ArgumentException(
                    string.Format("Only one HTTP method is allowed. Found: {1} and {2}.", HttpMethod, httpMethod));
            }

            HttpMethod = httpMethod;
            HasBody = hasBody;

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            // Get the relative URL path and existing query string, if present.
            int question = value.IndexOf('?');
            if (question != -1 && question < value.Length - 1)
            {
                // Ensure the query string does not have any named parameters.
                string queryParams = value.Substring(question + 1);
                Match queryParamMatcher = Regex.Match(queryParams, PARAM_URL_REGEX);
                if (queryParamMatcher.Success)
                {
                    throw new ArgumentException("URL query string \"" + queryParams + "\" must not have replace block. "
                        + "For dynamic query parameters use Query attribute.");
                }
            }

            RelativeUrl = value;
            ParsePathParameters(value);
        }

        /// <summary>
        /// parse all path parameters in relative uri.
        /// </summary>
        /// <param name="path"></param>
        private void ParsePathParameters(string path)
        {
            var matchs = Regex.Matches(path, PARAM_URL_REGEX, RegexOptions.Compiled);

            foreach(Match m in matchs)
            {
                RelativeUrlParamNames.Add(m.Groups[1].Value);
            }
        }


        private void ParseParameter(RequestBuilder builder, ParameterInfo parameter, object argument)
        {
            var attributes = parameter.GetCustomAttributes() as Attribute[];

            foreach(var attr in attributes)
            {
                if (attr is PathAttribute)
                {
                    var tmp = attr as PathAttribute;
                    if (RelativeUrlParamNames.Contains(tmp.Value))
                    {
                        builder.AddPath(tmp.Value, argument.ToString());
                        continue;
                    }
                    throw new ArgumentException("Specified path \"" + tmp.Value + "\" not found");
                }
                else if (attr is FieldAttribute)
                {
                    if (!IsFormEncoded)
                        throw new ArgumentException("Field parameters can only be used with form encoded.");

                    builder.AddField((attr as FieldAttribute).Value, argument.ToString());
                }
                else if (attr is PartAttribute)
                {
                    if (!IsMultipart)
                        throw new ArgumentException("Part parameters can only be used with multipart encoding.");

                    builder.AddPart((attr as PartAttribute).Value, argument.ToString());
                }
                else if (attr is QueryAttribute)
                {
                    builder.AddQeuryString((attr as QueryAttribute).Value, argument.ToString());
                }
                else if (attr is FileAttribute)
                {
                    if (!IsMultipart)
                        throw new ArgumentException("File parameters can only be used with multipart encoding.");

                    var tmp = attr as FileAttribute;
                    var file = argument as StorageFile;
                    var filestream = file.OpenReadAsync().AsTask();

                    builder.AddFile(tmp.Value, filestream.Result, file.Name);
                }
                else if (attr is HeadersAttribute)
                {
                    builder.ParseHeaders(argument as string[]);
                }
                else if(attr is SuccessAttribute)
                {
                    if (argument is ResponseHandler)
                        OnSuccess += (argument as ResponseHandler);
                    else
                        throw new ArgumentException("Request success handler must be type of \"ResponseHandler\".");
                }
                else if(attr is FailureAttribute)
                {
                    if (argument is ResponseHandler)
                        OnFailure += (argument as ResponseHandler);
                    else
                        throw new ArgumentException("Request failure handler must be type of \"ResponseHandler\".");
                }
            }
        }

    }

}
