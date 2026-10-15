class Superhtml < Formula
  desc "HTML Language Server & Templating Language Library"
  homepage "https://github.com/kristoff-it/superhtml"
  url "https://github.com/kristoff-it/superhtml/releases/download/v0.7.0/x86_64-windows.zip"
  sha256 "1ecb8d701ee01a837c6664707e49328821b660c28a5da82edbbb4e3639afccdc"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kristoff-it/superhtml/archive/refs/tags/v0.7.0.tar.gz"
  end

  def install
    bin.install "superhtml.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/superhtml version 2>&1")
  end
end
