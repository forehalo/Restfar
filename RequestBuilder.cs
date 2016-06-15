using System;
using System.Collections.Generic;
using Windows.Storage.Streams;
using Windows.Web.Http;

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

        public RequestBuilder(string httpMethod, string[] headersToParse, string baseUri, string relativeUri, bool hasBody, bool isFormEncoded, bool isMultipart)
        {
            Request = new HttpRequestMessage();
            Request.Method = new HttpMethod(httpMethod);

            ParseHeaders(headersToParse);

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
            Request.RequestUri = new Uri(BaseUri + RelativeUri);
            Request.Content = Form;
            return Request;
        }


        public void ParseHeaders(string[] headers)
        {
            foreach (var header in headers)
            {
                if (string.IsNullOrEmpty(header))
                {
                    throw new ArgumentException("Header content must not be empty.");
                }

                var keyValue = header.Split(':');

                if(keyValue.Length != 2)
                {
                    throw new ArgumentException("Header name and value must not be empty. Check the headers format(eg. {\" custom-header : header content\", \" another-header : another\"})");
                }

                AddHeader(keyValue[0].Trim(), keyValue[1].Trim());
            }
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
            if (Request.Headers.ContainsKey(key))
            {
                Request.Headers.Remove(key);
            }

            if (value == null) return;

            Request.Headers.TryAppendWithoutValidation(key, value);
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

        public void AddFile(string name, IInputStream stream, string fileName)
        {
            EnsureMultipart();
            ((HttpMultipartFormDataContent)Form).Add(new HttpStreamContent(stream), name, fileName);
        }

        public void AddPath(string name, string value)
        {
            if (string.IsNullOrEmpty(RelativeUri))
            {
                throw new ArgumentException("No path found in given relative uri: " + RelativeUri);
            }
            RelativeUri = RelativeUri.Replace("{" + name + "}", value);
        }

        private void EnsureMultipart()
        {
            if (!IsMultipart)
                throw new ArgumentException("Only multipart post method can add part.");
        }
    }
}
