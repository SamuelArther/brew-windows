class Gof5 < Formula
  desc "F5 BIG-IP VPN client"
  homepage "https://github.com/kayrus/gof5"
  url "https://github.com/kayrus/gof5/archive/refs/tags/v0.1.5.tar.gz"
  sha256 "33356f098a81b4ffa17eb63b440675a920a8cb0319b5f3285985b58f88973fed"
  license "Apache-2.0"
  head "https://github.com/kayrus/gof5.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c51c96611550dcddbb82d02088aea2b70a748a80a1ea4b481f29dfe74437847b"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=#{version}"), "./cmd/gof5"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gof5 --version")

    output = shell_output("#{bin}/gof5 --profile-index=-1 2>&1", 1)
    assert_match "profile-index cannot be negative", output
  end
end
