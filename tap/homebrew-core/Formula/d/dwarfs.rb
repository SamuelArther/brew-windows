class Dwarfs < Formula
  desc "Fast high compression read-only file system for Linux, Windows, and macOS"
  homepage "https://github.com/mhx/dwarfs"
  url "https://github.com/mhx/dwarfs/releases/download/v0.15.8/dwarfs-0.15.8-Windows-AMD64.7z"
  sha256 "49ffae19533b0c5b3dfc5516b19e97700f1197416062a5517f65bfb631631566"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mhx/dwarfs/releases/download/v0.15.8/dwarfs-0.15.8.tar.xz"
    regex(/^(?:release[._-])?v?(\d+(?:\.\d+)+)$/i)
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/dwarfsck.exe", "dwarfsck"
    windows_exe "libexec/bin/dwarfsextract.exe", "dwarfsextract"
    windows_exe "libexec/bin/mkdwarfs.exe", "mkdwarfs"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dwarfsck -v 2>&1")
  end
end
