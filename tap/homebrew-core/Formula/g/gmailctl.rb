class Gmailctl < Formula
  desc "Declarative configuration for Gmail filters"
  homepage "https://github.com/mbrt/gmailctl"
  url "https://github.com/mbrt/gmailctl/archive/refs/tags/v0.13.0.tar.gz"
  sha256 "49df3a2fe7929114ec406096d577004a27ad75bbb4c836ca603dff88c1a830bd"
  license "MIT"
  head "https://github.com/mbrt/gmailctl.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "446c6a0d4bf4244268f9b0949fc3368208a6ce5ae215111bf26c4a2026e6d47a"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/mbrt/gmailctl/cmd/gmailctl/cmd.version=#{version}]
    system "go", "build", *std_go_args(ldflags:), "cmd/gmailctl/main.go"

    generate_completions_from_executable(bin/"gmailctl", shell_parameter_format: :cobra)
  end

  test do
    assert_includes shell_output("#{bin}/gmailctl init --config #{testpath} 2>&1", 1),
      "The credentials are not initialized"

    assert_match version.to_s, shell_output("#{bin}/gmailctl version")
  end
end
