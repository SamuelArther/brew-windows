class Gcx < Formula
  desc "CLI for managing Grafana Cloud resources"
  homepage "https://github.com/grafana/gcx"
  url "https://github.com/grafana/gcx/archive/refs/tags/v1.7.1.tar.gz"
  sha256 "9b5ec00327d457f0acba20c301e94d2e1085448da60b500d58dd9a8d415a18c8"
  license "Apache-2.0"
  head "https://github.com/grafana/gcx.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "76ef1115c6819a0654002f3ec4f8cc181e1fb47d04252db09255275196be459c"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser), "./cmd/gcx"

    generate_completions_from_executable(bin/"gcx", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gcx --version")

    system bin/"gcx", "config", "set", "stacks.test.grafana.server", "https://grafana.example.net"
    assert_match "https://grafana.example.net", shell_output("#{bin}/gcx config view")

    assert_match "unknown output format", shell_output("#{bin}/gcx commands --output bogus 2>&1", 1)
  end
end
