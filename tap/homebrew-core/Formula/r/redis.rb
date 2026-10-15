class Redis < Formula
  desc "Persistent key-value database"
  homepage "https://github.com/redis-windows/redis-windows"
  url "https://github.com/redis-windows/redis-windows/releases/download/8.10.2/Redis-8.10.2-Windows-x64-msys2.zip"
  sha256 "7c8cebd50347eaa1d9e784da842ed47a4f33394637835531d6614777b950ee85"
  license "AGPL-3.0-only"

  livecheck do
    url :stable
    strategy :github_latest
  end

  # Community MSYS2-based Redis Windows port; not an official Redis Windows build.
  # No service is registered or started.

  def install
    install_tree
    windows_exe "libexec/redis-server.exe", "redis-server"
    windows_exe "libexec/redis-cli.exe", "redis-cli"
    windows_exe "libexec/redis-benchmark.exe", "redis-benchmark"
  end

  test do
    system bin/"redis-server", "--test-memory", "2"
    assert_match version.to_s, shell_output("#{bin}/redis-server --version")
    assert_match version.to_s, shell_output("#{bin}/redis-cli --version")
  end
end
