namespace NotesProject.User
{
    public class LoggedInResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string LoggedInUserID { get; set; } =  string.Empty;
        public string LoggedInUsername { get; set; } = string.Empty;
    }
}
