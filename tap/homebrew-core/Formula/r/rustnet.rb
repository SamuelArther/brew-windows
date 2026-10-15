class Rustnet < Formula
  desc "Cross-platform network monitoring terminal UI with deep packet inspection"
  homepage "https://github.com/domcyrus/rustnet"
  url "https://github.com/domcyrus/rustnet/releases/download/v1.7.0/rustnet-v1.7.0-x86_64-pc-windows-msvc.zip"
  sha256 "33f1814964e5f281608a9b012ac5695dfe27bce4a52e462fb1679ac8149fdd5d"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/domcyrus/rustnet/archive/refs/tags/v1.7.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/rustnet.exe", "rustnet"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rustnet --version 2>&1")
  end
end
