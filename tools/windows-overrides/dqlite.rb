class Dqlite < Formula
  desc "Embeddable, replicated and fault-tolerant SQLite-powered engine"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "dqlite-demo", "dqlite-demo"
    windows_exe "libexec/wintools.exe", "dqlite", "dqlite"
  end

  test do
    # Raft over an in-memory network: elections, replication, failover, catch-up and handover.
    assert_match "ok: the data survives on the new leader", shell_output("#{bin}/dqlite-demo --self-test")

    lo = "127.0.0.1"
    # cluster of 2 instances, 1 db and 1 API port each
    api1 = free_port
    db1 = free_port
    api2 = free_port
    db2 = free_port
    pid1 = spawn bin/"dqlite-demo", "--db", "#{lo}:#{db1}", "--api", "#{lo}:#{api1}", "--dir", testpath/"n1"
    sleep 3
    pid2 = spawn bin/"dqlite-demo", "--db", "#{lo}:#{db2}", "--api", "#{lo}:#{api2}",
                 "--join", "#{lo}:#{db1}", "--dir", testpath/"n2"
    sleep 3
    begin
      assert_match "done", shell_output("curl -s -X PUT -d my-value http://#{lo}:#{api1}/my-key")
      sleep 1
      assert_match "my-value", shell_output("curl -s http://#{lo}:#{api2}/my-key")
      assert_match "my-value", pipe_output("#{bin}/dqlite -s #{lo}:#{db2} demo", "SELECT value FROM model;\n")
    ensure
      Process.kill("TERM", pid1)
      Process.kill("TERM", pid2)
    end
  end
end
