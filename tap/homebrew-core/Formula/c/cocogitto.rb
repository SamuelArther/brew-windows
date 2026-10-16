class Cocogitto < Formula
  desc "Conventional Commits toolbox"
  homepage "https://docs.cocogitto.io/"
  url "https://github.com/cocogitto/cocogitto/releases/download/7.0.0/cocogitto-7.0.0-x86_64-pc-windows-msvc.tar.gz"
  sha256 "074f68f05d270da5c0d69d3e234ec362bec4c6e3189c21d1c948d038603655d7"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cocogitto/cocogitto/archive/refs/tags/7.0.0.tar.gz"
  end

  def install
    bin.install "cog.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cog --version 2>&1")
  end
end
