using System;
using System.Collections.Generic;
using Windows.Web.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Restfar.Attributes;
using System.Text.RegularExpressions;

namespace Restfar
{
    /// <summary>
    /// 
    /// </summary>
    public class ServiceMethod
    {
        const string PARAM = "[a-zA-Z][a-zA-Z0-9_-]*";
        const string PARAM_URL_REGEX = "{([a-zA-Z][a-zA-Z0-9_-]*)}";


        private MethodInfo Method { get; set; }
        private string BaseUri { get; set; }
        private Attribute[] MethodAttributes { get; set; }
        private ParameterInfo[] Parameters { get; set; }
        private string HttpMethod { get; set; }
        private bool IsFormEncoded { get; set; }
        private bool IsMultipart { get; set; }
        private bool HasBody { get; set; }
        private string RelativeUrl { get; set; }
        private HashSet<string> RelativeUrlParamNames { get; set; }

        public ServiceMethod(MethodInfo method, string baseUri)
        {
            Method = method;
            BaseUri = baseUri;
            MethodAttributes = method.GetCustomAttributes() as Attribute[];
            Parameters = method.GetParameters();
            ProcessRequestMethod();
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

        private void ProcessParameters()
        {

        }

        public async Task<T> Call<T>(T returnType, object[] args)
        {
            var httpClient = new HttpClient();

            var request = new RequestBuilder(HttpMethod, BaseUri, RelativeUrl, args).Bulid();

            //var result = await httpClient.GetAsync(new Uri("https://api.dribbble.com/v1/users/1135619/buckets?access_token=1d4e28bdbef36dbf2a8dc2cdf4b25a996c356d8defe3439371d2b153a008e915"));
            try
            {
                var result = await httpClient.SendRequestAsync(request);

                if (result.IsSuccessStatusCode)
                {
                    //OnSuccessHandler?.invoke();
                }
                else
                {
                    //OnFailureHanlder?.invoke();
                }

                var content = await result?.Content.ReadAsStringAsync();

                if (returnType != null)
                {
                    return JsonConvert.DeserializeObject<T>(content);
                }
                else
                {
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
                    ParseMethodNameAndPath("OPTIONS", ((OptionsAttribute)attr).Value, false);
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
            RelativeUrlParamNames = ParsePathParameters(value);
        }

        private HashSet<string> ParsePathParameters(string path)
        {
            var patterns = new HashSet<string>();
            var matchs = Regex.Matches(path, PARAM_URL_REGEX, RegexOptions.Compiled);

            foreach(Match m in matchs)
            {
                patterns.Add(m.Groups[1].Value);
            }
            return patterns;
        }

    }

}
