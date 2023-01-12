using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using LogicApps.Portals.Queries;
using LogicApps.Portals.Plugin;
using LogicApps.Portals.Helpers;
using LogicApps.Portals.CustomEntity;

namespace LogicApps.Portals
{
    public class QueryService
    {
        private readonly IUnitOfService _unitOfService;

        private readonly FetchExpression _fetchExpression;

        private readonly IReadOnlyDictionary<string, Func<string, IUnitOfService, EntityCollection>> _queryDataMap = new Dictionary<string, Func<string, IUnitOfService, EntityCollection>>
        {
            {"import-contact", ImportProcessQuery.RunImportProcessQuery }
        };

        public QueryService(IUnitOfService unitOfService, FetchExpression fetchQuery)
        {
            _unitOfService = unitOfService ?? throw new ArgumentNullException(nameof(unitOfService));
            _fetchExpression = fetchQuery ?? throw new ArgumentNullException(nameof(fetchQuery));
        }

        public EntityCollection GetData()
        {
            Tuple<string, string> queryData = ParseQueryData();

            try
            {
                if (string.IsNullOrWhiteSpace(queryData.Item1))
                    return JsonHelper.CustomJsonErrorResponse("QueryName cannot be null").ToCustomEntityCollection();
                //throw new ArgumentException(nameof(queryName));

                if (_queryDataMap.TryGetValue(queryData.Item1, out var handler))
                {
                    return handler.Invoke(queryData.Item2, _unitOfService);
                }
                return JsonHelper.CustomJsonErrorResponse($"No such method declared: {queryData.Item1}").ToCustomEntityCollection();
            }
            catch (Exception ex)
            {
                return JsonHelper.CustomJsonErrorResponse(ex.Message).ToCustomEntityCollection();
            }
        }

        private Tuple<string, string> ParseQueryData()
        {
            XDocument parsedQuery = XDocument.Parse(_fetchExpression.Query);

            Dictionary<string, string> requestParameters = parsedQuery
                .Descendants("condition")
                .Where(e =>
                    e.Attribute("attribute") != null &&
                    e.Attribute("operator") != null &&
                    e.Attribute("value") != null &&
                    string.Equals(e.Attribute("operator")?.Value, "eq", StringComparison.InvariantCultureIgnoreCase))
                .ToDictionary(e => e.Attribute("attribute")?.Value, e => e.Attribute("value")?.Value);

            if (requestParameters.TryGetValue(GlobalConfiguration.QueryName, out string queryName) &&
                !string.IsNullOrEmpty(queryName) &&
                requestParameters.TryGetValue(GlobalConfiguration.QueryParameters, out string queryParams))
            {
                queryParams = Encoding.UTF8.GetString(Convert.FromBase64String(queryParams));
                return Tuple.Create(queryName, queryParams);
            }

            return null;
        }
    }
}
