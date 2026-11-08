namespace RaceDayMVC.Helpers
{
    public static class AuthCheck
    {
        public static bool IsLoggedIn(ISession s) =>
            !string.IsNullOrEmpty(s.GetString("JwtToken"));
        public static bool IsOrganiser(ISession s) =>
            s.GetString("Role") == "Organiser";
        public static bool IsParticipant(ISession s) =>
            s.GetString("Role") == "Participant";
        public static int CurrentUserId(ISession s) =>
            s.GetInt32("UserId") ?? 0;
        public static string CurrentFullName(ISession s) =>
            s.GetString("FullName") ?? "";
    }
}
