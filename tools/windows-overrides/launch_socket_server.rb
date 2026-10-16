class LaunchSocketServer < Formula
  desc "Proxy a public port to a server that doesn't bind it itself"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "launch_socket_server", "launch_socket_server"
  end

  def caveats
    <<~EOS
      Windows has no privileged ports, so launch_socket_server binds LAUNCH_SOCKET_ADDRESS
      (default 127.0.0.1:80) itself and proxies to LAUNCH_PROGRAM_TCP_ADDRESS.
    EOS
  end

  test do
    assert_includes shell_output("#{bin}/launch_socket_server 2>&1; true"), "usage: launch_socket_server"
    assert_match "LAUNCH_PROGRAM_TCP_ADDRESS must be set", shell_output("#{bin}/launch_socket_server - 2>&1", 2)
    with_env(LAUNCH_PROGRAM_TCP_ADDRESS: "127.0.0.1:#{free_port}") do
      assert_match "invalid address 'nonsense'", shell_output("#{bin}/launch_socket_server -l nonsense - 2>&1", 2)
    end
  end
end
