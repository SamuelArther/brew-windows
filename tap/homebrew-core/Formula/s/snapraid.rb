class Snapraid < Formula
  desc "Backup program for disk arrays"
  homepage "https://www.snapraid.it/"
  url "https://github.com/amadvance/snapraid/releases/download/v14.10/snapraid-14.10-windows-x64.zip"
  sha256 "6e8892175bfe240ae7828d513342180a249511a2d5baf8c003ae8b2bb82e0f46"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/amadvance/snapraid/releases/download/v14.10/snapraid-14.10.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/snapraid.exe", "snapraid"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/snapraid --version")
  end
end
