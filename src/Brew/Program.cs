using Brew;
using System.Text;

Console.OutputEncoding = new UTF8Encoding(false);
return await Cli.RunAsync(args);
