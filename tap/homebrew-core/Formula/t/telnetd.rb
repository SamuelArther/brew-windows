class Telnetd < Formula
  desc "TELNET server with a pseudoconsole shell"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "telnetd", "telnetd"
  end

  def caveats
    <<~EOS
      Windows has no inetd: run it standalone with `telnetd -debug [port]` (add `-b 127.0.0.1`
      to keep it local). It only logs in the account that started it. TELNET sends passwords
      in the clear; use OpenSSH Server for anything that leaves this machine.
    EOS
  end

  test do
    assert_match "usage: telnetd", shell_output("#{bin}/telnetd usage 2>&1", 1)
    port = free_port
    pid = spawn bin/"telnetd", "-debug", port.to_s, "-b", "127.0.0.1"
    begin
      sleep 2
      assert_match "login:", shell_output("curl -s -m 3 telnet://127.0.0.1:#{port} </dev/null", 28)
    ensure
      Process.kill("TERM", pid)
    end
  end
end
