class Wallpaper < Formula
  desc "Manage the desktop wallpaper"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wallpaper", "wallpaper"
  end

  test do
    system bin/"wallpaper", "get"
    assert_match "set-solid-color", shell_output("#{bin}/wallpaper --help")
    assert_match "invalid colour", shell_output("#{bin}/wallpaper set-solid-color nothex 2>&1", 2)
  end
end
