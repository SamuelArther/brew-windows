class Mplayershell < Formula
  desc "Improved visual experience for MPlayer on Windows"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mps", "mps"
  end

  test do
    assert_match "Usage: mps", shell_output("#{bin}/mps --help")
    # No player at the given path: mps reports it instead of hanging.
    assert_match "mps:", shell_output("#{bin}/mps --player #{testpath}/missing/mplayer.exe movie.mkv 2>&1", 1)
  end
end
