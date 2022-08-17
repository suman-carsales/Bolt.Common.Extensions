﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bolt.Common.Extensions.FluentUrls
{

    public interface IHaveUrl : ICollectPath, ICollectQueryParams
    {

    }

    public interface ICollectPath
    {
        IHavePath Path(string path);
        IHavePath Path(params string[] paths);
        IHavePath Path(IEnumerable<string> paths);
    }

    public interface IHavePath : ICollectQueryParams, ICollectPath, IBuildUrl
    {

    }

    public interface ICollectQueryParams
    {
        IHaveQueryParams Query(string name, string value);
        IHaveQueryParams Query(string name, int? value);
        IHaveQueryParams Query(string name, bool? value);
        IHaveQueryParams Query(string name, double? value);
        IHaveQueryParams Query(string name, decimal? value);
        IHaveQueryParams QueryEncoded(string name, string value);
        IHaveQueryParams Query(Dictionary<string, string> values);
        IHaveQueryParams QueryEncoded(Dictionary<string, string> values);
    }

    public interface IHaveQueryParams : IBuildUrl, ICollectQueryParams
    {

    }

    public interface IBuildUrl
    {
        string Build(bool assumeUrlQueryParamsEncoded = false);
    }

    internal sealed class FluentUrlBuilder : IHaveUrl, IHavePath, IHaveQueryParams, IBuildUrl
    {
        private string url;
        private List<string> paths;
        private Dictionary<string, string> queryParams;


        public FluentUrlBuilder(string url, bool assumeUrlQueryParamsEncoded)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                this.url = string.Empty;
                return;
            }

            var urlParts = url.Split(Qs);

            this.url = urlParts[0];

            AppendQueryParamsFromUrlQueryPart(urlParts.Length > 1 ? urlParts[1] : string.Empty, assumeUrlQueryParamsEncoded);
        }

        private void AppendQueryParamsFromUrlQueryPart(string queryPart, bool assumeUrlQueryParamsEncoded)
        {
            if (string.IsNullOrWhiteSpace(queryPart)) return;

            var queryKeyValuePair = queryPart.Split(Amp);

            foreach (var pair in queryKeyValuePair)
            {
                var keyValue = pair.Split(Eq);

                AddQueryParam(keyValue[0], keyValue.Length > 1 ? keyValue[1] : string.Empty, encoded: assumeUrlQueryParamsEncoded);
            }
        }

        public IHavePath Path(string path)
        {
            if (string.IsNullOrEmpty(path)) return this;

            if (paths == null) paths = new List<string>();

            paths.Add(path);

            return this;
        }

        public IHaveQueryParams Query(string name, string value)
        {
            return AddQueryParam(name, value, encoded: false);
        }

        public IHaveQueryParams Query(Dictionary<string, string> values)
        {
            return AddQueryParams(values, encoded: false);
        }

        public IHaveQueryParams QueryEncoded(string name, string value)
        {
            return AddQueryParam(name, value, encoded: true);
        }

        public IHaveQueryParams QueryEncoded(Dictionary<string, string> values)
        {
            return AddQueryParams(values, encoded: true);
        }

        private IHaveQueryParams AddQueryParams(Dictionary<string, string> values, bool encoded)
        {
            if (values == null) return this;

            if (queryParams == null) queryParams = new Dictionary<string, string>();

            foreach (var keyValue in values)
            {
                if (keyValue.Value == null || string.IsNullOrWhiteSpace(keyValue.Key)) continue;

                queryParams[keyValue.Key] = encoded ? keyValue.Value : Uri.UnescapeDataString(keyValue.Value);
            }

            return this;
        }

        private IHaveQueryParams AddQueryParam(string name, string value, bool encoded)
        {
            if (value == null || string.IsNullOrWhiteSpace(name)) return this;

            if (queryParams == null) queryParams = new Dictionary<string, string>();

            queryParams[name] = encoded ? value : Uri.EscapeDataString(value);

            return this;
        }

        private const char Slash = '/';
        private const char Qs = '?';
        private const char Amp = '&';
        private const char Eq = '=';
        public string Build(bool assumeUrlQueryParamsEncoded = false)
        {
            var sb = new StringBuilder(url);

            if (paths != null && paths.Count > 0)
            {
                var urlHasSlash = url.EndsWith(Slash);

                if (urlHasSlash is false) sb.Append(Slash);

                for (var i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];

                    sb.Append(path.Trim(Slash));

                    if (i < paths.Count - 1) sb.Append(Slash);
                }

                var isLastItemHasSlash = paths[paths.Count - 1].EndsWith(Slash);

                if (urlHasSlash || isLastItemHasSlash) sb.Append(Slash);
            }

            if (queryParams != null && queryParams.Count > 0)
            {
                sb.Append(Qs);
                var index = 0;
                foreach (var keyValue in queryParams)
                {
                    sb.Append(keyValue.Key).Append(Eq).Append(keyValue.Value);

                    if (index < queryParams.Count - 1) sb.Append(Amp);

                    index++;
                }
            }

            return sb.ToString();
        }

        public IHaveQueryParams Query(string name, int? value)
        {
            return QueryEncoded(name, value.HasValue ? value.Value.ToString() : null);
        }

        public IHaveQueryParams Query(string name, bool? value)
        {
            return QueryEncoded(name, value.HasValue ? (value.Value ? "true" : "false") : null);
        }

        public IHaveQueryParams Query(string name, double? value)
        {
            return QueryEncoded(name, value.HasValue ? value.Value.ToString() : null);
        }

        public IHaveQueryParams Query(string name, decimal? value)
        {
            return QueryEncoded(name, value.HasValue ? value.Value.ToString() : null);
        }

        public IHavePath Path(params string[] paths)
        {
            if (paths == null || paths.Length == 0) return this;

            if (this.paths == null) this.paths = new List<string>();

            this.paths.AddRange(paths);

            return this;
        }

        public IHavePath Path(IEnumerable<string> paths)
        {
            if (paths == null || paths.Any() is false) return this;

            if (this.paths == null) this.paths = new List<string>();

            this.paths.AddRange(paths);

            return this;
        }
    }

}
