class Ccheck < Formula
  desc "Check X509 certificate expiration from the command-line, with TAP output"
  homepage "https://github.com/nerdlem/ccheck"
  url "https://github.com/nerdlem/ccheck/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "2325ea8476cce5111b8f848ca696409092b1d1d9c41bd46de7e3145374ed32cf"
  license "GPL-2.0-or-later"
  head "https://github.com/nerdlem/ccheck.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b236759a2f13a8716f667d11ce645aa51e65c843f2ce6e6f76337a6868abdf41"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
    # go.sum lacks cobra's Windows-only dependency.
    system "go", "get", "github.com/inconshreveable/mousetrap" if OS.windows?
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_equal "brew-ccheck.libertad.link:443 TLS",
    shell_output("#{bin}/ccheck brew-ccheck.libertad.link:443").strip
  end
end
