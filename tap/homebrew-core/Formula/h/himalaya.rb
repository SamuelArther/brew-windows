class Himalaya < Formula
  desc "CLI email client written in Rust"
  homepage "https://pimalaya.org"
  url "https://github.com/pimalaya/himalaya/releases/download/v2.2.1/himalaya.x86_64-windows.tgz"
  sha256 "abea8ecbca5fbd566860941bbab91e40f1dde57e737091a451208b8c61b99523"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pimalaya/himalaya/archive/refs/tags/v2.2.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/himalaya.exe", "himalaya"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/himalaya --version 2>&1")
  end
end
