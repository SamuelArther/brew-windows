class Dexidp < Formula
  desc "OpenID Connect Identity and OAuth 2.0 Provider"
  homepage "https://dexidp.io"
  url "https://github.com/dexidp/dex/archive/refs/tags/v2.46.0.tar.gz"
  sha256 "3c09c0f2be88719d5bc2a061d3df9310052c8220b651a3b72d774eecd9b50b65"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d08ba09dbaa3996b6c3d0887aa5593ff0b1a55fb7fbaea6a92b31080c157d47a"
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
