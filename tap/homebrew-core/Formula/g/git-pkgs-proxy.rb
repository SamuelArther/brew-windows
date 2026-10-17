class GitPkgsProxy < Formula
  desc "Lightweight caching proxy for package registries"
  homepage "https://github.com/git-pkgs/proxy"
  url "https://github.com/git-pkgs/proxy/archive/refs/tags/v0.9.1.tar.gz"
  sha256 "7a470a221374a8ee2ea25f38ef6068096fd674fd46221bafc2ae7dbf948f7616"
  license "GPL-3.0-or-later"
  head "https://github.com/git-pkgs/proxy.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9e5bf841c345b054481ad55a579888f64b9a6375b282b225455984d9f89bf2c3"
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
