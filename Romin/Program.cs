using Romin;

// Create the Romin virtual machine host and a module
// that will contain the script environment.
var host = new Romin.VMHost();
var module = new Romin.Module();

// Run as a command-line task.
// The first argument is the script file name.
// All following arguments are passed to the script
// as variables named arg1, arg2, arg3, etc.
if (args.Length > 0)
{
    string file = args[0];

    // Read the script source and create a parser for it.
    var p = host.GetParser(File.ReadAllText(file), file);

    // Add command-line arguments to the module environment.
    if (args.Length > 1)
    {
        int i = 1;
        foreach (string s in args.Skip(1))
        {
            // Store each command-line argument as arg1, arg2, etc.
            module.Env.Add("arg" + i, new Value(s));
            i++;
        }
    }

    // Parse the script using the existing module
    // and execute the resulting bytecode.
    host.Run(p.Parse(module));
}
else
{
    // No script file was specified.
    // Start the interactive Romin execution loop.
    while (true)
    {
        try
        {
            string file = "main.rn";

            // If main.rn exists, compile and execute it.
            // This provides a default entry point for a Romin application.
            if (File.Exists(file))
            {
                var m = host.Compile(file);

                // Create a virtual machine and run the compiled module.
                var vm = new VM(host);
                vm.Run(m);

                // Wait before starting the next execution cycle.
                Console.ReadLine();
            }
            else
            {
                // If main.rn does not exist, run in interactive mode.
                // Read one line from the console, parse it and execute it.
                module.Code.Clear();
                var p = host.GetParser(Console.ReadLine());
                host.Run(p.Parse(module));
            }
        }
        catch (Exception e)
        {
            // Catch parsing and runtime errors so that the host
            // remains running instead of terminating the process.
            Console.Write(e.Message);
            Console.ReadLine();
        }
    }
}