using Newtonsoft.Json;

namespace LogicApps.Portals.Helpers
{
    public static class JsonHelper
    {
        public static string CustomJsonErrorResponse(string errorMessage)
        {
            return JsonConvert.SerializeObject(new
            {
                Status = false,
                Error = errorMessage
            });
        }
    }
}
