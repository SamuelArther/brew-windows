class Fakecloud < Formula
  desc "Free, open-source local AWS cloud emulator for integration testing"
  homepage "https://fakecloud.dev/"
  url "https://github.com/faiscadev/fakecloud/archive/refs/tags/v0.52.0.tar.gz"
  sha256 "6c1f227ed9cb60e5aad9bce3b383c3e8b69bd76e35ea51992cf4cd25a5f81d01"
  license "AGPL-3.0-or-later"
  head "https://github.com/faiscadev/fakecloud.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "25b46a0e9645191a41496ca819e8fcdb8c8e4ccf9a1caf3f6361001f1a646483"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  uses_from_macos "bzip2"
  uses_from_macos "xz"

  on_linux do
    depends_on "openssl@4"
    depends_on "zlib-ng-compat"
  end

  # Test binds and queries a local fakecloud server
  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/fakecloud-server")
  end

  service do
    run [opt_bin/"fakecloud"]
    keep_alive true
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fakecloud --version")

    port = free_port
    pid = spawn bin/"fakecloud", "--addr", "127.0.0.1:#{port}"
    begin
      sleep 3
      output = shell_output("curl -s http://127.0.0.1:#{port}/_fakecloud/health 2>&1")
      assert_match "ok", output.downcase
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
