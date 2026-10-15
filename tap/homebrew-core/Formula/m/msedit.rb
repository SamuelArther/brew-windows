class Msedit < Formula
  desc "Simple text editor with clickable interface"
  homepage "https://github.com/microsoft/edit"
  url "https://github.com/microsoft/edit/releases/download/v2.0.0/edit-2.0.0-x86_64-windows.zip"
  sha256 "97b8f2c667faea8f211af5f9c879602949164911d74d16f540bbe31ad46c14cb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/microsoft/edit/archive/refs/tags/v2.0.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/edit.exe", "edit"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/edit --version 2>&1")
  end
end
