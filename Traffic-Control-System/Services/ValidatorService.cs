namespace Traffic_Control_System.Services
{
    public interface IValidator
    {
        ValueTask<bool> Validate(string password, string streamPath);
    }
    public class ValidatorService : IValidator
    {
        public ValueTask<bool> Validate(string password, string streamPath)
        {
            var temp = streamPath == "/live/stream";
            return ValueTask.FromResult(password == "123456" && temp);
        }
    }
}
