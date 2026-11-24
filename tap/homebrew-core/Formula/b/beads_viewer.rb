class BeadsViewer < Formula
  desc "Terminal-based UI for the Beads issue tracker"
  homepage "https://github.com/Dicklesworthstone/beads_viewer"
  url "https://github.com/Dicklesworthstone/beads_viewer/archive/refs/tags/v0.28.0.tar.gz"
  sha256 "171c5506d60fd1f90385f3b2eee1388d6e2395a4af5c950d57a3d7d1c0157a99"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "246c92625928a60e1ef0ed44d30aaaeb606c17707af3bcd3149bacd8a2c098b2"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/Dicklesworthstone/beads_viewer/pkg/version.version=v#{version}]
    system "go", "build", *std_go_args(ldflags:, output: bin/"bv"), "./cmd/bv"
    generate_completions_from_executable(bin/"bv", shell_parameter_format: :cobra)
  end

  test do
    assert_match "v#{version}", shell_output("#{bin}/bv --version")

    # Test that it detects missing .beads directory.
    output = shell_output("#{bin}/bv --robot-insights 2>&1", 1)
    assert_match "failed to read beads directory", output
  end
end
