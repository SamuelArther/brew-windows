class Shellshare < Formula
  desc "Live Terminal Broadcast"
  homepage "https://shellshare.net"
  url "https://github.com/vitorbaptista/shellshare/archive/refs/tags/v3.12.0.tar.gz"
  sha256 "f1619e4d7c604ffe6a2eac5b65529fecf063bfa10d592a23d556067a24a69acd"
  license "Apache-2.0"
  head "https://github.com/vitorbaptista/shellshare.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "dd76a49d8f3f918a5ce1dd37030f14ccaaa7dcef7d4d436de3335a2b6eb4fcde"
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
    with_test_process(bin/"shellshare", "server", "--port", port.to_s) do
      sleep 2
      assert_match "shellshare", shell_output("curl --silent --max-time 5 http://localhost:#{port}")
    end
  end
end
