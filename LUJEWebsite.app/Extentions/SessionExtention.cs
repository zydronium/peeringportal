using System.Text.Json;

namespace LUJEWebsite.app.Extentions
{
    public static class SessionExtention
    {
        // extension method on ISession 
        // how to use: HttpContext.Session.Set<T>(key, value) 
        public static void Set<T>(this ISession session, string key, T value)
        {

            session.SetString(key, JsonSerializer.Serialize(value));

        }

        // extension method on ISession 
        // how to use: HttpContext.Session.Get<T>(key) 
        public static T Get<T>(this ISession session, string key)
        {

            var value = session.GetString(key);

            return value == null ? default : JsonSerializer.Deserialize<T>(value);

        }
    }
}
