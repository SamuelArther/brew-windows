class Ferron < Formula
  desc "Fast, memory-safe web server written in Rust"
  homepage "https://www.ferronweb.org/"
  url "https://github.com/ferronweb/ferron/archive/refs/tags/2.8.1.tar.gz"
  sha256 "32354ffed4fbe8caede924d420e6a7ac84127f264c47e1e738b462d9479c0680"
  license "MIT"
  head "https://github.com/ferronweb/ferron.git", branch: "develop-2.x"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3b0b5c551e6a100f43e5c890502378cd384c3d58fcbff71c14f108c4a72e2076"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "ferron")
  end

  test do
    port = free_port

    (testpath/"ferron.yaml").write "global: {\"port\":#{port}}"
    begin
      pid = spawn bin/"ferron", "-c", testpath/"ferron.yaml"
      sleep 3
      assert_match "The requested resource wasn't found", shell_output("curl -s 127.0.0.1:#{port}")
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
