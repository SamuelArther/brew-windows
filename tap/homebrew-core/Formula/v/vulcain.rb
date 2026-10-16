class Vulcain < Formula
  desc "Fast and idiomatic client-driven REST APIs"
  homepage "https://vulcain.rocks/"
  url "https://github.com/dunglas/vulcain/releases/download/v1.4.5/vulcain-legacy_Windows_x86_64.zip"
  sha256 "5e6959d6b613da3a3b338b534a352b8ac48e800f6bb643fa0338bc44c5ce235d"
  license "AGPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dunglas/vulcain/archive/refs/tags/v1.4.5.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/vulcain.exe", "vulcain"
  end

  test do
    port = free_port

    assert_match version.to_s, shell_output("#{bin}/vulcain version")

    (testpath/"Caddyfile").write <<~EOS
      http://127.0.0.1:#{port} {
        respond "Vulcain API"
      }
    EOS

    pid = spawn bin/"vulcain", "run", "--config", testpath/"Caddyfile"

    sleep 2

    assert_match "Vulcain API", shell_output("curl -s http://127.0.0.1:#{port}")
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
