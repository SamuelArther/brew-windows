class Lume < Formula
  desc "Create and manage virtual machines on Hyper-V"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "lume", "lume"
  end

  def caveats
    <<~EOS
      lume drives Hyper-V, so it needs Windows Pro, Enterprise or Education with the Microsoft-Hyper-V
      feature, and an administrator prompt (or membership in Hyper-V Administrators).
      Guests are Linux or Windows installed from an ISO: lume run <name> --mount <iso>.
    EOS
  end

  test do
    output = shell_output("#{bin}/lume setup does-not-exist --unattended tahoe 2>&1", 1)
    assert_match(/Virtual machine not found|Hyper-V/, output)
    assert_match(/No virtual machines found|name\s+os\s+cpu|Hyper-V/, shell_output("#{bin}/lume ls 2>&1", nil))
    assert_match "invalid size '8XB'", shell_output("#{bin}/lume create vm --memory 8XB 2>&1", 2)
    assert_match "Apple silicon", shell_output("#{bin}/lume ipsw 2>&1", 1)
    port = free_port
    pid = spawn bin/"lume", "serve", "--port", port.to_s
    sleep 5
    begin
      assert_match %r{^HTTP/\d(.\d)? (200|404)}, shell_output("curl -si localhost:#{port}/lume").lines.first
    ensure
      Process.kill "TERM", pid
    end
  end
end
