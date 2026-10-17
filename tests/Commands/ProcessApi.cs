using System.Diagnostics;

// Ruby's process API in `test do` blocks: spawn/exec/fork, Process.wait/kill, Open3, IO.popen, IO.pipe,
// $stdout.reopen, PTY.spawn (ConPTY). The children are this test program itself in its `--child` modes.
static class ProcessApi
{
    // `Commands.exe --child <mode> ...`: small, harmless child programs.
    public static void Child(string[] a)
    {
        switch (a[0])
        {
            case "echo": Console.WriteLine(string.Join(" ", a[1..])); break;
            case "exit": Environment.Exit(int.Parse(a[1]));
                break;
            case "both": Console.Out.WriteLine("o"); Console.Out.Flush(); Thread.Sleep(50); Console.Error.WriteLine("e"); break;
            case "cat":
                string line;
                while ((line = Console.In.ReadLine()) != null) { Console.WriteLine(line); Console.Out.Flush(); }
                break;
            case "env": Console.WriteLine(Environment.GetEnvironmentVariable(a[1]) ?? "(unset)"); break;
            case "cwd": Console.WriteLine(Path.GetFileName(Directory.GetCurrentDirectory())); break;
            case "sleep":
                Console.WriteLine("ready");
                Console.Out.Flush();
                Thread.Sleep(int.Parse(a[1]) * 1000);
                break;
            // A child that starts a grandchild (both sleep): killing the child must take the grandchild too.
            case "tree":
                var g = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--child sleep 120") { UseShellExecute = false, RedirectStandardOutput = true })!;
                File.WriteAllText(a[1], g.Id.ToString());
                Thread.Sleep(120_000);
                break;
            // Under PTY.spawn: is stdout a console, then echo one typed line.
            case "console":
                Console.WriteLine($"ready tty={!Console.IsOutputRedirected && !Console.IsInputRedirected}");
                var typed = Console.ReadLine();
                Console.WriteLine($"got: {typed}");
                break;
        }
    }

    public static int Run(string root)
    {
        Directory.CreateDirectory(root);
        var helper = Environment.ProcessPath!.Replace('\\', '/');
        var count = 0;
        void Test(string name, string body)
        {
            var formula = Path.Combine(root, name + ".rb");
            File.WriteAllText(formula, $"class {char.ToUpperInvariant(name[0]) + name[1..]} < Formula\n  url \"https://example.com/x-1.0.tar.gz\"\n  version \"1.0\"\n" +
                $"  test do\n    helper = \"{helper}\"\n{body}\n  end\nend\n");
            var watch = Stopwatch.StartNew();
            try { Brew.Ruby.FormulaLoader.LoadSpec(formula).RunTest(Path.Combine(root, name + "-prefix"), null, Path.Combine(root, name + "-test")); }
            catch (Exception e) { throw new Exception($"FAIL process API test {name}: {e.Message}", e); }
            if (watch.Elapsed > TimeSpan.FromSeconds(60)) throw new Exception($"process API test {name} took {watch.Elapsed}");
            count++;
        }

        Test("spawning", """
            pid = spawn(helper, "--child", "echo", "hi", out: "spawn.txt")
            assert_equal pid, Process.wait(pid)
            assert_predicate $?, :success?
            assert_equal "hi\n", File.read("spawn.txt")
            pid = Process.spawn({ "PROCVAR" => "set" }, helper, "--child", "env", "PROCVAR", [:out, :err] => ["env.txt", "w"])
            Process.wait pid
            assert_equal "set\n", File.read("env.txt")
            # [:out, :err] => file opens it once: both streams land in it.
            Process.wait spawn(helper, "--child", "both", [:out, :err] => "both.log")
            assert_equal "o\ne\n", File.read("both.log")
            mkdir "sub"
            Process.wait spawn(helper, "--child", "cwd", chdir: "sub", out: "cwd.txt")
            assert_equal "sub\n", File.read("cwd.txt")
            Process.wait spawn(helper, "--child", "exit", "3")
            assert_equal 3, $?.exitstatus
            refute_predicate $?, :success?
            # One string goes through the same shell resolution as `system`.
            Process.wait spawn("#{helper} --child echo via line", out: "line.txt", err: File::NULL)
            assert_equal "via line\n", File.read("line.txt")
            pid, status = Process.wait2(spawn(helper, "--child", "exit", "0"))
            assert_equal 0, status.exitstatus
            assert_equal pid, status.pid
            assert_raises(Errno::ECHILD) { Process.wait(pid) }
            assert_raises(Errno::ENOENT) { spawn("no-such-program-here", "x") }
            """);

        Test("killing", """
            pid = spawn(helper, "--child", "sleep", "60", out: File::NULL)
            assert_nil Process.wait(pid, Process::WNOHANG)
            assert_equal 1, Process.kill("TERM", pid)
            Process.wait(pid)
            assert_predicate $?, :signaled?
            assert_equal Signal.list.fetch("TERM"), $?.termsig
            assert_nil $?.exitstatus
            assert_raises(Errno::ESRCH) { Process.kill("TERM", pid) }
            # Killing a child takes the processes it started.
            child = spawn(helper, "--child", "tree", (testpath/"grandchild.pid").to_s)
            sleep 0.1 until (testpath/"grandchild.pid").exist? && !(testpath/"grandchild.pid").read.empty?
            grandchild = (testpath/"grandchild.pid").read.to_i
            Process.kill("KILL", child)
            Process.wait child
            sleep 0.5
            assert_raises(Errno::ESRCH) { Process.kill(0, grandchild) }
            # Timeout.timeout still fires while Process.wait waits.
            pid = spawn(helper, "--child", "sleep", "60", out: File::NULL)
            assert_raises(Timeout::Error) { Timeout.timeout(0.5) { Process.wait(pid) } }
            Process.kill(9, pid)
            Process.wait pid
            # Left running on purpose: the end of the test kills it.
            File.write("leftover.pid", spawn(helper, "--child", "sleep", "120", out: File::NULL).to_s)
            assert_kind_of Float, Process.clock_gettime(Process::CLOCK_MONOTONIC)
            assert_kind_of Integer, Process.clock_gettime(Process::CLOCK_MONOTONIC, :millisecond)
            """);
        var leftover = int.Parse(File.ReadAllText(Path.Combine(root, "killing-test", "leftover.pid")));
        try
        {
            using var p = Process.GetProcessById(leftover);
            if (!p.WaitForExit(5000)) throw new Exception("FAIL a child the test left running outlived it");
        }
        catch (ArgumentException) { }
        count++;

        Test("openthree", """
            out, err, status = Open3.capture3(helper, "--child", "both")
            assert_equal ["o\n", "e\n", true], [out, err, status.success?]
            out, status = Open3.capture2(helper, "--child", "cat", stdin_data: "a\nb\n")
            assert_equal "a\nb\n", out
            assert_equal 0, status.exitstatus
            out, = Open3.capture2e(helper, "--child", "both")
            assert_equal "o\ne\n", out
            Open3.popen3(helper, "--child", "cat") do |stdin, stdout, stderr, wait_thr|
              stdin.puts "hello"
              assert_equal "hello\n", stdout.gets
              assert_kind_of Integer, wait_thr.pid
              assert_equal wait_thr.pid, wait_thr[:pid]
              stdin.close
              assert_equal "", stdout.read
              assert_predicate wait_thr.value, :success?
            end
            stdin, stdout, wait_thr = Open3.popen2(helper, "--child", "sleep", "60")
            assert_equal "ready\n", stdout.gets("\n")
            assert_predicate wait_thr, :alive?
            Process.kill("TERM", wait_thr.pid)
            assert_predicate wait_thr.value, :signaled?
            refute_predicate wait_thr, :alive?
            Open3.popen2e({ "PROCVAR" => "e" }, helper, "--child", "env", "PROCVAR") do |_, out, thr|
              assert_equal "e\n", out.read
              thr.join
            end
            """);

        Test("popen", """
            assert_equal "x\ny\n", IO.popen([helper, "--child", "cat"], "r+") { |io| io.puts "x"; io.write "y\n"; io.close_write; io.read }
            assert_predicate $?, :success?
            lines = IO.popen("#{helper} --child both", err: [:child, :out], &:readlines)
            assert_equal ["o\n", "e\n"], lines
            io = IO.popen([helper, "--child", "sleep", "60"])
            assert_equal "ready\n", io.readline
            Process.kill("KILL", io.pid)
            io.close
            assert_predicate $?, :signaled?
            IO.popen([helper, "--child", "exit", "5"]) { |p| p.read }
            assert_equal 5, $?.exitstatus
            """);

        Test("pipes", """
            r, w = IO.pipe
            pid = spawn(helper, "--child", "echo", "through a pipe", out: w)
            w.close
            assert_equal "through a pipe\n", r.read
            Process.wait pid
            # A pipe end given to one child doesn't leak into another (that would hide EOF).
            read, write = IO.pipe
            sleeper = spawn(helper, "--child", "sleep", "60", out: File::NULL)
            Process.wait spawn(helper, "--child", "echo", "eof", out: write)
            write.close
            assert_equal "eof\n", Timeout.timeout(10) { read.read }
            Process.kill("KILL", sleeper)
            Process.wait sleeper
            sender_read, sender_write = IO.pipe
            out_read, out_write = IO.pipe
            pid = spawn(helper, "--child", "cat", in: sender_read, out: out_write, err: out_write)
            sender_read.close
            out_write.close
            sender_write.puts "foobar"
            assert_equal "foobar\n", out_read.gets
            assert_equal :wait_readable, out_read.read_nonblock(10, exception: false)
            assert_raises(IO::WaitReadable) { out_read.read_nonblock(10) }
            assert_nil out_read.wait_readable(0.2)
            sender_write.close
            Process.wait pid
            assert out_read.eof?
            IO.pipe do |pr, pw|
              pw.write "abc"
              pw.close
              assert_equal ["a", "b"], [pr.getc, pr.read(1)]
              assert_equal "c", pr.readpartial(10)
              assert_raises(EOFError) { pr.readpartial(10) }
            end
            """);

        Test("forking", """
            read, write = IO.pipe
            pid = fork do
              exec helper, "--child", "echo", "forked", out: write
            end
            assert_equal "forked\n", read.gets
            Process.wait pid
            assert_predicate $?, :success?
            pid = fork do
              $stdout.reopen(testpath/"fork.log")
              $stderr.reopen(testpath/"fork.log")
              exec helper, "--child", "both"
            end
            Process.wait pid
            assert_equal "o\ne\n", (testpath/"fork.log").read
            # A daemon started by fork, killed like on Linux.
            pid = fork { exec helper, "--child", "sleep", "60" }
            Process.kill("TERM", pid)
            Process.wait pid
            assert_predicate $?, :signaled?
            pid = fork { system helper, "--child", "sleep", "60", out: File::NULL }
            sleep 0.3
            Process.kill("TERM", pid)
            Timeout.timeout(10) { Process.wait pid }
            # Ruby code in the child runs alongside the parent's.
            seen = []
            pid = fork do
              3.times { |n| seen << n; sleep 0.05 }
              exit 4
            end
            sleep 0.01 while seen.empty?
            Process.wait pid
            assert_equal [[0, 1, 2], 4], [seen, $?.exitstatus]
            # The child's ENV and cwd are its own.
            ENV["FORKVAR"] = "parent"
            Process.wait(fork { ENV["FORKVAR"] = "child"; Dir.chdir(testpath/"..") })
            assert_equal "parent", ENV["FORKVAR"]
            assert_equal testpath.to_s, Dir.pwd
            pid = fork { raise "boom" }
            Process.wait pid
            assert_equal 1, $?.exitstatus
            t = Thread.new(20) { |n| sleep 0.05; n + 1 }
            assert_equal 21, t.value
            Thread.current[:mark] = 1
            assert_equal 1, Thread.current[:mark]
            # $stdout.reopen in the parent: spawned children inherit it too.
            File.open("parent.log", "w") do |file|
              $stdout.reopen(file)
              Process.wait spawn(helper, "--child", "echo", "inherited")
            end
            assert_equal "inherited\n", File.read("parent.log")
            """);

        // A shell script named by a plain command string runs through the POSIX shell (its #! line).
        if (Brew.Ruby.PosixShell.FindShell(Environment.GetEnvironmentVariable) != null)
            Test("shebang", """
                File.write("start.sh", "#!/bin/sh\necho started $1\n")
                read, write = IO.pipe
                pid = fork { exec "#{testpath}/start.sh go", out: write }
                assert_equal "started go\n", read.gets
                Process.wait pid
                """);

        Test("pseudoterminal", """
            require "pty"
            require "expect"
            PTY.spawn(helper, "--child", "console") do |r, w, pid|
              r.winsize = [30, 100]
              assert_equal [30, 100], r.winsize
              refute_nil r.expect("ready tty=True", 15)
              w.puts "typed"
              result = r.expect(/got: (\w+)\r?\n/, 15)
              assert_equal "typed", result[1]
              Process.wait pid
              assert_predicate $?, :success?
              begin
                r.read
                r.readline
                flunk "expected EIO at the end"
              rescue Errno::EIO
                # GNU/Linux raises EIO when read is done on closed pty
              end
            end
            r, _w, pid = PTY.spawn("#{helper} --child echo pty line")
            output = +""
            begin
              r.each_line { |line| output << line }
            rescue Errno::EIO
              nil
            end
            Process.wait pid
            assert_match "pty line", output
            assert_nil PTY.check(spawn(helper, "--child", "sleep", "60", out: File::NULL))
            PTY.spawn(helper, "--child", "sleep", "60") do |r, _w, pid|
              refute_nil r.expect("ready", 15)
              Process.kill("INT", pid)
              Timeout.timeout(10) { Process.wait pid }
              assert_equal Signal.list["INT"], $?.termsig
            end
            pid = PTY.spawn(helper, "--child", "echo", "redirected", [:out, :err] => "pty.log").last
            Process.wait pid
            assert_equal "redirected", File.read("pty.log").strip
            """);
        return count;
    }
}
