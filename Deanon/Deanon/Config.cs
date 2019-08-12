namespace Deanon.Configuration
{
    class Config
    {
        public Vk VK { get; set; }
        public Db DB { get; set; }
        public Depth Depth { get; set; }
        public Stages Stages { get; set; }
        public Exec Exec { get; set; }
    }
    public class Exec
    {
        public int UserId { get; set; }
        public bool ContinueFromSavePoint { get; set; }
        public bool CompleteRelations { get; set; }
        public int Expansions { get; set; }
    }
    public class Stages
    {
        public bool Small { get; set; }
        public bool Big { get; set; }
    }
    public class Depth
    {
        public int Friends { get; set; }
        public int Followers { get; set; }
        public int Post { get; set; }
        public int Comments { get; set; }
        public int Likes { get; set; }
    }
    class Vk
    {
        public string Token { get; set; }
    }
    public class Db
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
