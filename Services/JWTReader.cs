using System.Security.Claims;

namespace BestStoreApi.Services
{
    public class JWTReader
    {
        public static int GetUserId(ClaimsPrincipal user)
        {
            var identity = user.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return 0;
            }

            var claim = identity.Claims.FirstOrDefault(c => c.Type.ToLower() == "id");
            if (claim == null)
            {
                return 0;
            }

            int id;

            try
            {
                id = int.Parse(claim.Value);
            }
            catch (FormatException)
            {
                return 0;
            }

            return id;
        }

        public static string GetUserRole(ClaimsPrincipal user)
        {
            var identity = user.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return "";
            }
            var claim = identity.Claims.FirstOrDefault(c => c.Type.ToLower().Contains("role"));
            if (claim == null)
            {
                return "";
            }
            return claim.Value;
        }


        public static Dictionary<string, string> GetUserClaims(ClaimsPrincipal user)
        {
            var identity = user.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return new Dictionary<string, string>();
            }
            var claims = identity.Claims.ToDictionary(c => c.Type, c => c.Value);
            return claims;
        }
    }
}
