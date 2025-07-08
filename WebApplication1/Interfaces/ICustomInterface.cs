namespace WebApplication1.Interfaces
{
    public interface ICustomInterface
    {
        Task<string> DoSomethings(string input);
    }

    public class CustomImplementation : ICustomInterface
    {
        public Task<string> DoSomethings(string input)
        {
            var result = $"Processed: {input}";
            Console.WriteLine(result);
            return Task.FromResult(result);
        }
    }
}
