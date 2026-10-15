class Ugrep < Formula
  desc "Ultra fast grep with query UI, fuzzy search, archive search, and more"
  homepage "https://ugrep.com/"
  url "https://github.com/Genivia/ugrep/releases/download/v7.8.5/ugrep-windows-x64.zip"
  sha256 "4887e305ff06e5510ee4e3c7da8b7928b59042b379717027aea1aec19b28204f"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Genivia/ugrep/archive/refs/tags/v7.8.5.tar.gz"
  end

  def install
    bin.install "ug.exe"
    bin.install "ugrep.exe"
    bin.install "ugrep-indexer.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ug --version 2>&1")
  end
end
