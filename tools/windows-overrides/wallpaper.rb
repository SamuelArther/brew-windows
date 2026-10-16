class Wallpaper < Formula
  desc "Manage the desktop wallpaper"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
