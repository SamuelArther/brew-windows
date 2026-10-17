class Grafanactl < Formula
  desc "CLI to interact with Grafana"
  homepage "https://grafana.github.io/grafanactl/"
  url "https://github.com/grafana/grafanactl/archive/refs/tags/v0.1.10.tar.gz"
  sha256 "c3a3fef02f073aa92fb7b62c6f60a26e80fe794cf158f86830fe38ff053035fd"
  license "Apache-2.0"
  head "https://github.com/grafana/grafanactl.git", branch: "main"

  deprecate! date: "2026-07-17", because: :deprecated_upstream, replacement_formula: "gcx"
  disable! date: "2027-01-17", because: :deprecated_upstream, replacement_formula: "gcx"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0f8af8c68d6efeff34849d33a3bc36d51d257a8e4297c702a20a90e059130fe4"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser), "./cmd/grafanactl"

    generate_completions_from_executable(bin/"grafanactl", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/grafanactl --version").strip
    assert_match "current-context: default", shell_output("#{bin}/grafanactl config view")
  end
end
