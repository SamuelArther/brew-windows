class GitPkgsProxy < Formula
  desc "Lightweight caching proxy for package registries"
  homepage "https://github.com/git-pkgs/proxy"
  url "https://github.com/git-pkgs/proxy/archive/refs/tags/v0.10.0.tar.gz"
  sha256 "b07335586199df429dac55c0bfc2d1a3d9489ada00c8c1583817ffa491722189"
  license "GPL-3.0-or-later"
  head "https://github.com/git-pkgs/proxy.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f1901125269f8e847b715160e8b01ee98029d74d51b90a6357935be6378ef6fa"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X main.Version=#{version}
      -X main.Commit=#{tap.user}
    ]
    system "go", "build", *std_go_args(ldflags:, output: bin/"proxy"), "./cmd/proxy"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/proxy -version")

    output = shell_output("#{bin}/proxy stats 2>&1", 1)
    assert_match "database not found: ./cache/proxy.db", output
  end
end
