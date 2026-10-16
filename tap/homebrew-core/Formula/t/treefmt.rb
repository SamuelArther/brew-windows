class Treefmt < Formula
  desc "One CLI to format the code tree"
  homepage "https://treefmt.com/latest/"
  url "https://github.com/numtide/treefmt/archive/refs/tags/v2.6.0.tar.gz"
  sha256 "90993f858b376c0a0ca49920b9679dc774107a96fad8ffc3808809c6a82f4ece"
  license "MIT"
  head "https://github.com/numtide/treefmt.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "38afbbfe61ed51e32add2e56bf5a2c6a2b0a47e868489fee460b2595912caf3d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/numtide/treefmt/v2/build.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    output = shell_output("#{bin}/treefmt 2>&1", 1)
    assert_match "failed to find treefmt config file: could not find " \
                 "[treefmt.toml .treefmt.toml .config/treefmt.toml]",
                 output
    assert_match version.to_s, shell_output("#{bin}/treefmt --version")
  end
end
