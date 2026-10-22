class Dexidp < Formula
  desc "OpenID Connect Identity and OAuth 2.0 Provider"
  homepage "https://dexidp.io"
  url "https://github.com/dexidp/dex/archive/refs/tags/v2.46.1.tar.gz"
  sha256 "05e2e149b53f9876269362fefa9061b4b1abd3d5437f23247d79e44a77ac487d"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6aae9b9700787f2e5165d8477e35f1e68b5e6a4648a2db593993c8fe87837d09"
  end

  depends_on "go" => :build

  conflicts_with "dex", because: "both install `dex` binaries"

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-w -X main.version=#{version}"
    system "go", "build", *std_go_args(ldflags:, output: bin/"dex"), "./cmd/dex"
    pkgetc.install "config.yaml.dist" => "config.yaml"
  end

  service do
    run [opt_bin/"dex", "serve", etc/"dexidp/config.yaml"]
    keep_alive true
    error_log_path var/"log/dex.log"
    log_path var/"log/dex.log"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dex version")

    port = free_port
    cp pkgetc/"config.yaml", testpath
    inreplace "config.yaml", "5556", port.to_s

    pid = spawn bin/"dex", "serve", "config.yaml"
    sleep 3

    assert_match "Dex", shell_output("curl -s localhost:#{port}/dex")
  ensure
    Process.kill "TERM", pid
  end
end
