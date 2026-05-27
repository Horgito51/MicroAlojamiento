namespace Alojamiento.API.Models.Requests.Public
{
    public static class PublicRequestGuard
    {
        public static void RejectUnsupportedQueryParameters(
            IQueryCollection query,
            IReadOnlySet<string> allowedParameters)
        {
            foreach (var key in query.Keys)
            {
                if (!allowedParameters.Contains(key))
                {
                    throw new global::Alojamiento.Business.Exceptions.ValidationException(
                        "PUB-QUERY-001",
                        $"El parametro '{key}' no esta soportado por este endpoint.");
                }
            }
        }

        public static bool IsIdProperty(string key)
            => key.Equals("id", StringComparison.OrdinalIgnoreCase)
               || key.StartsWith("id", StringComparison.OrdinalIgnoreCase);
    }
}
