class Buildkite < Formula
  desc "Work with Buildkite from the command-line"
  homepage "https://github.com/buildkite/cli"
  url "https://github.com/buildkite/cli/archive/refs/tags/v3.69.0.tar.gz"
  sha256 "96681b1a8772a04e28318af0e969dac279339165e05d238e8d05ad15f13cb43d"
  license "MIT"
  head "https://github.com/buildkite/cli.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "29c3e875a6f2afa41d5e5872cdf11d71dacb03b19253d5a1f31289d8e437e1e9"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-s -w -X github.com/buildkite/cli/v3/cmd/version.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "."
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/buildkite --version")

    system bin/"buildkite", "config", "set", "output_format", "yaml", "--local"
    assert_equal "yaml", shell_output("#{bin}/buildkite config get output_format").strip
  end
end
