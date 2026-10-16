class Supermodel < Formula
  desc "Sega Model 3 arcade emulator"
  homepage "https://github.com/trzy/Supermodel"
  url "https://github.com/trzy/Supermodel/releases/download/v0.3a-20261014-git-d795141/supermodel-0.3a-20261014-git-d795141-windows.zip"
  sha256 "a66f5b70b046c96257f2f5e57de68bc4004f83e427cca36d5f673aeb88de5efe"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/trzy/Supermodel/archive/refs/tags/v0.3a-20261014-git-12390db.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/supermodel.exe", "supermodel"
  end

  test do
    system bin/"supermodel", "-print-games"
  end
end
