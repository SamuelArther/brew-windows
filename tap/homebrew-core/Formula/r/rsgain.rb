class Rsgain < Formula
  desc "ReplayGain 2.0 tagging utility"
  homepage "https://github.com/complexlogic/rsgain"
  url "https://github.com/complexlogic/rsgain/releases/download/v3.8/rsgain-3.8-win64.zip"
  sha256 "8923cfe3ac930b7342ceae63839dcb65b450628aa452cb8b0047fd35cb86f640"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/complexlogic/rsgain/archive/refs/tags/v3.8.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/rsgain.exe", "rsgain"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rsgain -v")

    assert_match "No files were scanned",
      shell_output("#{bin}/rsgain easy -S #{testpath}")
  end
end
