using System;
using System.Text.Json;

namespace HSModLoader.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var mod = new Mod()
            {
                Name = "Test Mod",
                Version = "0.9.9",
                Author = "Test Author",
                HasCampaign = false,
                HasMutator = true,
                MutatorStartClass = "Nonexistent.MutatorClass"
            };

            var options = new JsonSerializerOptions();
            options.WriteIndented = true;

            var output = JsonSerializer.Serialize(mod, options);

            System.Console.WriteLine(output);
            System.Console.ReadLine();

        }
    }
}
