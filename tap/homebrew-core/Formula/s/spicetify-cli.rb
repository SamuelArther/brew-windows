class SpicetifyCli < Formula
  desc "Command-line tool to customize Spotify client"
  homepage "https://spicetify.app/"
  url "https://github.com/spicetify/cli/releases/download/v2.45.3/spicetify-2.45.3-windows-x64.zip"
  sha256 "5d641d4db9caa3891b6a7cc2eb239667af9bb40252a8b413856f9be1e09ff5c1"
  license "LGPL-2.1-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/spicetify/cli/archive/refs/tags/v2.45.3/v2.45.3.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/spicetify.exe", "spicetify"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/spicetify --version 2>&1")
  end
end
