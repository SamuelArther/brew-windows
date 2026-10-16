class Gtree < Formula
  desc "Generate directory trees and directories using Markdown or programmatically"
  homepage "https://ddddddo.github.io/gtree/"
  url "https://github.com/ddddddO/gtree/releases/download/v1.14.9/gtree_Windows_x86_64.zip"
  sha256 "7d7421eaf7198e53bc7295b8c9758c62ad3b6e54e04b71eed0363f43104ac632"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ddddddO/gtree/archive/refs/tags/v1.14.9.tar.gz"
  end

  def install
    bin.install "gtree.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gtree version")

    assert_match "testdata", shell_output("#{bin}/gtree template")
  end
end
