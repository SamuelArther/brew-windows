class Cli53 < Formula
  desc "Command-line tool for Amazon Route 53"
  homepage "https://github.com/barnybug/cli53"
  url "https://github.com/barnybug/cli53/archive/refs/tags/v0.9.0.tar.gz"
  sha256 "4dc4c3c552a0e045015d734d9505e120db879157cbaa3540f3090559df001ce0"
  license "MIT"
  head "https://github.com/barnybug/cli53.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "65ca6672864fd38b7c2bd1daa82f55bc84f0b4cc6580629508492afdcb643ddd"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/cli53"
  end

  test do
    assert_match "list domains", shell_output("#{bin}/cli53 help list")
  end
end
