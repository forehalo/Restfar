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

    public delegate void RequestSuccessHandler(HttpResponseMessage response);
    public delegate void RequestFailureHandler(HttpResponseMessage response);

    /// <summary>
    /// 
    /// </summary>
    public class ServiceMethod
    {
        const string PARAM_URL_REGEX = "{([a-zA-Z][a-zA-Z0-9_-]*)}";
        
        private MethodInfo Method { get; set; }
        private string BaseUri { get; set; }
        private Attribute[] MethodAttributes { get; set; }
        private Attribute[] ServiceAttributes { get; set; }
        private ParameterInfo[] Parameters { get; set; }
        private string[] HeadersToParse { get; set; } = { };
        private string HttpMethod { get; set; }
        private bool IsFormEncoded { get; set; }
        private bool IsMultipart { get; set; }
        private bool HasBody { get; set; }
        private string RelativeUrl { get; set; }
        private HashSet<string> RelativeUrlParamNames { get; set; } = new HashSet<string>();

        public event RequestSuccessHandler OnSuccess;
        public event RequestFailureHandler OnFailure;

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

        private void ProcessRequestMethod()
        {
            ParseMethodAttributes();

            if (string.IsNullOrEmpty(HttpMethod))
            {
                throw new ArgumentException("HTTP method annotation is required (e.g., @GET, @POST, etc.).");
            }

            if (!HasBody)
            {
                if (IsMultipart)
                {
                    throw new ArgumentException(
                        "Multipart can only be specified on HTTP methods with request body (e.g., @POST).");
                }
                if (IsFormEncoded)
                {
                    throw new ArgumentException("FormUrlEncoded can only be specified on HTTP methods with "
                        + "request body (e.g., @POST).");
                }
            }
        }

        private void ProcessParameters(RequestBuilder builder, object[] args)
        {
            int parameterCount = Parameters.Length;
            for(int i = 0; i < parameterCount; i++)
            {
                ParseParameter(builder, Parameters[i], args[i]);
            }
        }

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
                throw new AggregateException("Http Request return with a exception, please check your network.");
            }
        }


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
                        + "For dynamic query parameters use @Query.");
                }
            }

            RelativeUrl = value;
            ParsePathParameters(value);
        }

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
                        throw new ArgumentException("Field parameters can only be used with form encoding.");

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
                else if(attr is SuccessAttribute)
                {
                    if (argument is RequestSuccessHandler)
                        OnSuccess += (argument as RequestSuccessHandler);
                    else
                        throw new ArgumentException("Request success handler must be type of \"RequestSuccessHandler\".");
                }
                else if(attr is FailureAttribute)
                {
                    if (argument is RequestFailureHandler)
                        OnFailure += (argument as RequestFailureHandler);
                    else
                        throw new ArgumentException("Request failure handler must be type of \"RequestFailureHandler\".");
                }
            }
        }

    }

}
