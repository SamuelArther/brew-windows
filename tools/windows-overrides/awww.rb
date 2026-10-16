class Awww < Formula
  desc "Answer to your Wayland Wallpaper Woes (per-monitor and animated wallpapers on Windows)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "awww", "awww"
    windows_exe "libexec/wintools.exe", "awww-daemon", "awww-daemon"
  end

  def caveats
    <<~EOS
      Start the daemon in your desktop session (it owns the backgrounds), then send it images:
        awww-daemon &
        awww img wallpaper.gif --outputs DISPLAY1
      Windows changes wallpapers without transitions; --transition-* options are accepted.
    EOS
  end

  test do
    assert_match "Make sure awww-daemon is running", shell_output("#{bin}/awww clear 2>&1", 1)
    assert_match "Usage: awww-daemon", shell_output("#{bin}/awww-daemon --help")
    assert_match "--resize <RESIZE>", shell_output("#{bin}/awww img --help")
    assert_match "rrggbb", shell_output("#{bin}/awww clear zz 2>&1", 2)
  end
end
