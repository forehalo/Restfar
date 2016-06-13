using System;
using System.Collections.Generic;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace Restfar
{
    public class RequestBuilder
    {

        private HttpRequestMessage Request;
        
        private string BaseUri;
        private string RelativeUri;
        private bool HasBody;
        private List<KeyValuePair<string, string>> Fields;
        private IHttpContent Form;
        private bool IsFormEncoded;
        private bool IsMultipart;
        private HttpRequestHeaderCollection Headers;

        public RequestBuilder(string httpMethod, string baseUri, string relativeUri, bool hasBody, bool isFormEncoded, bool isMultipart)
        {
            Request = new HttpRequestMessage();
            Request.Method = new HttpMethod(httpMethod);
            Headers = Request.Headers;
            BaseUri = baseUri;
            RelativeUri = relativeUri;
            HasBody = hasBody;

            if (IsFormEncoded = isFormEncoded)
            {
                Fields = new List<KeyValuePair<string, string>>();
            }
            else if (IsMultipart = isMultipart)
            {
                Form = new HttpMultipartFormDataContent();
            }
        }

        public HttpRequestMessage GetRequest()
        {
            if (IsFormEncoded)
            {
                Form = new HttpFormUrlEncodedContent(Fields);
            }

            Request.Content = Form;
            return Request;
        }

        public void AddQeuryString(string name, string value)
        {
            if(!string.IsNullOrEmpty(RelativeUri))
            {
                if (!RelativeUri.Contains("?"))
                {
                    RelativeUri += "?";
                }
                RelativeUri += ("&" + name + "=" + value);
            }
        }

        public void AddHeader(string key, string value)
        {
            if (Headers.ContainsKey(key))
            {
                Headers.Remove(key);
            }
            if (Form != null && Form.Headers.ContainsKey(key))
            {
                Form.Headers.Remove(key);
            }

            if (value == null) return;

            if (Headers.TryAppendWithoutValidation(key, value) && Form != null)
            {
                Form.Headers.TryAppendWithoutValidation(key, value);
            }
        }

        public void AddField(string name, string value)
        {
            Fields.Add(new KeyValuePair<string, string>(name, value));
        }

        public void AddPart(string name, string value)
        {
            EnsureMultipart();
            ((HttpMultipartFormDataContent)Form).Add(new HttpStringContent(value), name);
        }

        public void AddFile(string name, IInputStream stream, string fileName = null)
        {
            EnsureMultipart();
            ((HttpMultipartFormDataContent)Form).Add(new HttpStreamContent(stream), name, fileName);
        }

        private void EnsureMultipart()
        {
            if (!IsMultipart)
                throw new ArgumentException("Only multipart post method can add part.");
        }
    }
}
