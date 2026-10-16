class Koji < Formula
  desc "Interactive CLI for creating conventional commits"
  homepage "https://github.com/cococonscious/koji"
  url "https://github.com/cococonscious/koji/releases/download/v3.4.0/koji-x86_64-pc-windows-msvc.tar.gz"
  sha256 "d34bde39e9febc4dbf94588eb6d91b80d784b6e93110ca52871d2309ce1642ad"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cococonscious/koji/archive/refs/tags/v3.4.0.tar.gz"
  end

  def install
    bin.install "koji.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/koji --version 2>&1")
  end
end
