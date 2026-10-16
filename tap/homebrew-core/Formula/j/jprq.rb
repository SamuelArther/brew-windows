class Jprq < Formula
  desc "Join Public Router, Quickly"
  homepage "https://jprq.io/"
  url "https://github.com/azimjohn/jprq/archive/refs/tags/2.4.tar.gz"
  sha256 "a3fc5a804851129c79a02deb3e5b7f5b84c0f351d688ca0088b571407399ff30"
  license "BSD-3-Clause"
  head "https://github.com/azimjohn/jprq.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a01f08ce9a6c898311eb6c5441706251de3387ba4d1099f89957cb5b75f840be"
  end

  depends_on "go" => :build

  # `test do` block starts a local server and contacts the jprq service
  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}"), "./cli"
  end

  test do
    assert_match "auth token has been set", shell_output("#{bin}/jprq auth jprqbolmagin 2>&1")
    output = shell_output("#{bin}/jprq serve #{testpath} 2>&1", 1)
    assert_match "jprq is now invite-only service", output

    assert_match version.to_s, shell_output("#{bin}/jprq --version 2>&1")
  end
end
