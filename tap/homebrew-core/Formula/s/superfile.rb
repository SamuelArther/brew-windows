class Superfile < Formula
  desc "Modern and pretty fancy file manager for the terminal"
  homepage "https://superfile.dev/"
  url "https://github.com/yorukot/superfile/archive/refs/tags/v1.6.1.tar.gz"
  sha256 "cdca43cebee4b302104274084d67de31e244b4add39a247a76ab2ae3b255e7d6"
  license "MIT"

  # Upstream creates releases that use a stable tag (e.g., `v1.2.3`) but are
  # labeled as "pre-release" on GitHub before the version is released, so it's
  # necessary to use the `GithubLatest` strategy.
  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "37a4f3356d913a2f8047101bcac84a987e223e7996ad695073b99b940823a2b1"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(output: bin/"spf")
  end

  test do
    # superfile is a GUI application
    assert_match version.to_s, shell_output("#{bin}/spf -v")
  end
end
