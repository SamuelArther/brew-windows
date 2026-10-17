class Shellshare < Formula
  desc "Live Terminal Broadcast"
  homepage "https://shellshare.net"
  url "https://github.com/vitorbaptista/shellshare/archive/refs/tags/v3.12.0.tar.gz"
  sha256 "f1619e4d7c604ffe6a2eac5b65529fecf063bfa10d592a23d556067a24a69acd"
  license "Apache-2.0"
  head "https://github.com/vitorbaptista/shellshare.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a5031d027c59a91cf58fcd63b4b9c79468cef7c7b7dedc81cc5f57c5a942786f"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/shellshare --version")

    port = free_port
    pid = spawn(bin/"shellshare", "server", "--port", port.to_s)
    sleep 2
    assert_match "shellshare", shell_output("curl --silent --max-time 5 http://localhost:#{port}")
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
