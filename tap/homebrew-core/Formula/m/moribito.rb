class Moribito < Formula
  desc "TUI for LDAP Viewing/Queries"
  homepage "https://ericschmar.github.io/moribito/"
  url "https://github.com/ericschmar/moribito/releases/download/v0.2.6/moribito_Windows_x86_64.zip"
  sha256 "459dd6446f41390c6b46981ab06599f801040a595d9486ad8e3cdc7c6cbe78a5"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ericschmar/moribito/archive/refs/tags/v0.2.6.tar.gz"
  end

  def install
    bin.install "moribito.exe"
  end

  test do
    system bin/"moribito", "--version"
  end
end
