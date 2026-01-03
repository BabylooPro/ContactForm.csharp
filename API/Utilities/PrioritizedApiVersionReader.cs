using Asp.Versioning;

namespace API.Utilities
{
    // CUSTOM API VERSION READER THAT PRIORITIZES QUERY STRING OVER HEADER TO AVOID AMBIGUITY
    public class PrioritizedApiVersionReader : IApiVersionReader
    {
        private readonly QueryStringApiVersionReader _queryReader;
        private readonly HeaderApiVersionReader _headerReader;
        private readonly UrlSegmentApiVersionReader _urlReader;

        public PrioritizedApiVersionReader()
        {
            _queryReader = new QueryStringApiVersionReader("api-version");
            _headerReader = new HeaderApiVersionReader("X-Version");
            _urlReader = new UrlSegmentApiVersionReader();
        }

        public IReadOnlyList<string> Read(HttpRequest request)
        {
            var versions = new List<string>();

            // PRIORITY 1: URL SEGMENT VERSIONING - /api/v1/controller
            var urlVersions = _urlReader.Read(request);
            if (urlVersions.Count > 0)
            {
                versions.AddRange(urlVersions);
                return versions;
            }

            // PRIORITY 2: QUERY STRING VERSIONING - ?api-version=1.0
            var queryVersions = _queryReader.Read(request);
            if (queryVersions.Count > 0)
            {
                versions.AddRange(queryVersions);
                return versions;
            }

            // PRIORITY 3: HEADER VERSIONING - X-Version: 1.0 (ONLY IF QUERY STRING NOT PRESENT)
            var headerVersions = _headerReader.Read(request);
            if (headerVersions.Count > 0)
            {
                versions.AddRange(headerVersions);
            }

            return versions;
        }

        // ADD PARAMETERS FOR ALL THREE METHODS FOR SWAGGER DOCUMENTATION
        public void AddParameters(IApiVersionParameterDescriptionContext context)
        {
            _urlReader.AddParameters(context);
            _queryReader.AddParameters(context);
            _headerReader.AddParameters(context);
        }
    }
}
