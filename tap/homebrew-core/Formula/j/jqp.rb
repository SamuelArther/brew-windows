class Jqp < Formula
  desc "TUI playground to experiment and play with jq"
  homepage "https://github.com/noahgorstein/jqp"
  url "https://github.com/noahgorstein/jqp/releases/download/v0.8.0/jqp_Windows_x86_64.tar.gz"
  sha256 "9d62970c1355705c7050788f70fe597f65f1fe808a7b29fc34876c90bf509c17"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/noahgorstein/jqp/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "jqp.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jqp --version 2>&1")
  end
end
