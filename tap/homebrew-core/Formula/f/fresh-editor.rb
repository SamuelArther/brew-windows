class FreshEditor < Formula
  desc "Text editor for your terminal: easy, powerful and fast"
  homepage "https://sinelaw.github.io/fresh/"
  url "https://github.com/sinelaw/fresh/releases/download/v0.5.3/fresh-editor-x86_64-pc-windows-msvc.zip"
  sha256 "7e342d27523df9778d66059b08c1421dbd9a27d866f325d640274be66ac3fa0c"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sinelaw/fresh/archive/refs/tags/v0.5.3.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/fresh.exe", "fresh"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fresh --version 2>&1")
  end
end
